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
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetImagePreviewQuery, CloudDriveImagePreviewDto>
{
    public async Task<Result<CloudDriveImagePreviewDto>> Handle(
        GetImagePreviewQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        CloudDriveFileDetailDto? file = await fileManager.GetFileDetailAsync<CloudDriveFileDetailDto?>(
            request.UserId,
            cloudAccountUid,
            request.Input.PickCode,
            CloudDrivePreviewSupport.CreateFileDetailContext(request.Input),
            cancellationToken);

        if (file is null)
        {
            return Result.Failure<CloudDriveImagePreviewDto>(
                CloudDrivePreviewSupport.FileNotFound(request.Input.PickCode));
        }

        string extension = CloudDrivePreviewSupport.ResolveExtension(file);
        if (!CloudDrivePreviewSupport.IsImage(extension))
        {
            return Result.Failure<CloudDriveImagePreviewDto>(CloudDrivePreviewSupport.UnsupportedImageType);
        }

        string previewUrl = file.OriginalImageUrl ?? string.Empty;
        if (string.IsNullOrWhiteSpace(previewUrl))
        {
            // 下载地址 API 待补采集，不得使用猜测路径 files/download 调用 115 API
            return Result.Failure<CloudDriveImagePreviewDto>(
                CloudDriveFileHandlerSupport.ApiNotCollected("图片下载地址"));
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
