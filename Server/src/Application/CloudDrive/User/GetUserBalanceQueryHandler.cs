using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.User;

internal sealed class GetUserBalanceQueryHandler(
    UserAggregationService userAggregationService,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetUserBalanceQuery, CloudDriveBalanceDto>
{
    public async Task<Result<CloudDriveBalanceDto>> Handle(
        GetUserBalanceQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        CloudDriveUserProfileDto profile = await userAggregationService.AggregateUserProfileAsync(
            request.UserId,
            cloudAccountUid,
            cancellationToken);

        return profile.Balance is null
            ? Result.Failure<CloudDriveBalanceDto>(new Error(
                "cloud_drive_user_balance_unavailable",
                "用户余额信息暂不可用。"))
            : Result.Success(profile.Balance);
    }
}
