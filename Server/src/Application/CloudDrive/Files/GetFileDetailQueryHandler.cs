using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class GetFileDetailQueryHandler(
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetFileDetailQuery, CloudDriveFileDetailDto>
{
    public Task<Result<CloudDriveFileDetailDto>> Handle(
        GetFileDetailQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        // 网页端详情接口（files/file）待补采集，不得使用猜测路径调用 115 API
        return Task.FromResult(Result.Failure<CloudDriveFileDetailDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("文件详情")));
    }
}
