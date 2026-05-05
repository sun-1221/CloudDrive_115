using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Preview;

internal sealed class GetDocPreviewQueryHandler(
    CloudDriveFileManager fileManager,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetDocPreviewQuery, CloudDriveDocPreviewDto>
{
    public async Task<Result<CloudDriveDocPreviewDto>> Handle(
        GetDocPreviewQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveAppConfig config = configOptions.Value;
        CloudDriveHandlerGuard.EnsureEnabled(config);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        CloudDriveFileDetailDto? file = await fileManager.GetFileDetailAsync<CloudDriveFileDetailDto?>(
            request.UserId,
            cloudAccountUid,
            request.Input.PickCode,
            CloudDrivePreviewSupport.CreateFileDetailContext(request.Input),
            cancellationToken);

        if (file is null)
        {
            return Result.Failure<CloudDriveDocPreviewDto>(
                CloudDrivePreviewSupport.FileNotFound(request.Input.PickCode));
        }

        string extension = CloudDrivePreviewSupport.ResolveExtension(file);
        if (!CloudDrivePreviewSupport.IsSupportedDocument(extension))
        {
            return Result.Failure<CloudDriveDocPreviewDto>(CloudDrivePreviewSupport.UnsupportedDocumentType);
        }

        bool officeConfigured = !string.IsNullOrWhiteSpace(config.OfficePreviewServiceUrl);
        DocRenderMode renderMode = CloudDrivePreviewSupport.ResolveDocumentRenderMode(extension, officeConfigured);
        if (renderMode == DocRenderMode.NotAvailable && CloudDrivePreviewSupport.IsOfficeDocument(extension))
        {
            return Result.Success(new CloudDriveDocPreviewDto
            {
                FileName = file.Name,
                FileType = extension,
                RenderMode = DocRenderMode.NotAvailable,
                IsTemporary = true,
            });
        }

        // 下载地址 API 待补采集，不得使用猜测路径 files/download 调用 115 API
        return Result.Failure<CloudDriveDocPreviewDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("文档下载地址"));
    }
}
