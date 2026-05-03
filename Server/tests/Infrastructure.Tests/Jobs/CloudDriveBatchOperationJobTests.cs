using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;

using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

using Infrastructure.Jobs;

using MediatR;

using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Jobs;

public sealed class CloudDriveBatchOperationJobTests
{
    private static readonly Guid TestUserId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private const string TestCloudAccountUid = "115001";

    [Theory]
    [MemberData(nameof(BatchOperationCases))]
    public async Task Execute_WhenValidBatchPayload_CallsExpectedEndpointAndCompletesJobAsync(
        BatchOperationCase testCase)
    {
        // Arrange
        CloudDriveJob job = CreateJob(testCase.JobType, testCase.Payload);
        var repository = new CloudDriveJobTestDoubles.FakeCloudDriveJobRepository(job);
        var gateway = new RecordingBatchGateway();
        var metadataCache = new CloudDriveJobTestDoubles.RecordingMetadataCache();
        var transactionManager = new CloudDriveJobTestDoubles.RecordingTransactionManager();
        var publisher = new CloudDriveJobTestDoubles.RecordingPublisher();
        CloudDriveBatchOperationJob batchJob = CreateBatchJob(
            repository,
            gateway,
            metadataCache,
            transactionManager,
            publisher);

        // Act
        await batchJob.Execute(CloudDriveJobTestDoubles.CreateExecutionContext(job.Id));

        // Assert
        Assert.Equal(CloudDriveJobStatus.Completed, job.Status);
        Assert.Equal(100, job.Progress);
        Assert.Null(job.ErrorMessage);
        Assert.NotNull(job.CheckpointData);
        Assert.Equal(3, transactionManager.CommitCount);
        Assert.Equal(0, transactionManager.RollbackCount);
        Assert.Equal([(TestUserId, TestCloudAccountUid, testCase.ExpectedInvalidatedDirectoryId)],
            metadataCache.InvalidatedDirectories);

        RecordingBatchRequest request = Assert.Single(gateway.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(testCase.ExpectedEndpoint, request.Endpoint);
        foreach (KeyValuePair<string, string> expectedField in testCase.ExpectedFormFields)
        {
            Assert.Equal(expectedField.Value, request.FormFields[expectedField.Key]);
        }

        using JsonDocument checkpoint = JsonDocument.Parse(job.CheckpointData!);
        Assert.Equal(testCase.ExpectedEndpoint, checkpoint.RootElement.GetProperty("endpoint").GetString());
        Assert.Equal(2, checkpoint.RootElement.GetProperty("completedCount").GetInt32());
    }

    [Fact]
    public async Task Execute_WhenGatewayThrows_FailsJobAndDoesNotInvalidateCacheAsync()
    {
        // Arrange
        CloudDriveJob job = CreateJob(
            CloudDriveJobType.BatchDelete,
            new DeleteFilesInput
            {
                CloudAccountUid = TestCloudAccountUid,
                FileIds = ["file-1", "file-2"]
            });
        var repository = new CloudDriveJobTestDoubles.FakeCloudDriveJobRepository(job);
        var gateway = new RecordingBatchGateway
        {
            ExceptionToThrow = new InvalidOperationException("upstream rejected")
        };
        var metadataCache = new CloudDriveJobTestDoubles.RecordingMetadataCache();
        var transactionManager = new CloudDriveJobTestDoubles.RecordingTransactionManager();
        CloudDriveBatchOperationJob batchJob = CreateBatchJob(
            repository,
            gateway,
            metadataCache,
            transactionManager,
            new CloudDriveJobTestDoubles.RecordingPublisher());

        // Act
        await batchJob.Execute(CloudDriveJobTestDoubles.CreateExecutionContext(job.Id));

        // Assert
        Assert.Equal(CloudDriveJobStatus.Failed, job.Status);
        Assert.Equal(1, job.Progress);
        Assert.Contains("upstream rejected", job.ErrorMessage, StringComparison.Ordinal);
        Assert.Empty(metadataCache.InvalidatedDirectories);
        Assert.Equal(2, transactionManager.CommitCount);
        Assert.Equal(0, transactionManager.RollbackCount);
    }

    public static IEnumerable<object[]> BatchOperationCases()
    {
        yield return
        [
            new BatchOperationCase(
                CloudDriveJobType.BatchCopy,
                new CopyFilesInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    FileIds = ["file-1", "file-2"],
                    TargetDirectoryId = "target-1"
                },
                "files/copy",
                "target-1",
                new Dictionary<string, string>
                {
                    ["format"] = "json",
                    ["fileIds"] = "file-1,file-2",
                    ["targetDirectoryId"] = "target-1"
                })
        ];

        yield return
        [
            new BatchOperationCase(
                CloudDriveJobType.BatchMove,
                new MoveFilesInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    FileIds = ["file-1", "file-2"],
                    TargetDirectoryId = "target-1"
                },
                "files/move",
                "target-1",
                new Dictionary<string, string>
                {
                    ["format"] = "json",
                    ["fileIds"] = "file-1,file-2",
                    ["targetDirectoryId"] = "target-1"
                })
        ];

        yield return
        [
            new BatchOperationCase(
                CloudDriveJobType.BatchDelete,
                new DeleteFilesInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    FileIds = ["file-1", "file-2"]
                },
                "rb/delete",
                "0",
                new Dictionary<string, string>
                {
                    ["aid"] = "7",
                    ["format"] = "json",
                    ["fileIds"] = "file-1,file-2"
                })
        ];

        yield return
        [
            new BatchOperationCase(
                CloudDriveJobType.BatchRecycleBinRestore,
                new RecycleBinOperationInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    RecycleBinItemIds = ["recycle-1", "recycle-2"]
                },
                "rb/revert",
                "0",
                new Dictionary<string, string>
                {
                    ["format"] = "json",
                    ["tid"] = "recycle-1,recycle-2"
                })
        ];

        yield return
        [
            new BatchOperationCase(
                CloudDriveJobType.BatchRecycleBinDelete,
                new RecycleBinOperationInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    RecycleBinItemIds = ["recycle-1", "recycle-2"]
                },
                "rb/delete",
                "0",
                new Dictionary<string, string>
                {
                    ["format"] = "json",
                    ["ignore_warn"] = "1",
                    ["tid"] = "recycle-1,recycle-2"
                })
        ];
    }

    private static CloudDriveJob CreateJob(CloudDriveJobType jobType, object payload)
    {
        string payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return CloudDriveJob.Create(
            TestUserId,
            TestCloudAccountUid,
            jobType,
            payloadJson: payloadJson);
    }

    private static CloudDriveBatchOperationJob CreateBatchJob(
        ICloudDriveJobRepository repository,
        ICloudDriveOpenApiGateway gateway,
        ICloudDriveMetadataCache metadataCache,
        Application.Abstractions.ITransactionManager transactionManager,
        IPublisher publisher)
    {
        return new CloudDriveBatchOperationJob(
            repository,
            gateway,
            metadataCache,
            transactionManager,
            publisher,
            new CloudDriveJobTestDoubles.RecordingJobAuditLogger(),
            NullLogger<CloudDriveBatchOperationJob>.Instance);
    }

    public sealed record BatchOperationCase(
        CloudDriveJobType JobType,
        object Payload,
        string ExpectedEndpoint,
        string ExpectedInvalidatedDirectoryId,
        IReadOnlyDictionary<string, string> ExpectedFormFields)
    {
        public override string ToString() => JobType.ToString();
    }

    private sealed class RecordingBatchGateway : ICloudDriveOpenApiGateway
    {
        public List<RecordingBatchRequest> Requests { get; } = [];

        public Exception? ExceptionToThrow { get; init; }

        public Task<T> SendWebApiRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            HttpMethod method,
            string endpoint,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            Requests.Add(new RecordingBatchRequest(
                method,
                endpoint,
                context.FormFields is null
                    ? new Dictionary<string, string>(StringComparer.Ordinal)
                    : new Dictionary<string, string>(context.FormFields, StringComparer.Ordinal),
                context.Category));

            return Task.FromResult((T)(object)JsonDocument.Parse("""{"state":true}""").RootElement.Clone());
        }

        public Task<T> SendUserCenterRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            HttpMethod method,
            string endpoint,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<T> SendOfflineApiRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            HttpMethod method,
            string endpoint,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<T> SendUploadInitRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<T> UploadFileAsync<T>(
            Guid userId,
            string cloudAccountUid,
            string uploadUrl,
            Stream fileStream,
            string fileName,
            IDictionary<string, string>? formFields = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed record RecordingBatchRequest(
        HttpMethod Method,
        string Endpoint,
        IReadOnlyDictionary<string, string> FormFields,
        string? Category);
}
