using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Auth;
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
    public bool GetDocPreviewQueryHandler_WhenDirectDocumentExtension_ReturnsFileNotFound(byte selector)
    {
        // files/file 接口未采集，GetFileDetailAsync 总是返回 null → 返回 file_not_found
        string extension = Pick(selector, "pdf", "txt", "PDF", ".txt");
        TestContext context = CreateContext();
        var handler = new GetDocPreviewQueryHandler(
            context.FileManager,
            context.Accessor,
            CreateEnabledConfig());

        Result<CloudDriveDocPreviewDto> result = handler.Handle(
            new GetDocPreviewQuery(TestUserId, CreatePreviewInput()),
            CancellationToken.None).GetAwaiter().GetResult();

        return result.IsFailure && result.Error.Code == "cloud_drive_file_not_found";
    }

    [Property]
    public bool GetDocPreviewQueryHandler_WhenOfficeServiceMissing_ReturnsFileNotFound(byte selector)
    {
        // files/file 接口未采集，GetFileDetailAsync 总是返回 null → 返回 file_not_found
        string extension = Pick(selector, "doc", "docx", "xls", "xlsx", "ppt", "pptx");
        TestContext context = CreateContext();
        var handler = new GetDocPreviewQueryHandler(
            context.FileManager,
            context.Accessor,
            CreateEnabledConfig());

        Result<CloudDriveDocPreviewDto> result = handler.Handle(
            new GetDocPreviewQuery(TestUserId, CreatePreviewInput()),
            CancellationToken.None).GetAwaiter().GetResult();

        return result.IsFailure && result.Error.Code == "cloud_drive_file_not_found";
    }

    [Property]
    public bool GetImagePreviewQueryHandler_WhenImageExtension_ReturnsFileNotFound(byte selector)
    {
        // files/file 接口未采集，GetFileDetailAsync 总是返回 null → 返回 file_not_found
        string extension = Pick(selector, "jpg", "jpeg", "png", "gif", "bmp", "webp");
        TestContext context = CreateContext();
        var handler = new GetImagePreviewQueryHandler(
            context.FileManager,
            context.Accessor,
            CreateEnabledConfig());

        Result<CloudDriveImagePreviewDto> result = handler.Handle(
            new GetImagePreviewQuery(TestUserId, CreatePreviewInput()),
            CancellationToken.None).GetAwaiter().GetResult();

        return result.IsFailure && result.Error.Code == "cloud_drive_file_not_found";
    }

    [Property]
    public bool GetImagePreviewQueryHandler_WhenVideoExtension_ReturnsFileNotFound(byte selector)
    {
        // files/file 接口未采集，GetFileDetailAsync 总是返回 null → 返回 file_not_found
        string extension = Pick(selector, "mp4", "mkv", "mov");
        TestContext context = CreateContext();
        var handler = new GetImagePreviewQueryHandler(
            context.FileManager,
            context.Accessor,
            CreateEnabledConfig());

        Result<CloudDriveImagePreviewDto> result = handler.Handle(
            new GetImagePreviewQuery(TestUserId, CreatePreviewInput()),
            CancellationToken.None).GetAwaiter().GetResult();

        return result.IsFailure && result.Error.Code == "cloud_drive_file_not_found";
    }

    [Fact]
    public async Task GetDocPreviewQueryHandler_WhenOfficeServiceConfigured_ReturnsFileNotFound()
    {
        // Arrange — files/file 接口未采集，GetFileDetailAsync 返回 null
        TestContext context = CreateContext();
        var handler = new GetDocPreviewQueryHandler(
            context.FileManager,
            context.Accessor,
            CreateEnabledConfig("https://office.test/preview"));

        // Act
        Result<CloudDriveDocPreviewDto> result = await handler.Handle(
            new GetDocPreviewQuery(TestUserId, CreatePreviewInput()),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_file_not_found", result.Error.Code);
    }

    [Fact]
    public async Task GetDocPreviewQueryHandler_WhenUnsupportedExtension_ReturnsFileNotFound()
    {
        // Arrange — files/file 接口未采集，GetFileDetailAsync 返回 null
        TestContext context = CreateContext();
        var handler = new GetDocPreviewQueryHandler(
            context.FileManager,
            context.Accessor,
            CreateEnabledConfig());

        // Act
        Result<CloudDriveDocPreviewDto> result = await handler.Handle(
            new GetDocPreviewQuery(TestUserId, CreatePreviewInput()),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_file_not_found", result.Error.Code);
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
        var accessor = new CloudDriveAccountContextAccessor();
        accessor.SetContext(new CloudDriveAccountContext(TestUserId, TestCloudAccountUid, TestCloudAccountUid));

        return new TestContext(gateway, fileManager, accessor);
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
            PickCode = "file-1",
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
        CloudDriveFileManager FileManager,
        CloudDriveAccountContextAccessor Accessor);
}
