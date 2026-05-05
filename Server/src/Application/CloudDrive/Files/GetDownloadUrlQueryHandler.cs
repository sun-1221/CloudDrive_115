using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class GetDownloadUrlQueryHandler(
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetDownloadUrlQuery, CloudDriveDownloadDto>
{
    public Task<Result<CloudDriveDownloadDto>> Handle(
        GetDownloadUrlQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        // 下载地址 API 待补采集，不得使用猜测路径 files/download 调用 115 API
        return Task.FromResult(Result.Failure<CloudDriveDownloadDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("文件下载地址")));
    }
}
