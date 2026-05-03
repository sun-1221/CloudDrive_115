using Application.CloudDrive.Dtos;
using Application.Queries;

using Domain.Enums;
using Domain.Repositories;
using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Auth;

internal sealed class GetUserInfoQueryHandler(
    ICookieSessionRepository cookieSessionRepository,
    UserAggregationService userAggregationService,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetUserInfoQuery, CloudDriveUserInfoDto>
{
    public async Task<Result<CloudDriveUserInfoDto>> Handle(
        GetUserInfoQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveUserInfoDto>(new Error(
                "cloud_drive_cloud_account_uid_required",
                "CloudAccountUid 不能为空，请指定要查询的 115 账号。"));
        }

        Domain.Entities.CookieSession? session = await cookieSessionRepository.GetByAccountAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            cancellationToken);

        if (session is null || session.Status != CookieSessionStatus.Active)
        {
            return Result.Failure<CloudDriveUserInfoDto>(new Error(
                "cookie_not_active",
                "Cookie 会话不可用，请由运维重新注入 Cookie。"));
        }

        CloudDriveUserInfoDto dto = await userAggregationService.AggregateUserInfoAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            cancellationToken);

        return Result.Success(dto);
    }
}
