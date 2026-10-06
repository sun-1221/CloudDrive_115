using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Preview;
using Application.Tests.Support;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Preview;

/// <summary>
/// 115 网盘文档与图片预览处理器测试。
/// </summary>
public sealed class PreviewHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private const string TestCloudAccountUid = "115001";

    [Fact]
    public async Task GetImagePreviewQueryHandler_WhenDownloadReturned_ReturnsTemporaryImageResource()
    {
        TestContext context = CreateContext();
        context.Gateway.RespondWeb("files/download", """
            {"state":true,"file_name":"photo.PNG","file_url_302":"https://download.test/photo"}
            """);
        var handler = new GetImagePreviewQueryHandler(context.Gateway, context.Accessor, CreateEnabledConfig());

        Result<CloudDriveImagePreviewDto> result = await handler.Handle(
            new GetImagePreviewQuery(TestUserId, CreatePreviewInput()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://download.test/photo", result.Value.PreviewUrl);
        Assert.Equal("photo.PNG", result.Value.FileName);
        Assert.Equal("png", result.Value.FileType);
        Assert.True(result.Value.IsTemporary);
        CloudDriveGatewayRequest request = Assert.Single(context.Gateway.WebApiRequests);
        Assert.Equal("files/download", request.Endpoint);
        Assert.Equal("1", request.Context.QueryParameters!["_"]);
        Assert.Equal("1", request.Context.QueryParameters["dl"]);
        Assert.Equal("file-1", request.Context.QueryParameters["pickcode"]);
    }

    [Fact]
    public async Task GetImagePreviewQueryHandler_WhenDownloadIsUnsupported_ReturnsUnsupportedError()
    {
        TestContext context = CreateContext();
        context.Gateway.RespondWeb("files/download", """{"state":true,"file_name":"movie.mp4","file_url_302":"https://download.test/movie"}""");
        var handler = new GetImagePreviewQueryHandler(context.Gateway, context.Accessor, CreateEnabledConfig());

        Result<CloudDriveImagePreviewDto> result = await handler.Handle(
            new GetImagePreviewQuery(TestUserId, CreatePreviewInput()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_image_preview_unsupported", result.Error.Code);
    }

    [Fact]
    public async Task GetDocPreviewQueryHandler_WhenPdfDownloadReturned_ReturnsDirectTemporaryResource()
    {
        TestContext context = CreateContext();
        context.Gateway.RespondWeb("files/download", """{"state":true,"file_name":"manual.pdf","file_url_302":"https://download.test/manual"}""");
        var handler = new GetDocPreviewQueryHandler(context.Gateway, context.Accessor, CreateEnabledConfig());

        Result<CloudDriveDocPreviewDto> result = await handler.Handle(
            new GetDocPreviewQuery(TestUserId, CreatePreviewInput()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("pdf", result.Value.FileType);
        Assert.Equal(DocRenderMode.Direct, result.Value.RenderMode);
        Assert.Equal("https://download.test/manual", result.Value.PreviewUrl);
        Assert.True(result.Value.IsTemporary);
    }

    [Fact]
    public async Task GetDocPreviewQueryHandler_WhenOfficeServiceConfigured_ReturnsThirdPartyResource()
    {
        TestContext context = CreateContext();
        context.Gateway.RespondWeb("files/download", """{"state":true,"file_name":"manual.docx","file_url_302":"https://download.test/manual?a=1"}""");
        var handler = new GetDocPreviewQueryHandler(
            context.Gateway,
            context.Accessor,
            CreateEnabledConfig("https://office.test/preview"));

        Result<CloudDriveDocPreviewDto> result = await handler.Handle(
            new GetDocPreviewQuery(TestUserId, CreatePreviewInput()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DocRenderMode.ThirdParty, result.Value.RenderMode);
        Assert.Equal("https://office.test/preview?url=https%3A%2F%2Fdownload.test%2Fmanual%3Fa%3D1", result.Value.ThirdPartyPreviewUrl);
        Assert.True(result.Value.IsTemporary);
    }

    [Fact]
    public async Task GetDocPreviewQueryHandler_WhenOfficeServiceMissing_ReturnsNotAvailable()
    {
        TestContext context = CreateContext();
        context.Gateway.RespondWeb("files/download", """{"state":true,"file_name":"manual.docx","file_url_302":"https://download.test/manual"}""");
        var handler = new GetDocPreviewQueryHandler(context.Gateway, context.Accessor, CreateEnabledConfig());

        Result<CloudDriveDocPreviewDto> result = await handler.Handle(
            new GetDocPreviewQuery(TestUserId, CreatePreviewInput()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DocRenderMode.NotAvailable, result.Value.RenderMode);
        Assert.Equal("https://download.test/manual", result.Value.PreviewUrl);
        Assert.True(result.Value.IsTemporary);
    }

    private static TestContext CreateContext()
    {
        var gateway = new RecordingCloudDriveOpenApiGateway();
        var accessor = new CloudDriveAccountContextAccessor();
        accessor.SetContext(new CloudDriveAccountContext(TestUserId, TestCloudAccountUid, TestCloudAccountUid));
        return new TestContext(gateway, accessor);
    }

    private static GetPreviewInput CreatePreviewInput() => new() { PickCode = "file-1" };

    private static IOptions<CloudDriveAppConfig> CreateEnabledConfig(string? officePreviewUrl = null) =>
        Options.Create(new CloudDriveAppConfig
        {
            CloudDriveFeatureEnabled = true,
            OfficePreviewServiceUrl = officePreviewUrl,
        });

    private sealed record TestContext(
        RecordingCloudDriveOpenApiGateway Gateway,
        CloudDriveAccountContextAccessor Accessor);
}
