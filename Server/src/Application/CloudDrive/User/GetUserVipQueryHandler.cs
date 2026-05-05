using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.User;

internal sealed class GetUserVipQueryHandler(
    UserAggregationService userAggregationService,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetUserVipQuery, CloudDriveVipInfoDto>
{
    public async Task<Result<CloudDriveVipInfoDto>> Handle(
        GetUserVipQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        CloudDriveUserProfileDto profile = await userAggregationService.AggregateUserProfileAsync(
            request.UserId,
            cloudAccountUid,
            cancellationToken);

        return profile.Vip is null
            ? Result.Failure<CloudDriveVipInfoDto>(new Error(
                "cloud_drive_user_vip_unavailable",
                "用户 VIP 信息暂不可用。"))
            : Result.Success(profile.Vip);
    }
}
