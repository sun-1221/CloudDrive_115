using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class GetDownloadUrlQueryHandler(
    ICloudDriveWebApiGateway openApiGateway,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetDownloadUrlQuery, CloudDriveDownloadDto>
{
    public async Task<Result<CloudDriveDownloadDto>> Handle(
        GetDownloadUrlQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);
        JsonElement response = await openApiGateway.SendWebApiRequestAsync<JsonElement>(
            request.UserId,
            accountContextAccessor.CloudAccountUid,
            HttpMethod.Get,
            "files/download",
            CloudDriveFileHandlerSupport.CreateDownloadContext(request.Input),
            cancellationToken);
        CloudDriveFileHandlerSupport.EnsureSuccessEnvelope(response, "文件下载");
        string downloadUrl = CloudDriveFileHandlerSupport.RequireString(
            response,
            "file_url_302",
            "文件下载");
        return Result.Success(new CloudDriveDownloadDto
        {
            DownloadUrl = downloadUrl,
            FileName = CloudDriveFileHandlerSupport.ReadString(response, "file_name") ?? string.Empty,
            FileSize = CloudDriveFileHandlerSupport.ReadLong(response, "file_size"),
            IsTemporary = true,
        });
    }
}
