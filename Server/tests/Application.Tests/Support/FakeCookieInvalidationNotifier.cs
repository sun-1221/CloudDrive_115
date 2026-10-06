using Application.Abstractions.CloudDrive;

namespace Application.Tests.Support;

/// <summary>
/// 测试用失效通知器，记录通知调用。
/// </summary>
internal sealed class FakeCookieInvalidationNotifier : ICookieInvalidationNotifier
{
    private readonly List<(Guid UserId, string CloudAccountUid, string Reason)> _notifications = [];

    public IReadOnlyList<(Guid UserId, string CloudAccountUid, string Reason)> Notifications => _notifications;

    public Task NotifyAsync(Guid userId, string cloudAccountUid, string reason, CancellationToken cancellationToken = default)
    {
        _notifications.Add((userId, cloudAccountUid, reason));
        return Task.CompletedTask;
    }
}
