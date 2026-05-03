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

public sealed class CloudDriveClearRecycleBinJobTests
{
    private static readonly Guid TestUserId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private const string TestCloudAccountUid = "115001";

    [Fact]
    public async Task Execute_WhenValidPayload_CallsCleanEndpointCompletesJobAndInvalidatesRecycleBinAsync()
    {
        // Arrange
        CloudDriveJob job = CreateJob(new ClearRecycleBinInput { CloudAccountUid = TestCloudAccountUid });
        var repository = new CloudDriveJobTestDoubles.FakeCloudDriveJobRepository(job);
        var gateway = new RecordingClearGateway();
        var metadataCache = new CloudDriveJobTestDoubles.RecordingMetadataCache();
        var transactionManager = new CloudDriveJobTestDoubles.RecordingTransactionManager();
        CloudDriveClearRecycleBinJob clearJob = CreateClearJob(
            repository,
            gateway,
            metadataCache,
            transactionManager,
            new CloudDriveJobTestDoubles.RecordingPublisher());

        // Act
        await clearJob.Execute(CloudDriveJobTestDoubles.CreateExecutionContext(job.Id));

        // Assert
        Assert.Equal(CloudDriveJobStatus.Completed, job.Status);
        Assert.Equal(100, job.Progress);
        Assert.Null(job.ErrorMessage);
        Assert.NotNull(job.CheckpointData);
        Assert.Equal(3, transactionManager.CommitCount);
        Assert.Equal(0, transactionManager.RollbackCount);
        Assert.Equal([(TestUserId, TestCloudAccountUid, "0")], metadataCache.InvalidatedDirectories);

        RecordingClearRequest request = Assert.Single(gateway.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("rb/clean", request.Endpoint);
        Assert.Equal("recycle-bin", request.Category);
        Assert.Equal("json", request.FormFields["format"]);

        using JsonDocument checkpoint = JsonDocument.Parse(job.CheckpointData!);
        Assert.Equal("rb/clean", checkpoint.RootElement.GetProperty("endpoint").GetString());
    }

    [Fact]
    public async Task Execute_WhenGatewayThrows_FailsJobAndDoesNotInvalidateCacheAsync()
    {
        // Arrange
        CloudDriveJob job = CreateJob(new ClearRecycleBinInput { CloudAccountUid = TestCloudAccountUid });
        var repository = new CloudDriveJobTestDoubles.FakeCloudDriveJobRepository(job);
        var gateway = new RecordingClearGateway
        {
            ExceptionToThrow = new InvalidOperationException("clean rejected")
        };
        var metadataCache = new CloudDriveJobTestDoubles.RecordingMetadataCache();
        var transactionManager = new CloudDriveJobTestDoubles.RecordingTransactionManager();
        CloudDriveClearRecycleBinJob clearJob = CreateClearJob(
            repository,
            gateway,
            metadataCache,
            transactionManager,
            new CloudDriveJobTestDoubles.RecordingPublisher());

        // Act
        await clearJob.Execute(CloudDriveJobTestDoubles.CreateExecutionContext(job.Id));

        // Assert
        Assert.Equal(CloudDriveJobStatus.Failed, job.Status);
        Assert.Equal(1, job.Progress);
        Assert.Contains("clean rejected", job.ErrorMessage, StringComparison.Ordinal);
        Assert.Empty(metadataCache.InvalidatedDirectories);
        Assert.Equal(2, transactionManager.CommitCount);
        Assert.Equal(0, transactionManager.RollbackCount);
    }

    private static CloudDriveJob CreateJob(ClearRecycleBinInput input)
    {
        string payloadJson = JsonSerializer.Serialize(input, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return CloudDriveJob.Create(
            TestUserId,
            input.CloudAccountUid,
            CloudDriveJobType.ClearRecycleBin,
            resourceId: "recycle-bin",
            payloadJson: payloadJson);
    }

    private static CloudDriveClearRecycleBinJob CreateClearJob(
        ICloudDriveJobRepository repository,
        ICloudDriveOpenApiGateway gateway,
        ICloudDriveMetadataCache metadataCache,
        Application.Abstractions.ITransactionManager transactionManager,
        IPublisher publisher)
    {
        return new CloudDriveClearRecycleBinJob(
            repository,
            gateway,
            metadataCache,
            transactionManager,
            publisher,
            new CloudDriveJobTestDoubles.RecordingJobAuditLogger(),
            NullLogger<CloudDriveClearRecycleBinJob>.Instance);
    }

    private sealed class RecordingClearGateway : ICloudDriveOpenApiGateway
    {
        public List<RecordingClearRequest> Requests { get; } = [];

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

            Requests.Add(new RecordingClearRequest(
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

    private sealed record RecordingClearRequest(
        HttpMethod Method,
        string Endpoint,
        IReadOnlyDictionary<string, string> FormFields,
        string? Category);
}
