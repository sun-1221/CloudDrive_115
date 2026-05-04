using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.User;

internal sealed class GetUserVipQueryHandler(
    UserAggregationService userAggregationService,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetUserVipQuery, CloudDriveVipInfoDto>
{
    public async Task<Result<CloudDriveVipInfoDto>> Handle(
        GetUserVipQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveVipInfoDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        CloudDriveUserInfoDto userInfo = await userAggregationService.AggregateUserInfoAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            cancellationToken);

        return userInfo.VipInfo is null
            ? Result.Failure<CloudDriveVipInfoDto>(new Error(
                "cloud_drive_user_vip_unavailable",
                "用户 VIP 信息暂不可用。"))
            : Result.Success(userInfo.VipInfo);
    }
}
