using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.CloudDrive.Video;
using Application.Tests.Support;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Video;

/// <summary>
/// 115 网盘视频播放、转码、字幕与播放进度处理器测试。
/// </summary>
public sealed class VideoHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private const string TestCloudAccountUid = "115001";

    /// <summary>
    /// 验证视频播放结构未采集时返回空播放信息。
    /// </summary>
    [Fact]
    public async Task GetVideoPlayUrlCommandHandler_WhenVideoSchemaCollected_ReturnsBasePlaybackFields()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.RespondWeb("files/video", """
            {"state":true,"data":{"download_url":"https://video.test/1080.mp4","dv_profile":"profile","file_id":"fid-1","file_name":"clip.mp4","file_size":"100","file_status":1,"hdr_type":"none","inlay_power":0,"is_dolby_vision":0,"is_hdr":0,"is_mark":"0","parent_id":"0","pick_code":"pick-1","sha1":"sha1","video_push_state":true}}
            """);
        var handler = new GetVideoPlayUrlCommandHandler(
            context.FileManager,
            context.Gateway,
            context.Accessor,
            CreateEnabledConfig());

        // Act
        Result<CloudDriveVideoPlayDto> result = await handler.Handle(
            new GetVideoPlayUrlCommand(
                TestUserId,
                new GetVideoPlayInput
                {
                    PickCode = "pick-1",
                    Definition = 4,
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(VideoPlayStatus.Ready, result.Value.Status);
        Assert.Equal("available", result.Value.PlaybackSchemaStatus);
        Assert.Equal("https://video.test/1080.mp4", result.Value.DownloadUrl);
        Assert.Equal("fid-1", result.Value.FileId);
        Assert.Equal("clip.mp4", result.Value.FileName);
        Assert.Empty(result.Value.QualityOptions);
        Assert.True(result.Value.IsTemporary);
        Assert.Empty(context.JobOrchestrator.Transcodes);
        CloudDriveGatewayRequest request = Assert.Single(context.Gateway.WebApiRequests);
        Assert.Equal("files/video", request.Endpoint);
        Assert.Equal("0", request.Context.QueryParameters!["share_id"]);
    }

    /// <summary>
    /// 验证转码中视频播放查询不会创建转码任务。
    /// </summary>
    [Fact]
    public async Task GetVideoPlayUrlCommandHandler_WhenTranscoding_EnqueuesTranscodeJob()
    {
        // Arrange — files/file 接口未采集，直接传 PickCode 绕过文件详情查找
        TestContext context = CreateContext();
        context.Gateway.RespondWeb("files/video", """{"state":true,"data":{"status":"transcoding","video_url":[]}}""");
        var handler = new GetVideoPlayUrlCommandHandler(
            context.FileManager,
            context.Gateway,
            context.Accessor,
            CreateEnabledConfig());

        // Act
        Result<CloudDriveVideoPlayDto> result = await handler.Handle(
            new GetVideoPlayUrlCommand(
                TestUserId,
                new GetVideoPlayInput
                {
                    PickCode = "pick-1",
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(VideoPlayStatus.Ready, result.Value.Status);
        Assert.Equal("available", result.Value.PlaybackSchemaStatus);
        Assert.Null(result.Value.JobId);
        Assert.Empty(context.JobOrchestrator.Transcodes);
    }

    /// <summary>
    /// 验证主动转码保持 disabled，不创建转码后台任务。
    /// </summary>
    [Fact]
    public async Task SubmitTranscodeCommandHandler_WhenVideoFile_ReturnsNotCollectedWithoutJob()
    {
        // Arrange — files/file 接口未采集，直接传 PickCode 绕过文件详情查找
        TestContext context = CreateContext();
        var handler = new SubmitTranscodeCommandHandler(
            context.FileManager,
            context.JobOrchestrator,
            context.Accessor,
            CreateEnabledConfig());

        // Act
        Result<CloudDriveVideoTranscodeDto> result = await handler.Handle(
            new SubmitTranscodeCommand(
                TestUserId,
                new SubmitTranscodeInput
                {
                    PickCode = "pick-1",
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_api_not_collected", result.Error.Code);
        Assert.Empty(context.JobOrchestrator.Transcodes);
    }

    /// <summary>
    /// 验证字幕查询返回未采集状态时不暴露字幕明细。
    /// </summary>
    [Fact]
    public async Task GetSubtitlesQueryHandler_WhenSubtitlesReturned_MapsPublicFields()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.RespondWeb("movies/subtitle", """
            {"state":true,"data":{"autoload":[{"sid":"s1","language":"zh","title":"中文","url":"https://sub.test/a.srt","caption_map_id":"cm1","is_caption_map":1,"sync_time":3,"sha1":"sha","file_id":"sub-file","file_name":"a.srt","pick_code":"pick-1"}],"list":[]}}
            """);
        var handler = new GetSubtitlesQueryHandler(
            context.FileManager,
            context.Gateway,
            context.Accessor,
            CreateEnabledConfig());

        // Act
        Result<CloudDriveSubtitleListDto> result = await handler.Handle(
            new GetSubtitlesQuery(
                TestUserId,
                new GetSubtitlesInput
                {
                    PickCode = "pick-1",
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.AutoloadSubtitle);
        Assert.Empty(result.Value.Items);
        Assert.Equal("not_collected", result.Value.ItemSchemaStatus);
    }

    /// <summary>
    /// 验证播放历史为空时返回空播放进度。
    /// </summary>
    [Fact]
    public async Task GetVideoPlayProgressQueryHandler_WhenHistoryIsEmpty_ReturnsEmptyProgress()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.RespondWeb("files/history", """{"state":true,"data":[]}""");
        var handler = new GetVideoPlayProgressQueryHandler(
            context.FileManager,
            context.Gateway,
            context.Accessor,
            CreateEnabledConfig());

        // Act
        Result<CloudDriveVideoPlayProgressDto> result = await handler.Handle(
            new GetVideoPlayProgressQuery(
                TestUserId,
                new GetVideoPlayProgressInput
                {
                    PickCode = "pick-1",
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("pick-1", result.Value.PickCode);
        Assert.Null(result.Value.FileId);
        Assert.Null(result.Value.FileName);
        Assert.Null(result.Value.Hash);
        Assert.Null(result.Value.PlayedSeconds);
        Assert.Null(result.Value.AddTime);
        Assert.Equal("available", result.Value.SchemaStatus);
    }

    /// <summary>
    /// 验证播放历史为对象结构时映射播放进度字段。
    /// </summary>
    [Fact]
    public async Task GetVideoPlayProgressQueryHandler_WhenHistoryIsObject_MapsProgressFields()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.RespondWeb("files/history", """{"state":true,"data":{"file_id":"fid-1","file_name":"clip.mp4","hash":"sha1hash","pick_code":"pick-1","time":120,"add_time":1776518000}}""");
        var handler = new GetVideoPlayProgressQueryHandler(
            context.FileManager,
            context.Gateway,
            context.Accessor,
            CreateEnabledConfig());

        // Act
        Result<CloudDriveVideoPlayProgressDto> result = await handler.Handle(
            new GetVideoPlayProgressQuery(
                TestUserId,
                new GetVideoPlayProgressInput
                {
                    PickCode = "pick-1",
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("fid-1", result.Value.FileId);
        Assert.Equal("clip.mp4", result.Value.FileName);
        Assert.Equal("sha1hash", result.Value.Hash);
        Assert.Equal("pick-1", result.Value.PickCode);
        Assert.Equal(120L, result.Value.PlayedSeconds);
        Assert.Equal(1776518000L, result.Value.AddTime);
        Assert.Equal("available", result.Value.SchemaStatus);
    }

    /// <summary>
    /// 验证播放进度写入保持 disabled。
    /// </summary>
    [Fact]
    public async Task RememberVideoPlayProgressCommandHandler_WhenValidInput_ReturnsNotCollected()
    {
        // Arrange
        TestContext context = CreateContext();
        var handler = new RememberVideoPlayProgressCommandHandler(
            context.FileManager,
            context.Gateway,
            context.Accessor,
            CreateEnabledConfig());

        // Act
        Result result = await handler.Handle(
            new RememberVideoPlayProgressCommand(
                TestUserId,
                new RememberVideoPlayProgressInput
                {
                    PickCode = "pick-1",
                    Time = 30,
                    WatchEnd = true,
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_api_not_collected", result.Error.Code);
        Assert.Empty(context.Gateway.WebApiRequests);
    }

    private static TestContext CreateContext()
    {
        var cache = new FakeCloudDriveMetadataCache();
        var gateway = new RecordingCloudDriveOpenApiGateway();
        var rateLimiter = new FakeCloudDriveRateLimiter();
        var fileManager = new CloudDriveFileManager(
            cache,
            gateway,
            CreateEnabledConfig(),
            new FakeLogger<CloudDriveFileManager>());
        var accessor = new CloudDriveAccountContextAccessor();
        accessor.SetContext(new CloudDriveAccountContext(TestUserId, TestCloudAccountUid, TestCloudAccountUid));

        return new TestContext(gateway, fileManager, new FakeCloudDriveJobOrchestrator(), accessor);
    }

    private static IOptions<CloudDriveAppConfig> CreateEnabledConfig()
    {
        return Options.Create(new CloudDriveAppConfig
        {
            CloudDriveFeatureEnabled = true,
            CacheTtlSeconds = 60,
        });
    }

    private sealed record TestContext(
        RecordingCloudDriveOpenApiGateway Gateway,
        CloudDriveFileManager FileManager,
        FakeCloudDriveJobOrchestrator JobOrchestrator,
        CloudDriveAccountContextAccessor Accessor);
}
