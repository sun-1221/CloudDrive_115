using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Preview;

internal sealed class GetImagePreviewQueryHandler(
    CloudDriveFileManager fileManager,
    ICloudDriveOpenApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetImagePreviewQuery, CloudDriveImagePreviewDto>
{
    public async Task<Result<CloudDriveImagePreviewDto>> Handle(
        GetImagePreviewQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveImagePreviewDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        CloudDriveFileDetailDto? file = await fileManager.GetFileDetailAsync<CloudDriveFileDetailDto?>(
            request.UserId,
            request.Input.CloudAccountUid,
            request.Input.FileId,
            CloudDrivePreviewSupport.CreateFileDetailContext(request.Input),
            cancellationToken);

        if (file is null)
        {
            return Result.Failure<CloudDriveImagePreviewDto>(
                CloudDrivePreviewSupport.FileNotFound(request.Input.FileId));
        }

        string extension = CloudDrivePreviewSupport.ResolveExtension(file);
        if (!CloudDrivePreviewSupport.IsImage(extension))
        {
            return Result.Failure<CloudDriveImagePreviewDto>(CloudDrivePreviewSupport.UnsupportedImageType);
        }

        string previewUrl = file.OriginalImageUrl ?? string.Empty;
        if (string.IsNullOrWhiteSpace(previewUrl))
        {
            CloudDriveDownloadDto download = await openApiGateway.SendWebApiRequestAsync<CloudDriveDownloadDto>(
                request.UserId,
                request.Input.CloudAccountUid,
                HttpMethod.Get,
                "files/download",
                CloudDrivePreviewSupport.CreateDownloadContext(file),
                cancellationToken);

            previewUrl = download.DownloadUrl;
        }

        if (string.IsNullOrWhiteSpace(previewUrl))
        {
            return Result.Failure<CloudDriveImagePreviewDto>(
                CloudDriveFileHandlerSupport.InvalidInput(
                    "cloud_drive_image_preview_url_unavailable",
                    "图片预览地址不可用，请稍后重试。"));
        }

        return Result.Success(new CloudDriveImagePreviewDto
        {
            PreviewUrl = previewUrl,
            FileName = file.Name,
            FileType = extension,
            IsTemporary = true,
        });
    }
}
