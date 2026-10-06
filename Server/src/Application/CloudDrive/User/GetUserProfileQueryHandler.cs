using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.User;

internal sealed class GetUserProfileQueryHandler(
    UserAggregationService userAggregationService,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetUserProfileQuery, CloudDriveUserProfileDto>
{
    public async Task<Result<CloudDriveUserProfileDto>> Handle(
        GetUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        CloudDriveUserProfileDto profile = await userAggregationService.AggregateUserProfileAsync(
            request.UserId,
            cloudAccountUid,
            cancellationToken);

        return Result.Success(profile);
    }
}
