using System.Text.Json;

using Application.Abstractions.CloudDrive;

using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.CloudDrive.Mappings;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Preview;

internal sealed class GetDocPreviewQueryHandler(
    ICloudDriveWebApiGateway openApiGateway,
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

        JsonElement response = await openApiGateway.SendWebApiRequestAsync<JsonElement>(
            request.UserId,
            accountContextAccessor.CloudAccountUid,
            HttpMethod.Get,
            "files/download",
            CloudDrivePreviewSupport.CreateDownloadContext(request.Input),
            cancellationToken);

        CloudDriveDownloadDto download = CloudDrivePreviewSupport.MapDownload(response);
        if (string.IsNullOrWhiteSpace(download.FileName))
        {
            return Result.Failure<CloudDriveDocPreviewDto>(
                CloudDriveFileHandlerSupport.ApiNotCollected("下载文件名"));
        }

        string extension = CloudDrivePreviewSupport.ResolveExtension(new CloudDriveFileDto
        {
            Name = download.FileName,
        });
        if (!CloudDrivePreviewSupport.IsSupportedDocument(extension))
        {
            return Result.Failure<CloudDriveDocPreviewDto>(CloudDrivePreviewSupport.UnsupportedDocumentType);
        }

        bool officeConfigured = !string.IsNullOrWhiteSpace(config.OfficePreviewServiceUrl);
        DocRenderMode renderMode = CloudDrivePreviewSupport.ResolveDocumentRenderMode(extension, officeConfigured);
        if (renderMode == DocRenderMode.NotAvailable && CloudDrivePreviewSupport.IsOfficeDocument(extension))
        {
            return Result.Success(download.ToDto(extension, DocRenderMode.NotAvailable));
        }

        if (string.IsNullOrWhiteSpace(download.DownloadUrl))
        {
            return Result.Failure<CloudDriveDocPreviewDto>(
                CloudDriveFileHandlerSupport.ApiNotCollected("文档下载地址"));
        }

        string? thirdPartyPreviewUrl = CloudDrivePreviewSupport.IsOfficeDocument(extension)
            ? CloudDrivePreviewSupport.BuildThirdPartyPreviewUrl(config.OfficePreviewServiceUrl, download.DownloadUrl)
            : null;

        return Result.Success(download.ToDto(extension, renderMode, thirdPartyPreviewUrl));
    }
}
