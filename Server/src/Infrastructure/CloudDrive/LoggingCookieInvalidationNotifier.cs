using Application.Abstractions.CloudDrive;

using Microsoft.Extensions.Logging;

namespace Infrastructure.CloudDrive;

internal sealed class LoggingCookieInvalidationNotifier(
    ICloudDriveSensitiveFieldsFilter sensitiveFieldsFilter,
    ILogger<LoggingCookieInvalidationNotifier> logger) : ICookieInvalidationNotifier
{
    public Task NotifyAsync(
        Guid userId,
        string cloudAccountUid,
        string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        logger.LogWarning(
            "115 Cookie 会话失效 UserId={UserId}, CloudAccountUid={CloudAccountUid}, Reason={Reason}",
            userId,
            sensitiveFieldsFilter.Redact(cloudAccountUid),
            sensitiveFieldsFilter.Redact(reason));

        return Task.CompletedTask;
    }
}
