using System.Text;

using Application.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Queries;
using Application.Tests.Support;

using Domain.Enums;
using Domain.Exceptions;
using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Files;

public sealed class FileCommandQueryHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private const string TestCloudAccountUid = "115001";

    [Fact]
    public async Task GetFileListQueryHandler_WhenDirectoryMissing_UsesRootAndCachesResult()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = new PagedResultDto<CloudDriveFileDto>
        {
            Items =
            [
                new CloudDriveFileDto
                {
                    FileId = "file-1",
                    Name = "clip.mp4",
                    Size = 42,
                    IsDirectory = false,
                    PickCode = "pick-1",
                }
            ],
            TotalCount = 1,
            SkipCount = 0,
            MaxResultCount = 20,
        };
        var handler = new GetFileListQueryHandler(context.FileManager, CreateEnabledConfig());

        // Act
        Result<PagedResultDto<CloudDriveFileDto>> result = await handler.Handle(
            new GetFileListQuery(
                TestUserId,
                new GetFileListInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    Offset = 0,
                    Limit = 20,
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Contains($":{TestCloudAccountUid}:dir:0:list", context.Cache.GetOrSetKeys.Single());
        CloudDriveGatewayRequest request = Assert.Single(context.Gateway.WebApiRequests);
        Assert.Equal("files", request.Endpoint);
        Assert.Equal("1", request.Context.QueryParameters!["aid"]);
        Assert.Equal("0", request.Context.QueryParameters["cid"]);
        Assert.Equal("json", request.Context.QueryParameters["format"]);
    }

    [Fact]
    public async Task GetFileDetailQueryHandler_WhenGatewayReturnsFile_ReturnsDetail()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = new CloudDriveFileDetailDto
        {
            FileId = "file-1",
            Name = "clip.mp4",
            PickCode = "pick-1",
        };
        var handler = new GetFileDetailQueryHandler(context.FileManager, CreateEnabledConfig());

        // Act
        Result<CloudDriveFileDetailDto> result = await handler.Handle(
            new GetFileDetailQuery(
                TestUserId,
                new GetFileInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    FileId = "file-1",
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("file-1", result.Value.FileId);
        CloudDriveGatewayRequest request = Assert.Single(context.Gateway.WebApiRequests);
        Assert.Equal("files/file", request.Endpoint);
        Assert.Equal("file-1", request.Context.QueryParameters!["file_id"]);
    }

    [Fact]
    public async Task SearchFilesQueryHandler_WhenKeywordProvided_SendsSearchRequestWithoutMetadataCache()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = new PagedResultDto<CloudDriveSearchResultDto>
        {
            Items =
            [
                new CloudDriveSearchResultDto
                {
                    FileId = "file-1",
                    Name = "demo.txt",
                    ParentId = "dir-1",
                }
            ],
            TotalCount = 1,
            MaxResultCount = 50,
        };
        var handler = new SearchFilesQueryHandler(context.Gateway, context.RateLimiter, CreateEnabledConfig());

        // Act
        Result<PagedResultDto<CloudDriveSearchResultDto>> result = await handler.Handle(
            new SearchFilesQuery(
                TestUserId,
                new SearchFileInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    Keyword = "demo",
                    DirectoryId = "dir-1",
                    FileLabel = "label-1",
                    GteDay = "2026-01-01",
                    LteDay = "2026-01-02",
                    Fc = 2,
                    Type = 1,
                    Suffix = "txt",
                    Limit = 50,
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(context.Cache.GetOrSetKeys);
        Assert.Equal("file-search", context.RateLimiter.Acquisitions.Single().Category);
        CloudDriveGatewayRequest request = Assert.Single(context.Gateway.WebApiRequests);
        Assert.Equal("files/search", request.Endpoint);
        Assert.Equal("demo", request.Context.QueryParameters!["search_value"]);
        Assert.Equal("label-1", request.Context.QueryParameters["file_label"]);
        Assert.Equal("2", request.Context.QueryParameters["fc"]);
    }

    [Fact]
    public async Task CreateFolderCommandHandler_WhenFolderNameInvalid_ThrowsInvalidName()
    {
        // Arrange
        TestContext context = CreateContext();
        var handler = new CreateFolderCommandHandler(context.FileManager, context.Gateway, CreateEnabledConfig());

        // Act & Assert
        await Assert.ThrowsAsync<CloudDriveInvalidNameException>(() => handler.Handle(
            new CreateFolderCommand(
                TestUserId,
                new CreateFolderInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    ParentId = "0",
                    FolderName = "bad/name",
                }),
            CancellationToken.None));
    }

    [Fact]
    public async Task CreateFolderCommandHandler_WhenSuccessful_InvalidatesParentDirectory()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = CreateJson("""{"data":{"file_id":"folder-1","file_name":"Photos"}}""");
        var handler = new CreateFolderCommandHandler(context.FileManager, context.Gateway, CreateEnabledConfig());

        // Act
        Result<CloudDriveFolderDto> result = await handler.Handle(
            new CreateFolderCommand(
                TestUserId,
                new CreateFolderInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    ParentId = "parent-1",
                    FolderName = "Photos",
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("folder-1", result.Value.FolderId);
        Assert.Equal([(TestUserId, TestCloudAccountUid, "parent-1")], context.Cache.InvalidatedDirectories);
        CloudDriveGatewayRequest request = Assert.Single(context.Gateway.WebApiRequests);
        Assert.Equal("files/add", request.Endpoint);
        Assert.Equal("Photos", request.Context.FormFields!["cname"]);
    }

    [Fact]
    public async Task RenameFileCommandHandler_WhenUtf8NameTooLong_ThrowsInvalidName()
    {
        // Arrange
        TestContext context = CreateContext();
        var handler = new RenameFileCommandHandler(context.FileManager, context.Gateway, CreateEnabledConfig());
        string tooLong = new('中', 86);

        // Act & Assert
        Assert.True(Encoding.UTF8.GetByteCount(tooLong) > 255);
        await Assert.ThrowsAsync<CloudDriveInvalidNameException>(() => handler.Handle(
            new RenameFileCommand(
                TestUserId,
                new RenameFileInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    FileId = "file-1",
                    NewFileName = tooLong,
                }),
            CancellationToken.None));
    }

    [Fact]
    public async Task UploadFileCommandHandler_WhenInstantUpload_ReturnsFileAndInvalidatesTargetDirectory()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.UploadInitResponse = new CloudDriveUploadSessionDto
        {
            Status = 2,
            PickCode = "pick-1",
            File = new CloudDriveUploadedFileDto
            {
                FileId = "file-1",
                Name = "demo.txt",
                Path = "dir-1",
            },
        };
        var handler = new UploadFileCommandHandler(
            context.Gateway,
            context.JobOrchestrator,
            context.Cache,
            CreateEnabledConfig());

        // Act
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("demo"));
        Result<CloudDriveUploadExecutionDto> result = await handler.Handle(
            new UploadFileCommand(TestUserId, CreateUploadInput(), stream),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Accepted);
        Assert.Equal("file-1", result.Value.File!.FileId);
        Assert.Equal([(TestUserId, TestCloudAccountUid, "dir-1")], context.Cache.InvalidatedDirectories);
        Assert.Empty(context.Gateway.UploadRequests);
    }

    [Fact]
    public async Task UploadFileCommandHandler_WhenOssUploadRequired_UploadsStream()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.UploadInitResponse = new CloudDriveUploadSessionDto
        {
            Status = 1,
            UploadEndpoint = "https://oss.example.com/",
            FormFields = new Dictionary<string, string>
            {
                ["key"] = "object-key",
                ["policy"] = "policy",
                ["OSSAccessKeyId"] = "access-id",
                ["signature"] = "signature",
                ["callback"] = "callback",
            },
        };
        context.Gateway.UploadFileResponse = new CloudDriveUploadedFileDto
        {
            FileId = "file-oss",
            Name = "demo.txt",
            Path = "dir-1",
        };
        var handler = new UploadFileCommandHandler(
            context.Gateway,
            context.JobOrchestrator,
            context.Cache,
            CreateEnabledConfig());

        // Act
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("payload"));
        Result<CloudDriveUploadExecutionDto> result = await handler.Handle(
            new UploadFileCommand(TestUserId, CreateUploadInput(), stream),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("file-oss", result.Value.File!.FileId);
        CloudDriveUploadRequest upload = Assert.Single(context.Gateway.UploadRequests);
        Assert.Equal("https://oss.example.com/", upload.UploadUrl);
        Assert.Equal("payload", upload.Content);
        Assert.Equal("object-key", upload.FormFields["key"]);
    }

    [Fact]
    public async Task UploadFileCommandHandler_WhenForceAsync_EnqueuesUploadJob()
    {
        // Arrange
        TestContext context = CreateContext();
        var handler = new UploadFileCommandHandler(
            context.Gateway,
            context.JobOrchestrator,
            context.Cache,
            CreateEnabledConfig());
        UploadFileInput input = CreateUploadInput();
        input.ForceAsync = true;
        input.TempFilePath = "C:\\temp\\upload.tmp";

        // Act
        Result<CloudDriveUploadExecutionDto> result = await handler.Handle(
            new UploadFileCommand(TestUserId, input, FileStream: null),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Accepted);
        Assert.Equal(context.JobOrchestrator.NextJobId, result.Value.JobId);
        Assert.Single(context.JobOrchestrator.Uploads);
        Assert.Empty(context.Gateway.UploadInitContexts);
    }

    [Fact]
    public async Task MoveFilesCommandHandler_WhenSmallBatch_SendsSyncRequestAndInvalidatesTarget()
    {
        // Arrange
        TestContext context = CreateContext();
        var handler = new MoveFilesCommandHandler(
            context.FileManager,
            context.Gateway,
            context.JobOrchestrator,
            CreateEnabledConfig());
        var input = new MoveFilesInput
        {
            CloudAccountUid = TestCloudAccountUid,
            FileIds = ["file-1", "file-2"],
            TargetDirectoryId = "target-1",
        };

        // Act
        Result<CloudDriveOperationResultDto> result = await handler.Handle(
            new MoveFilesCommand(TestUserId, input),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.Accepted);
        Assert.Equal([(TestUserId, TestCloudAccountUid, "target-1")], context.Cache.InvalidatedDirectories);
        CloudDriveGatewayRequest request = Assert.Single(context.Gateway.WebApiRequests);
        Assert.Equal("files/move", request.Endpoint);
        Assert.Equal("file-1,file-2", request.Context.FormFields!["fileIds"]);
    }

    [Fact]
    public async Task CopyFilesCommandHandler_WhenLargeBatch_EnqueuesBatchJob()
    {
        // Arrange
        TestContext context = CreateContext();
        var handler = new CopyFilesCommandHandler(
            context.FileManager,
            context.Gateway,
            context.JobOrchestrator,
            CreateEnabledConfig());
        var input = new CopyFilesInput
        {
            CloudAccountUid = TestCloudAccountUid,
            FileIds = Enumerable.Range(0, CloudDriveFileHandlerSupport.DefaultAsyncBatchThreshold + 1)
                .Select(index => $"file-{index}")
                .ToList(),
            TargetDirectoryId = "target-1",
        };

        // Act
        Result<CloudDriveOperationResultDto> result = await handler.Handle(
            new CopyFilesCommand(TestUserId, input),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Accepted);
        Assert.Equal(CloudDriveJobType.BatchCopy, context.JobOrchestrator.BatchOperations.Single().JobType);
        Assert.Empty(context.Gateway.WebApiRequests);
    }

    [Fact]
    public async Task GetRecycleBinListQueryHandler_WhenGatewayReturnsEmptyPage_ReturnsEmptyItems()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = new PagedResultDto<CloudDriveRecycleBinItemDto>
        {
            Items = [],
            TotalCount = 0,
            SkipCount = 0,
            MaxResultCount = 30,
        };
        var handler = new GetRecycleBinListQueryHandler(context.Gateway, CreateEnabledConfig());

        // Act
        Result<PagedResultDto<CloudDriveRecycleBinItemDto>> result = await handler.Handle(
            new GetRecycleBinListQuery(
                TestUserId,
                new GetRecycleBinListInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    Offset = 0,
                    Limit = 30,
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        CloudDriveGatewayRequest request = Assert.Single(context.Gateway.WebApiRequests);
        Assert.Equal("rb", request.Endpoint);
        Assert.Equal("7", request.Context.QueryParameters!["aid"]);
    }

    [Fact]
    public async Task ClearRecycleBinCommandHandler_AlwaysEnqueuesBackgroundJob()
    {
        // Arrange
        TestContext context = CreateContext();
        var handler = new ClearRecycleBinCommandHandler(context.JobOrchestrator, CreateEnabledConfig());

        // Act
        Result<CloudDriveOperationResultDto> result = await handler.Handle(
            new ClearRecycleBinCommand(
                TestUserId,
                new ClearRecycleBinInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Accepted);
        Assert.Equal(context.JobOrchestrator.NextJobId, result.Value.JobId);
        Assert.Equal((TestUserId, TestCloudAccountUid), context.JobOrchestrator.ClearRecycleBins.Single());
    }

    [Fact]
    public async Task RefreshDirectoryCommandHandler_InvalidatesAndFetchesFreshMetadata()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = new PagedResultDto<CloudDriveFileDto>();
        var handler = new RefreshDirectoryCommandHandler(
            context.Gateway,
            context.Cache,
            context.RateLimiter,
            CreateEnabledConfig());

        // Act
        Result result = await handler.Handle(
            new RefreshDirectoryCommand(
                TestUserId,
                new RefreshDirectoryInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    DirectoryId = "dir-1",
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal([(TestUserId, TestCloudAccountUid, "dir-1")], context.Cache.InvalidatedDirectories);
        Assert.Equal("file-list", context.RateLimiter.Acquisitions.Single().Category);
        Assert.Single(context.Gateway.WebApiRequests);
    }

    private static TestContext CreateContext()
    {
        var cache = new FakeCloudDriveMetadataCache();
        var gateway = new RecordingCloudDriveOpenApiGateway();
        var rateLimiter = new FakeCloudDriveRateLimiter();
        var logger = new FakeLogger<CloudDriveFileManager>();
        var fileManager = new CloudDriveFileManager(
            cache,
            gateway,
            rateLimiter,
            CreateEnabledConfig(),
            logger);

        return new TestContext(
            cache,
            gateway,
            rateLimiter,
            new FakeCloudDriveJobOrchestrator(),
            fileManager);
    }

    private static UploadFileInput CreateUploadInput()
    {
        return new UploadFileInput
        {
            CloudAccountUid = TestCloudAccountUid,
            DirectoryId = "dir-1",
            FileName = "demo.txt",
            FileSize = 4,
            FileSha1 = "0123456789abcdef0123456789abcdef01234567",
        };
    }

    private static System.Text.Json.JsonElement CreateJson(string json)
    {
        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static IOptions<CloudDriveAppConfig> CreateEnabledConfig()
    {
        return Options.Create(new CloudDriveAppConfig
        {
            CloudDriveFeatureEnabled = true,
            LargeFileAsyncThresholdBytes = 1024 * 1024,
        });
    }

    private sealed record TestContext(
        FakeCloudDriveMetadataCache Cache,
        RecordingCloudDriveOpenApiGateway Gateway,
        FakeCloudDriveRateLimiter RateLimiter,
        FakeCloudDriveJobOrchestrator JobOrchestrator,
        CloudDriveFileManager FileManager);
}
