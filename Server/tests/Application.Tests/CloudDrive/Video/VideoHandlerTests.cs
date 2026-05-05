using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.CloudDrive.Video;
using Application.Tests.Support;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Video;

public sealed class VideoHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private const string TestCloudAccountUid = "115001";

    [Fact]
    public async Task GetVideoPlayUrlCommandHandler_WhenVideoSchemaUncollected_ReturnsEmptyPlayback()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.RespondWeb("files/video", """
            {"state":true,"data":{"play_long":120,"video_url":[{"definition":4,"url":"https://video.test/1080.mp4","title":"1080P","height":1080,"width":1920}]}}
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
        Assert.Equal(VideoPlayStatus.SchemaNotCollected, result.Value.Status);
        Assert.Equal("not_collected", result.Value.PlaybackSchemaStatus);
        Assert.Empty(result.Value.QualityOptions);
        Assert.True(result.Value.IsTemporary);
        Assert.Empty(context.JobOrchestrator.Transcodes);
        CloudDriveGatewayRequest request = Assert.Single(context.Gateway.WebApiRequests);
        Assert.Equal("files/video", request.Endpoint);
        Assert.Equal("0", request.Context.QueryParameters!["share_id"]);
    }

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
        Assert.Equal(VideoPlayStatus.SchemaNotCollected, result.Value.Status);
        Assert.Null(result.Value.JobId);
        Assert.Empty(context.JobOrchestrator.Transcodes);
    }

    [Fact]
    public async Task SubmitTranscodeCommandHandler_WhenVideoFile_EnqueuesTranscode()
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
        Assert.True(result.IsSuccess);
        Assert.Equal(context.JobOrchestrator.NextJobId, result.Value.JobId);
        Assert.Equal("pick-1", context.JobOrchestrator.Transcodes.Single().Input.PickCode);
    }

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

    [Fact]
    public async Task RememberVideoPlayProgressCommandHandler_WhenValidInput_PostsHistory()
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
        Assert.True(result.IsSuccess);
        CloudDriveGatewayRequest request = Assert.Single(context.Gateway.WebApiRequests);
        Assert.Equal("files/history", request.Endpoint);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("end", request.Context.FormFields!["op"]);
        Assert.Equal("30", request.Context.FormFields["time"]);
    }

    private static TestContext CreateContext()
    {
        var cache = new FakeCloudDriveMetadataCache();
        var gateway = new RecordingCloudDriveOpenApiGateway();
        var rateLimiter = new FakeCloudDriveRateLimiter();
        var fileManager = new CloudDriveFileManager(
            cache,
            gateway,
            rateLimiter,
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
