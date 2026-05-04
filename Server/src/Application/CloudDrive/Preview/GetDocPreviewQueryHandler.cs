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
    ICloudDriveOpenApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetDocPreviewQuery, CloudDriveDocPreviewDto>
{
    public async Task<Result<CloudDriveDocPreviewDto>> Handle(
        GetDocPreviewQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveAppConfig config = configOptions.Value;
        CloudDriveHandlerGuard.EnsureEnabled(config);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveDocPreviewDto>(
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
            return Result.Failure<CloudDriveDocPreviewDto>(
                CloudDrivePreviewSupport.FileNotFound(request.Input.FileId));
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

        CloudDriveDownloadDto download = await openApiGateway.SendWebApiRequestAsync<CloudDriveDownloadDto>(
            request.UserId,
            request.Input.CloudAccountUid,
            HttpMethod.Get,
            "files/download",
            CloudDrivePreviewSupport.CreateDownloadContext(file),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(download.DownloadUrl))
        {
            return Result.Failure<CloudDriveDocPreviewDto>(
                CloudDriveFileHandlerSupport.InvalidInput(
                    "cloud_drive_document_preview_url_unavailable",
                    "文档预览地址不可用，请稍后重试。"));
        }

        return Result.Success(new CloudDriveDocPreviewDto
        {
            PreviewUrl = download.DownloadUrl,
            FileName = file.Name,
            FileType = extension,
            RenderMode = renderMode,
            ThirdPartyPreviewUrl = renderMode == DocRenderMode.ThirdParty
                ? CloudDrivePreviewSupport.BuildThirdPartyPreviewUrl(config.OfficePreviewServiceUrl, download.DownloadUrl)
                : null,
            IsTemporary = true,
        });
    }
}
