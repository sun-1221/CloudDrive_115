using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.User;

internal sealed class GetUserSpaceQueryHandler(
    UserAggregationService userAggregationService,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetUserSpaceQuery, CloudDriveSpaceInfoDto>
{
    public async Task<Result<CloudDriveSpaceInfoDto>> Handle(
        GetUserSpaceQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        CloudDriveUserProfileDto profile = await userAggregationService.AggregateUserProfileAsync(
            request.UserId,
            cloudAccountUid,
            cancellationToken);

        return profile.Storage is null
            ? Result.Failure<CloudDriveSpaceInfoDto>(new Error(
                "cloud_drive_user_space_unavailable",
                "用户空间信息暂不可用。"))
            : Result.Success(profile.Storage);
    }
}
