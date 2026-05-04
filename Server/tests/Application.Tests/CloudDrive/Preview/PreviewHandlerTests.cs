using Application.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.CloudDrive.Preview;
using Application.Tests.Support;

using Domain.Results;

using FsCheck.Xunit;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Preview;

public sealed class PreviewHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private const string TestCloudAccountUid = "115001";

    [Property]
    public bool GetDocPreviewQueryHandler_WhenDirectDocumentExtension_ReturnsDirectRenderMode(byte selector)
    {
        string extension = Pick(selector, "pdf", "txt", "PDF", ".txt");
        return PreviewDocument(extension, officePreviewUrl: null).RenderMode == DocRenderMode.Direct;
    }

    [Property]
    public bool GetDocPreviewQueryHandler_WhenOfficeServiceMissing_ReturnsNotAvailable(byte selector)
    {
        string extension = Pick(selector, "doc", "docx", "xls", "xlsx", "ppt", "pptx");
        return PreviewDocument(extension, officePreviewUrl: null).RenderMode == DocRenderMode.NotAvailable;
    }

    [Property]
    public bool GetImagePreviewQueryHandler_WhenImageExtension_ReturnsPreview(byte selector)
    {
        string extension = Pick(selector, "jpg", "jpeg", "png", "gif", "bmp", "webp");
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = CreateFile(extension, isVideo: false, originalUrl: "https://img.test/a.jpg");
        var handler = new GetImagePreviewQueryHandler(
            context.FileManager,
            context.Gateway,
            CreateEnabledConfig());

        Result<CloudDriveImagePreviewDto> result = handler.Handle(
            new GetImagePreviewQuery(TestUserId, CreatePreviewInput()),
            CancellationToken.None).GetAwaiter().GetResult();

        return result.IsSuccess
            && result.Value.FileType == extension
            && result.Value.IsTemporary
            && context.Gateway.WebApiRequests.Count == 1;
    }

    [Property]
    public bool GetImagePreviewQueryHandler_WhenVideoExtension_ReturnsUnsupported(byte selector)
    {
        string extension = Pick(selector, "mp4", "mkv", "mov");
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = CreateFile(extension, isVideo: true);
        var handler = new GetImagePreviewQueryHandler(
            context.FileManager,
            context.Gateway,
            CreateEnabledConfig());

        Result<CloudDriveImagePreviewDto> result = handler.Handle(
            new GetImagePreviewQuery(TestUserId, CreatePreviewInput()),
            CancellationToken.None).GetAwaiter().GetResult();

        return result.IsFailure && result.Error.Code == "cloud_drive_image_preview_unsupported";
    }

    [Fact]
    public async Task GetDocPreviewQueryHandler_WhenOfficeServiceConfigured_ReturnsThirdPartyUrl()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = CreateFile("docx", isVideo: false);
        context.Gateway.RespondWeb("files/download", new CloudDriveDownloadDto
        {
            DownloadUrl = "https://download.test/a.docx",
            FileName = "a.docx",
        });
        var handler = new GetDocPreviewQueryHandler(
            context.FileManager,
            context.Gateway,
            CreateEnabledConfig("https://office.test/preview"));

        // Act
        Result<CloudDriveDocPreviewDto> result = await handler.Handle(
            new GetDocPreviewQuery(TestUserId, CreatePreviewInput()),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(DocRenderMode.ThirdParty, result.Value.RenderMode);
        Assert.Contains("https%3A%2F%2Fdownload.test%2Fa.docx", result.Value.ThirdPartyPreviewUrl);
        Assert.True(result.Value.IsTemporary);
    }

    [Fact]
    public async Task GetDocPreviewQueryHandler_WhenUnsupportedExtension_ReturnsFailure()
    {
        // Arrange
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = CreateFile("exe", isVideo: false);
        var handler = new GetDocPreviewQueryHandler(
            context.FileManager,
            context.Gateway,
            CreateEnabledConfig());

        // Act
        Result<CloudDriveDocPreviewDto> result = await handler.Handle(
            new GetDocPreviewQuery(TestUserId, CreatePreviewInput()),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_document_preview_unsupported", result.Error.Code);
    }

    private static CloudDriveDocPreviewDto PreviewDocument(string extension, string? officePreviewUrl)
    {
        TestContext context = CreateContext();
        context.Gateway.WebApiResponse = CreateFile(extension.TrimStart('.'), isVideo: false);
        context.Gateway.RespondWeb("files/download", new CloudDriveDownloadDto
        {
            DownloadUrl = $"https://download.test/a.{extension.TrimStart('.')}",
            FileName = $"a.{extension.TrimStart('.')}",
        });
        var handler = new GetDocPreviewQueryHandler(
            context.FileManager,
            context.Gateway,
            CreateEnabledConfig(officePreviewUrl));

        Result<CloudDriveDocPreviewDto> result = handler.Handle(
            new GetDocPreviewQuery(TestUserId, CreatePreviewInput()),
            CancellationToken.None).GetAwaiter().GetResult();

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static string Pick(byte selector, params string[] values)
    {
        return values[selector % values.Length];
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

        return new TestContext(gateway, fileManager);
    }

    private static CloudDriveFileDetailDto CreateFile(string extension, bool isVideo, string? originalUrl = null)
    {
        return new CloudDriveFileDetailDto
        {
            FileId = "file-1",
            Name = $"demo.{extension.TrimStart('.')}",
            FileType = extension.TrimStart('.').ToLowerInvariant(),
            PickCode = "pick-1",
            IsVideo = isVideo,
            OriginalImageUrl = originalUrl,
        };
    }

    private static GetPreviewInput CreatePreviewInput()
    {
        return new GetPreviewInput
        {
            CloudAccountUid = TestCloudAccountUid,
            FileId = "file-1",
        };
    }

    private static IOptions<CloudDriveAppConfig> CreateEnabledConfig(string? officePreviewUrl = null)
    {
        return Options.Create(new CloudDriveAppConfig
        {
            CloudDriveFeatureEnabled = true,
            CacheTtlSeconds = 60,
            OfficePreviewServiceUrl = officePreviewUrl,
        });
    }

    private sealed record TestContext(
        RecordingCloudDriveOpenApiGateway Gateway,
        CloudDriveFileManager FileManager);
}
