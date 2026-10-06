using System.Text;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Tests.Support;

using Domain.Enums;
using Domain.Exceptions;
using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Files;

/// <summary>
/// 115 网盘文件命令与查询处理器测试。
/// </summary>
public sealed class FileCommandQueryHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private const string TestCloudAccountUid = "115001";

    /// <summary>
    /// 验证目录缺省时文件列表查询使用根目录并缓存结果。
    /// </summary>
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
                    PickCode = "pick-1",
                }

            ],
            TotalCount = 1,
            SkipCount = 0,
            MaxResultCount = 50,
        };
        var handler = new GetFileListQueryHandler(context.FileManager, context.Accessor, CreateEnabledConfig());

        // Act
        Result<PagedResultDto<CloudDriveFileDto>> result = await handler.Handle(
            new GetFileListQuery(
                TestUserId,
                new GetFileListInput
                {
                    Offset = 0,
                    Limit = 50,
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

    /// <summary>
    /// 验证文件列表仅发送已采集的查询键，并保留 fc_mix 与 nf 控制值。
    /// </summary>
    [Fact]
    public void CreateFileListContext_WhenObservedControlsProvided_UsesCollectedQuerySet()
    {
        CloudDriveApiContext context = CloudDriveFileHandlerSupport.CreateFileListContext(
            new GetFileListInput
            {
                DirectoryId = "dir-1",
                Offset = 4,
                Limit = 20,
                Type = 0,
                Asc = 0,
                OrderBy = "user_ptime",
                CustomOrder = 1,
                FcMix = 0,
                Nf = 1,
                Stdir = 1,
                ShowDir = 1,
                NatSort = 1,
                CountFolders = 1,
                Suffix = "mp4",
                Star = 1,
                Cur = 1,
                Snap = 1,
                RecordOpenTime = 1,
                Code = "ignored",
                Scid = "ignored",
                Source = "ignored",
            });

        Assert.Equal(
            ["aid", "asc", "cid", "count_folders", "custom_order", "fc_mix", "format", "limit", "natsort", "nf", "o", "offset", "show_dir", "stdir", "type"],
            context.QueryParameters!.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray());
        Assert.DoesNotContain("suffix", context.QueryParameters.Keys);
        Assert.Equal("0", context.QueryParameters["fc_mix"]);
        Assert.Equal("1", context.QueryParameters["nf"]);
    }

    /// <summary>
    /// 验证文件详情由已采集文件列表条目派生，且不会调用独立详情端点。
    /// </summary>
    [Fact]
    public async Task GetFileDetailQueryHandler_WhenListContainsFile_DerivesDetailFromList()
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
                    PickCode = "pick-1",
                    ParentId = "parent-1",
                    IsVideo = true,
                }
            ],
            TotalCount = 1,
            SkipCount = 0,
            MaxResultCount = 200,
            Paths =
            [
                new CloudDrivePathNodeDto { FileId = "parent-1", FileName = "Videos" }
            ],
        };
        var handler = new GetFileDetailQueryHandler(
            context.FileManager,
            context.Accessor,
            CreateEnabledConfig());

        // Act
        Result<CloudDriveFileDetailDto> result = await handler.Handle(
            new GetFileDetailQuery(
                TestUserId,
                new GetFileInput
                {
                    FileId = "file-1",
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("file-1", result.Value.FileId);
        Assert.Equal("clip.mp4", result.Value.Name);
        Assert.Equal("parent-1", result.Value.ParentId);
        Assert.Equal("file_list_derived", result.Value.SchemaSource);
        Assert.Single(result.Value.Paths!);
        CloudDriveGatewayRequest request = Assert.Single(context.Gateway.WebApiRequests);
        Assert.Equal("files", request.Endpoint);
        Assert.Equal("0", request.Context.QueryParameters!["cid"]);
    }

    /// <summary>
    /// 验证列表未包含指定文件时返回文件不存在错误。
    /// </summary>
    [Fact]
    public async Task GetFileDetailQueryHandler_WhenListDoesNotContainFile_ReturnsFileNotFound()
    {
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = new PagedResultDto<CloudDriveFileDto>();
        var handler = new GetFileDetailQueryHandler(
            context.FileManager,
            context.Accessor,
            CreateEnabledConfig());

        Result<CloudDriveFileDetailDto> result = await handler.Handle(
            new GetFileDetailQuery(
                TestUserId,
                new GetFileInput { FileId = "missing" }),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_file_not_found", result.Error.Code);
    }

    /// <summary>
    /// 验证创建文件夹名称非法时抛出无效名称异常。
    /// </summary>
    [Fact]
    public async Task CreateFolderCommandHandler_WhenFolderNameInvalid_ThrowsInvalidName()
    {
        // Arrange
        TestContext context = CreateContext();
        var handler = new CreateFolderCommandHandler(
            context.Gateway, context.Accessor, context.Cache, CreateEnabledConfig());

        // Act & Assert
        await Assert.ThrowsAsync<CloudDriveInvalidNameException>(() => handler.Handle(
            new CreateFolderCommand(
                TestUserId,
                new CreateFolderInput
                {
                    ParentId = "0",
                    FolderName = "bad/name",
                }),
            CancellationToken.None));
    }

    /// <summary>
    /// 验证创建文件夹成功 envelope 映射结果并失效父目录缓存。
    /// </summary>
    [Fact]
    public async Task CreateFolderCommandHandler_WhenSuccessEnvelopeReturned_MapsFolderAndInvalidatesCache()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = "{\"state\":true,\"errno\":\"\",\"file_id\":\"folder-1\",\"file_name\":\"Photos\"}";
        var handler = new CreateFolderCommandHandler(
            context.Gateway, context.Accessor, context.Cache, CreateEnabledConfig());

        // Act
        Result<CloudDriveFolderDto> result = await handler.Handle(
            new CreateFolderCommand(
                TestUserId,
                new CreateFolderInput
                {
                    ParentId = "parent-1",
                    FolderName = "Photos",
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("folder-1", result.Value.FolderId);
        Assert.Equal("Photos", result.Value.Name);
        Assert.Equal([(TestUserId, TestCloudAccountUid, "parent-1")], context.Cache.InvalidatedDirectories);
        CloudDriveGatewayRequest request = Assert.Single(context.Gateway.WebApiRequests);
        Assert.Equal("files/add", request.Endpoint);
        Assert.Equal("Photos", request.Context.FormFields!["cname"]);
        Assert.Equal("parent-1", request.Context.FormFields["pid"]);
        Assert.Equal(2, request.Context.FormFields.Count);
    }

    /// <summary>
    /// 验证下载地址只发送已采集 query，并标记返回地址为临时资源。
    /// </summary>
    [Fact]
    public async Task GetDownloadUrlQueryHandler_WhenSuccessEnvelopeReturned_UsesCollectedQuery()
    {
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = """
            {
              "state": true,
              "file_id": "file-1",
              "file_name": "demo.txt",
              "file_size": "42",
              "file_url_302": "https://download.example/temporary"
            }
            """;
        var handler = new GetDownloadUrlQueryHandler(
            context.Gateway,
            context.Accessor,
            CreateEnabledConfig());

        Result<CloudDriveDownloadDto> result = await handler.Handle(
            new GetDownloadUrlQuery(
                TestUserId,
                new GetDownloadUrlInput { PickCode = "pick-1" }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsTemporary);
        Assert.Equal("42", result.Value.FileSize.ToString());
        CloudDriveGatewayRequest request = Assert.Single(context.Gateway.WebApiRequests);
        Assert.Equal("files/download", request.Endpoint);
        Assert.Equal(
            ["_", "dl", "pickcode"],
            request.Context.QueryParameters!.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray());
    }

    /// <summary>
    /// 验证单项复制仅在精确成功 envelope 后完成并失效目标目录缓存。
    /// </summary>
    [Fact]
    public async Task CopyFilesCommandHandler_WhenSuccessEnvelopeReturned_CompletesSingleCopy()
    {
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = "{\"state\":true,\"errno\":\"\",\"error\":\"\"}";
        var handler = new CopyFilesCommandHandler(
            context.Gateway,
            context.Accessor,
            context.Cache,
            CreateEnabledConfig());

        Result<CloudDriveOperationResultDto> result = await handler.Handle(
            new CopyFilesCommand(
                TestUserId,
                new CopyFilesInput
                {
                    FileIds = ["file-1"],
                    TargetDirectoryId = "target-1",
                }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains((TestUserId, TestCloudAccountUid, "target-1"), context.Cache.InvalidatedDirectories);
        CloudDriveGatewayRequest request = Assert.Single(context.Gateway.WebApiRequests);
        Assert.Equal("files/copy", request.Endpoint);
        Assert.Equal("file-1", request.Context.FormFields!["fid[0]"]);
        Assert.Equal("json", request.Context.FormFields["format"]);
        Assert.Equal("target-1", request.Context.FormFields["pid"]);
    }

    /// <summary>
    /// 验证精确成功码类型不匹配时重命名 fail-closed。
    /// </summary>
    [Fact]
    public async Task RenameFileCommandHandler_WhenErrnoTypeIsWrong_FailsClosed()
    {
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = "{\"state\":true,\"errno\":\"0\",\"data\":{}}";
        var handler = new RenameFileCommandHandler(
            context.Gateway,
            context.Accessor,
            context.Cache,
            CreateEnabledConfig());

        await Assert.ThrowsAsync<CloudDriveApiException>(() => handler.Handle(
            new RenameFileCommand(
                TestUserId,
                new RenameFileInput { FileId = "file-1", NewFileName = "renamed.txt" }),
            CancellationToken.None));

        Assert.Empty(context.Cache.InvalidatedFiles);
    }

    /// <summary>
    /// 验证星标请求仅发送已采集 form 字段并在成功后失效文件缓存。
    /// </summary>
    [Fact]
    public async Task ToggleStarCommandHandler_WhenSuccessEnvelopeReturned_UsesCollectedForm()
    {
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = "{\"state\":true,\"error\":\"\"}";
        var handler = new ToggleStarCommandHandler(
            context.Gateway,
            context.Accessor,
            context.Cache,
            CreateEnabledConfig());

        Result<CloudDriveOperationResultDto> result = await handler.Handle(
            new ToggleStarCommand(
                TestUserId,
                new ToggleStarInput { FileId = "file-1", Star = true }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains((TestUserId, TestCloudAccountUid, "file-1"), context.Cache.InvalidatedFiles);
        CloudDriveGatewayRequest request = Assert.Single(context.Gateway.WebApiRequests);
        Assert.Equal("1", request.Context.FormFields!["star"]);
        Assert.Equal("file-1", request.Context.FormFields["file_id"]);
        Assert.Equal("json", request.Context.FormFields["format"]);
    }

    /// <summary>
    /// 验证重命名文件名 UTF-8 长度超限时抛出无效名称异常。
    /// </summary>
    [Fact]
    public async Task RenameFileCommandHandler_WhenUtf8NameTooLong_ThrowsInvalidName()
    {
        // Arrange
        TestContext context = CreateContext();
        var handler = new RenameFileCommandHandler(
            context.Gateway, context.Accessor, context.Cache, CreateEnabledConfig());
        string tooLong = new('中', 86);

        // Act & Assert
        Assert.True(Encoding.UTF8.GetByteCount(tooLong) > 255);
        await Assert.ThrowsAsync<CloudDriveInvalidNameException>(() => handler.Handle(
            new RenameFileCommand(
                TestUserId,
                new RenameFileInput
                {
                    FileId = "file-1",
                    NewFileName = tooLong,
                }),
            CancellationToken.None));
    }

    /// <summary>
    /// 验证上传能力禁用时在 Gateway、临时文件和后台任务之前 fail-closed。
    /// </summary>
    [Fact]
    public async Task UploadFileCommandHandler_WhenCapabilityDisabled_ReturnsNotCollectedWithoutSideEffects()
    {
        // Arrange
        TestContext context = CreateContext();
        var handler = new UploadFileCommandHandler();

        // Act
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("demo"));
        Result<CloudDriveUploadExecutionDto> result = await handler.Handle(
            new UploadFileCommand(TestUserId, CreateUploadInput(), stream),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_api_not_collected", result.Error.Code);
        Assert.Empty(context.Gateway.UploadRequests);
        Assert.Empty(context.JobOrchestrator.Uploads);
    }

    /// <summary>
    /// 验证小批量移动接口未采集时返回失败且不发送网关请求。
    /// </summary>
    [Fact]
    public async Task MoveFilesCommandHandler_WhenSmallBatchApiNotCollected_ReturnsFailureWithoutGatewayRequest()
    {
        // Arrange
        TestContext context = CreateContext();
        var handler = new MoveFilesCommandHandler();
        var input = new MoveFilesInput
        {
            FileIds = ["file-1", "file-2"],
            TargetDirectoryId = "target-1",
        };

        // Act
        Result<CloudDriveOperationResultDto> result = await handler.Handle(
            new MoveFilesCommand(TestUserId, input),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_api_not_collected", result.Error.Code);
        Assert.Empty(context.Cache.InvalidatedDirectories);
        Assert.Empty(context.Gateway.WebApiRequests);
    }

    /// <summary>
    /// 验证复制能力保持单项 disabled，不创建批量后台任务。
    /// </summary>
    [Fact]
    public async Task CopyFilesCommandHandler_WhenBatchRequested_ReturnsNotCollectedWithoutJob()
    {
        // Arrange
        TestContext context = CreateContext();
        var handler = new CopyFilesCommandHandler(
            context.Gateway,
            context.Accessor,
            context.Cache,
            CreateEnabledConfig());
        var input = new CopyFilesInput
        {
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
        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_copy_single_file_required", result.Error.Code);
        Assert.Empty(context.JobOrchestrator.BatchOperations);
        Assert.Empty(context.Gateway.WebApiRequests);
    }

    /// <summary>
    /// 验证回收站网关返回空页时处理器返回空列表。
    /// </summary>
    [Fact]
    public async Task GetRecycleBinListQueryHandler_WhenGatewayReturnsEmptyPage_ReturnsEmptyItems()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = new CloudDriveRecycleListDto
        {
            Items = [],
            Count = 0,
            Offset = 0,
            PageSize = 30,
                    ItemSchemaStatus = "available",
        };
        var handler = new GetRecycleBinListQueryHandler(context.Gateway, context.Accessor, CreateEnabledConfig());

        // Act
        Result<CloudDriveRecycleListDto> result = await handler.Handle(
            new GetRecycleBinListQuery(
                TestUserId,
                new GetRecycleBinListInput
                {
                    Offset = 0,
                    Limit = 30,
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal("available", result.Value.ItemSchemaStatus);
        CloudDriveGatewayRequest request = Assert.Single(context.Gateway.WebApiRequests);
        Assert.Equal("rb", request.Endpoint);
        Assert.Equal(
            ["asc", "format", "limit", "o", "offset"],
            request.Context.QueryParameters!.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray());
        Assert.Equal("0", request.Context.QueryParameters!["asc"]);
        Assert.Equal("file_name", request.Context.QueryParameters["o"]);
    }

    /// <summary>
    /// 验证清空回收站保持 disabled，不创建后台任务。
    /// </summary>
    [Fact]
    public async Task ClearRecycleBinCommandHandler_WhenCalled_ReturnsNotCollectedWithoutJob()
    {
        // Arrange
        TestContext context = CreateContext();
        var handler = new ClearRecycleBinCommandHandler();

        // Act
        Result<CloudDriveOperationResultDto> result = await handler.Handle(
            new ClearRecycleBinCommand(
                TestUserId,
                new ClearRecycleBinInput()),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_api_not_collected", result.Error.Code);
        Assert.Empty(context.JobOrchestrator.ClearRecycleBins);
    }

    /// <summary>
    /// 验证刷新目录命令失效目录缓存并重新拉取最新元数据。
    /// </summary>
    [Fact]
    public async Task RefreshDirectoryCommandHandler_WhenCalled_InvalidatesAndFetchesFreshMetadata()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = new PagedResultDto<CloudDriveFileDto>();
        var handler = new RefreshDirectoryCommandHandler(
            context.Gateway,
            context.Cache,
            context.Accessor,
            CreateEnabledConfig());

        // Act
        Result result = await handler.Handle(
            new RefreshDirectoryCommand(
                TestUserId,
                new RefreshDirectoryInput
                {
                    DirectoryId = "dir-1",
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal([(TestUserId, TestCloudAccountUid, "dir-1")], context.Cache.InvalidatedDirectories);
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
            CreateEnabledConfig(),
            logger);
        var accessor = new CloudDriveAccountContextAccessor();
        accessor.SetContext(new CloudDriveAccountContext(TestUserId, TestCloudAccountUid, TestCloudAccountUid));

        return new TestContext(
            cache,
            gateway,
            rateLimiter,
            new FakeCloudDriveJobOrchestrator(),
            fileManager,
            accessor);
    }

    private static UploadFileInput CreateUploadInput()
    {
        return new UploadFileInput
        {
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
        CloudDriveFileManager FileManager,
        CloudDriveAccountContextAccessor Accessor);
}
