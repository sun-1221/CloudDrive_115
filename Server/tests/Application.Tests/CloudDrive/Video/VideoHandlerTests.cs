using Application.CloudDrive;
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
    public async Task GetVideoPlayUrlCommandHandler_WhenVideoReady_ReturnsQualityOptions()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.RespondWeb("files/video", """
            {"state":true,"data":{"play_long":120,"video_url":[{"definition":4,"url":"https://video.test/1080.mp4","title":"1080P","height":1080,"width":1920}]}}
            """);
        var handler = new GetVideoPlayUrlCommandHandler(
            context.FileManager,
            context.Gateway,
            context.JobOrchestrator,
            CreateEnabledConfig());

        // Act
        Result<CloudDriveVideoPlayDto> result = await handler.Handle(
            new GetVideoPlayUrlCommand(
                TestUserId,
                new GetVideoPlayInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    PickCode = "pick-1",
                    Definition = 4,
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(VideoPlayStatus.Ready, result.Value.Status);
        Assert.Equal(120, result.Value.Duration);
        Assert.Equal("https://video.test/1080.mp4", result.Value.QualityOptions.Single().PlayUrl);
        Assert.True(result.Value.IsTemporary);
        Assert.Empty(context.JobOrchestrator.Transcodes);
        CloudDriveGatewayRequest request = Assert.Single(context.Gateway.WebApiRequests);
        Assert.Equal("files/video", request.Endpoint);
        Assert.Equal("0", request.Context.QueryParameters!["share_id"]);
    }

    [Fact]
    public async Task GetVideoPlayUrlCommandHandler_WhenTranscoding_EnqueuesTranscodeJob()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = new CloudDriveFileDetailDto
        {
            FileId = "file-1",
            Name = "demo.mp4",
            FileType = "mp4",
            PickCode = "pick-1",
            IsVideo = true,
        };
        context.Gateway.RespondWeb("files/video", """{"state":true,"data":{"status":"transcoding","video_url":[]}}""");
        var handler = new GetVideoPlayUrlCommandHandler(
            context.FileManager,
            context.Gateway,
            context.JobOrchestrator,
            CreateEnabledConfig());

        // Act
        Result<CloudDriveVideoPlayDto> result = await handler.Handle(
            new GetVideoPlayUrlCommand(
                TestUserId,
                new GetVideoPlayInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    FileId = "file-1",
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(VideoPlayStatus.Transcoding, result.Value.Status);
        Assert.Equal(context.JobOrchestrator.NextJobId, result.Value.JobId);
        Assert.Equal("pick-1", context.JobOrchestrator.Transcodes.Single().Input.PickCode);
    }

    [Fact]
    public async Task SubmitTranscodeCommandHandler_WhenVideoFile_EnqueuesTranscode()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = new CloudDriveFileDetailDto
        {
            FileId = "file-1",
            Name = "demo.mp4",
            FileType = "mp4",
            PickCode = "pick-1",
            IsVideo = true,
        };
        var handler = new SubmitTranscodeCommandHandler(
            context.FileManager,
            context.JobOrchestrator,
            CreateEnabledConfig());

        // Act
        Result<CloudDriveVideoTranscodeDto> result = await handler.Handle(
            new SubmitTranscodeCommand(
                TestUserId,
                new SubmitTranscodeInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    FileId = "file-1",
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
            CreateEnabledConfig());

        // Act
        Result<CloudDriveSubtitleListDto> result = await handler.Handle(
            new GetSubtitlesQuery(
                TestUserId,
                new GetSubtitlesInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    PickCode = "pick-1",
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        CloudDriveSubtitleDto subtitle = Assert.Single(result.Value.Items);
        Assert.Equal("s1", subtitle.SubtitleId);
        Assert.Equal("cm1", subtitle.CaptionMapId);
        Assert.True(subtitle.IsCaptionMap);
        Assert.Equal(3, subtitle.SyncTime);
    }

    [Fact]
    public async Task RememberVideoPlayProgressCommandHandler_WhenValidInput_PostsHistory()
    {
        // Arrange
        TestContext context = CreateContext();
        var handler = new RememberVideoPlayProgressCommandHandler(
            context.FileManager,
            context.Gateway,
            CreateEnabledConfig());

        // Act
        Result result = await handler.Handle(
            new RememberVideoPlayProgressCommand(
                TestUserId,
                new RememberVideoPlayProgressInput
                {
                    CloudAccountUid = TestCloudAccountUid,
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

        return new TestContext(gateway, fileManager, new FakeCloudDriveJobOrchestrator());
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
        FakeCloudDriveJobOrchestrator JobOrchestrator);
}
