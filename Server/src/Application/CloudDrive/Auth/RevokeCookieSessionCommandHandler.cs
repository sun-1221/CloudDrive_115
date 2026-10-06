using Application.Abstractions.CloudDrive;
using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Auth;

internal sealed class RevokeCookieSessionCommandHandler(
    CookieSessionManager cookieSessionManager,
    ICloudDriveMetadataCache metadataCache,
    ICloudDriveRateLimiter rateLimiter,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<RevokeCookieSessionCommand>
{
    public async Task<Result> Handle(
        RevokeCookieSessionCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure(new Error(
                "cloud_drive_cloud_account_uid_required",
                "CloudAccountUid 不能为空，请指定要撤销的 115 账号。"));
        }

        await cookieSessionManager.RevokeAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            cancellationToken);

        await metadataCache.InvalidateDirectoryAsync(request.UserId, request.Input.CloudAccountUid, directoryId: "0", cancellationToken: cancellationToken);

        rateLimiter.Reset(request.UserId, request.Input.CloudAccountUid);

        return Result.Success();
    }
}
