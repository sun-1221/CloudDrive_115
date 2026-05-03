using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Events;

using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using Domain.Repositories;

using Infrastructure.Http.Responses;
using Infrastructure.Jobs;

using MediatR;

using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Jobs;

public sealed class CloudDriveUploadJobTests : IDisposable
{
    private static readonly Guid TestUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private const string TestCloudAccountUid = "115001";
    private readonly string _tempFilePath = Path.Combine(Path.GetTempPath(), $"cloud-upload-{Guid.NewGuid():N}.tmp");

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Fact]
    public async Task Execute_WhenInstantUpload_CompletesJobAndInvalidatesTargetDirectoryAsync()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempFilePath, "demo");
        UploadFileInput input = CreateInput();
        CloudDriveJob job = CreateJob(input);
        var repository = new CloudDriveJobTestDoubles.FakeCloudDriveJobRepository(job);
        var gateway = new RecordingUploadGateway
        {
            InitResponse = new CloudDriveUploadInitResponse
            {
                FileId = "file-001",
                FileName = "demo.txt",
                PickCode = "pick-001",
                Sha1 = "SHA1",
                DirectoryId = input.DirectoryId,
                FileSize = input.FileSize
            }
        };
        var metadataCache = new CloudDriveJobTestDoubles.RecordingMetadataCache();
        var transactionManager = new CloudDriveJobTestDoubles.RecordingTransactionManager();
        var publisher = new CloudDriveJobTestDoubles.RecordingPublisher();
        CloudDriveUploadJob uploadJob = CreateUploadJob(repository, gateway, metadataCache, transactionManager, publisher);

        // Act
        await uploadJob.Execute(CloudDriveJobTestDoubles.CreateExecutionContext(job.Id));

        // Assert
        Assert.Equal(CloudDriveJobStatus.Completed, job.Status);
        Assert.Equal(100, job.Progress);
        Assert.Null(job.ErrorMessage);
        Assert.NotNull(job.CheckpointData);
        Assert.Equal([(TestUserId, TestCloudAccountUid, "dir-001")], metadataCache.InvalidatedDirectories);
        Assert.Single(gateway.UploadInitContexts);
        Assert.Empty(gateway.UploadRequests);
        Assert.Equal(3, transactionManager.CommitCount);
        Assert.Equal(0, transactionManager.RollbackCount);
        Assert.Equal(2, publisher.PublishedNotifications.Count);
        Assert.All(publisher.PublishedNotifications, notification =>
            Assert.IsType<DomainEventNotification<CloudDriveJobStatusChanged>>(notification));

        using JsonDocument checkpoint = JsonDocument.Parse(job.CheckpointData!);
        Assert.Equal(_tempFilePath, checkpoint.RootElement.GetProperty("tempFilePath").GetString());
        Assert.Equal("pick-001", checkpoint.RootElement.GetProperty("pickCode").GetString());

        CloudDriveApiContext initContext = Assert.Single(gateway.UploadInitContexts);
        Assert.Equal("upload", initContext.Category);
        Assert.Equal(TestCloudAccountUid, initContext.FormFields!["userid"]);
        Assert.Equal("demo.txt", initContext.FormFields["filename"]);
        Assert.Equal("U_1_dir-001", initContext.FormFields["target"]);
    }

    [Fact]
    public async Task Execute_WhenOssUploadRequired_UploadsFileAndCompletesJobAsync()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempFilePath, "demo");
        UploadFileInput input = CreateInput();
        CloudDriveJob job = CreateJob(input);
        var repository = new CloudDriveJobTestDoubles.FakeCloudDriveJobRepository(job);
        var gateway = new RecordingUploadGateway
        {
            InitResponse = new CloudDriveUploadInitResponse
            {
                Object = "object-key",
                AccessId = "access-id",
                Host = "https://oss.example.com/",
                Policy = "policy",
                Signature = "signature",
                Callback = "callback",
                PickCode = "pick-oss",
                Expire = 1893456000
            },
            CompleteResponse = new CloudDriveUploadCompleteResponse
            {
                FileId = "file-oss",
                FileName = input.FileName,
                DirectoryId = input.DirectoryId,
                PickCode = "pick-oss"
            }
        };
        var metadataCache = new CloudDriveJobTestDoubles.RecordingMetadataCache();
        CloudDriveUploadJob uploadJob = CreateUploadJob(
            repository,
            gateway,
            metadataCache,
            new CloudDriveJobTestDoubles.RecordingTransactionManager(),
            new CloudDriveJobTestDoubles.RecordingPublisher());

        // Act
        await uploadJob.Execute(CloudDriveJobTestDoubles.CreateExecutionContext(job.Id));

        // Assert
        Assert.Equal(CloudDriveJobStatus.Completed, job.Status);
        Assert.Equal(100, job.Progress);
        RecordingUploadRequest request = Assert.Single(gateway.UploadRequests);
        Assert.Equal("https://oss.example.com/", request.UploadUrl);
        Assert.Equal("demo.txt", request.FileName);
        Assert.Equal("object-key", request.FormFields["key"]);
        Assert.Equal("access-id", request.FormFields["OSSAccessKeyId"]);
        Assert.Equal([(TestUserId, TestCloudAccountUid, "dir-001")], metadataCache.InvalidatedDirectories);
    }

    [Fact]
    public async Task Execute_WhenTempFileMissing_FailsJobAndDoesNotCallGatewayAsync()
    {
        // Arrange
        UploadFileInput input = CreateInput();
        CloudDriveJob job = CreateJob(input);
        var repository = new CloudDriveJobTestDoubles.FakeCloudDriveJobRepository(job);
        var gateway = new RecordingUploadGateway();
        var transactionManager = new CloudDriveJobTestDoubles.RecordingTransactionManager();
        CloudDriveUploadJob uploadJob = CreateUploadJob(
            repository,
            gateway,
            new CloudDriveJobTestDoubles.RecordingMetadataCache(),
            transactionManager,
            new CloudDriveJobTestDoubles.RecordingPublisher());

        // Act
        await uploadJob.Execute(CloudDriveJobTestDoubles.CreateExecutionContext(job.Id));

        // Assert
        Assert.Equal(CloudDriveJobStatus.Failed, job.Status);
        Assert.Equal(1, job.Progress);
        Assert.Contains("上传临时文件不存在", job.ErrorMessage, StringComparison.Ordinal);
        Assert.Empty(gateway.UploadInitContexts);
        Assert.Empty(gateway.UploadRequests);
        Assert.Equal(2, transactionManager.CommitCount);
        Assert.Equal(0, transactionManager.RollbackCount);
    }

    private UploadFileInput CreateInput()
    {
        return new UploadFileInput
        {
            CloudAccountUid = TestCloudAccountUid,
            DirectoryId = "dir-001",
            FileName = "demo.txt",
            FileSize = 4,
            FileSha1 = "SHA1",
            TempFilePath = _tempFilePath
        };
    }

    private static CloudDriveJob CreateJob(UploadFileInput input)
    {
        string payloadJson = JsonSerializer.Serialize(input, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return CloudDriveJob.Create(
            TestUserId,
            input.CloudAccountUid,
            CloudDriveJobType.Upload,
            input.DirectoryId,
            payloadJson);
    }

    private static CloudDriveUploadJob CreateUploadJob(
        ICloudDriveJobRepository repository,
        ICloudDriveOpenApiGateway gateway,
        ICloudDriveMetadataCache metadataCache,
        Application.Abstractions.ITransactionManager transactionManager,
        IPublisher publisher)
    {
        return new CloudDriveUploadJob(
            repository,
            gateway,
            metadataCache,
            transactionManager,
            publisher,
            new CloudDriveJobTestDoubles.RecordingJobAuditLogger(),
            NullLogger<CloudDriveUploadJob>.Instance);
    }

    private sealed class RecordingUploadGateway : ICloudDriveOpenApiGateway
    {
        public CloudDriveUploadInitResponse InitResponse { get; init; } = new();

        public CloudDriveUploadCompleteResponse CompleteResponse { get; init; } = new();

        public List<CloudDriveApiContext> UploadInitContexts { get; } = [];

        public List<RecordingUploadRequest> UploadRequests { get; } = [];

        public Task<T> SendWebApiRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            HttpMethod method,
            string endpoint,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

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
            CancellationToken cancellationToken = default)
        {
            UploadInitContexts.Add(context);
            return Task.FromResult((T)(object)InitResponse);
        }

        public async Task<T> UploadFileAsync<T>(
            Guid userId,
            string cloudAccountUid,
            string uploadUrl,
            Stream fileStream,
            string fileName,
            IDictionary<string, string>? formFields = null,
            CancellationToken cancellationToken = default)
        {
            using var reader = new StreamReader(fileStream);
            string content = await reader.ReadToEndAsync(cancellationToken);
            UploadRequests.Add(new RecordingUploadRequest(
                uploadUrl,
                fileName,
                formFields is null
                    ? new Dictionary<string, string>(StringComparer.Ordinal)
                    : new Dictionary<string, string>(formFields, StringComparer.Ordinal),
                content));

            return (T)(object)CompleteResponse;
        }
    }

    private sealed record RecordingUploadRequest(
        string UploadUrl,
        string FileName,
        IReadOnlyDictionary<string, string> FormFields,
        string FileContent);

}
