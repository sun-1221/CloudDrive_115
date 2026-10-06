using Application.CloudDrive.Dtos;
using Application.Queries;

using Domain.Results;

namespace Application.CloudDrive.Share;

internal sealed class GetShareSendsQueryHandler
    : IQueryHandler<GetShareSendsQuery, CloudDriveShareListDto>
{
    public Task<Result<CloudDriveShareListDto>> Handle(
        GetShareSendsQuery request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure<CloudDriveShareListDto>(
            new Error("cloud_drive_api_not_collected", "分享发送接口尚未采集，当前能力保持禁用。")));
    }
}
