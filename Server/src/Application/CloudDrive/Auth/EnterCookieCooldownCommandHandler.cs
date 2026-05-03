using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Auth;

internal sealed class EnterCookieCooldownCommandHandler(
    CookieSessionManager cookieSessionManager,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<EnterCookieCooldownCommand>
{
    public async Task<Result> Handle(
        EnterCookieCooldownCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        await cookieSessionManager.EnterCooldownAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            request.Input.Reason,
            request.Input.CooldownSeconds,
            cancellationToken);

        return Result.Success();
    }
}
