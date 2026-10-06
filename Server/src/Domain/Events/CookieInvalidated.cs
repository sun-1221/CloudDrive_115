namespace Domain.Events;

/// <summary>
/// Cookie 会话失效领域事件。当 115 Cookie 被探测到失效或主动进入冷却期时发布。
/// </summary>
/// <param name="CloudAccountUid">115 账号唯一标识。</param>
/// <param name="Reason">失效原因。</param>
/// <param name="InvalidatedAt">失效时间（UTC）。</param>
/// <param name="CooldownUntil">冷却期截止时间（UTC）。</param>
public sealed record CookieInvalidated(
    string CloudAccountUid,
    string Reason,
    DateTime InvalidatedAt,
    DateTime? CooldownUntil) : DomainEvent;
