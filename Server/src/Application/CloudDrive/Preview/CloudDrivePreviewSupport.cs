using System.Text.Json;

using Application.Abstractions.CloudDrive;

using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;

using Domain.Results;

namespace Application.CloudDrive.Preview;

internal static class CloudDrivePreviewSupport
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "jpg",
        "jpeg",
        "png",
        "gif",
        "bmp",
        "webp",
    };

    private static readonly HashSet<string> DirectDocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "pdf",
        "txt",
    };

    private static readonly HashSet<string> OfficeDocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "doc",
        "docx",
        "xls",
        "xlsx",
        "ppt",
        "pptx",
    };

    public static readonly Error UnsupportedImageType = new(
        "cloud_drive_image_preview_unsupported",
        "当前文件类型不支持图片预览。");

    public static readonly Error UnsupportedDocumentType = new(
        "cloud_drive_document_preview_unsupported",
        "当前文件类型不支持文档预览。");

    public static Error FileNotFound(string pickCode) => CloudDriveFileHandlerSupport.FileNotFound(pickCode);

    public static CloudDriveApiContext CreateFileDetailContext(GetPreviewInput input)
    {
        return CloudDriveFileHandlerSupport.CreateFileDetailContext(new GetFileInput
        {
            FileId = input.PickCode,
        });
    }

    public static CloudDriveApiContext CreateDownloadContext(CloudDriveFileDetailDto file)
    {
        string pickCode = file.PickCode ?? file.FileId;

        return CloudDriveFileHandlerSupport.CreateDownloadContext(new GetDownloadUrlInput
        {
            PickCode = pickCode,
        });
    }

    public static CloudDriveApiContext CreateDownloadContext(GetPreviewInput input)
    {
        return CloudDriveFileHandlerSupport.CreateDownloadContext(new GetDownloadUrlInput
        {
            PickCode = input.PickCode,
        });
    }

    public static CloudDriveDownloadDto MapDownload(JsonElement response)
    {
        JsonElement payload = CloudDriveFileHandlerSupport.Payload(response);
        return new CloudDriveDownloadDto
        {
            DownloadUrl = CloudDriveFileHandlerSupport.ReadString(payload, "file_url_302") ?? string.Empty,
            FileName = CloudDriveFileHandlerSupport.ReadString(payload, "file_name") ?? string.Empty,
            FileSize = CloudDriveFileHandlerSupport.ReadLong(payload, "file_size"),
            IsTemporary = true,
        };
    }

    public static string ResolveExtension(CloudDriveFileDto file)
    {
        if (!string.IsNullOrWhiteSpace(file.FileType))
        {
            return NormalizeExtension(file.FileType);
        }

        return NormalizeExtension(Path.GetExtension(file.Name));
    }

    public static bool IsImage(string extension) => ImageExtensions.Contains(extension);

    public static DocRenderMode ResolveDocumentRenderMode(string extension, bool officePreviewConfigured)
    {
        if (DirectDocumentExtensions.Contains(extension))
        {
            return DocRenderMode.Direct;
        }

        if (OfficeDocumentExtensions.Contains(extension))
        {
            return officePreviewConfigured ? DocRenderMode.ThirdParty : DocRenderMode.NotAvailable;
        }

        return DocRenderMode.NotAvailable;
    }

    public static bool IsSupportedDocument(string extension)
    {
        return DirectDocumentExtensions.Contains(extension) || OfficeDocumentExtensions.Contains(extension);
    }

    public static bool IsOfficeDocument(string extension) => OfficeDocumentExtensions.Contains(extension);

    public static string? BuildThirdPartyPreviewUrl(string? officePreviewServiceUrl, string downloadUrl)
    {
        if (string.IsNullOrWhiteSpace(officePreviewServiceUrl) || string.IsNullOrWhiteSpace(downloadUrl))
        {
            return null;
        }

        string separator = officePreviewServiceUrl.Contains('?') ? "&" : "?";
        return $"{officePreviewServiceUrl}{separator}url={Uri.EscapeDataString(downloadUrl)}";
    }

    private static string NormalizeExtension(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().TrimStart('.').ToLowerInvariant();
    }
}
