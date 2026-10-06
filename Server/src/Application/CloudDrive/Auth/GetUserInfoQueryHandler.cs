using Application.Abstractions.CloudDrive;
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
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetUserInfoQuery, CloudDriveUserInfoDto>
{
    public async Task<Result<CloudDriveUserInfoDto>> Handle(
        GetUserInfoQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        Domain.Entities.CookieSession? session = await cookieSessionRepository.GetByAccountAsync(
            request.UserId,
            cloudAccountUid,
            cancellationToken);

        if (session is null || session.Status != CookieSessionStatus.Active)
        {
            return Result.Failure<CloudDriveUserInfoDto>(new Error(
                "cookie_not_active",
                "Cookie 会话不可用，请由运维重新注入 Cookie。"));
        }

        return Result.Failure<CloudDriveUserInfoDto>(new Error(
            "cloud_drive_api_not_collected",
            "用户基础资料接口尚未采集，当前能力保持禁用。"));
    }
}
