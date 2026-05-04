using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.User;

internal sealed class GetUserProfileQueryHandler(
    UserAggregationService userAggregationService,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetUserProfileQuery, CloudDriveUserProfileDto>
{
    public async Task<Result<CloudDriveUserProfileDto>> Handle(
        GetUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveUserProfileDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        CloudDriveUserProfileDto profile = await userAggregationService.AggregateUserProfileAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            cancellationToken);

        return Result.Success(profile);
    }
}
