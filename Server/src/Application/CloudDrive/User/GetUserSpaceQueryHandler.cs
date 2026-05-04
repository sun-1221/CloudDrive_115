using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.User;

internal sealed class GetUserSpaceQueryHandler(
    UserAggregationService userAggregationService,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetUserSpaceQuery, CloudDriveSpaceInfoDto>
{
    public async Task<Result<CloudDriveSpaceInfoDto>> Handle(
        GetUserSpaceQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveSpaceInfoDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        CloudDriveUserInfoDto userInfo = await userAggregationService.AggregateUserInfoAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            cancellationToken);

        return userInfo.SpaceInfo is null
            ? Result.Failure<CloudDriveSpaceInfoDto>(new Error(
                "cloud_drive_user_space_unavailable",
                "用户空间信息暂不可用。"))
            : Result.Success(userInfo.SpaceInfo);
    }
}
