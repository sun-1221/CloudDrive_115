namespace Domain.Events;

/// <summary>
/// Cookie 会话已注入或更新领域事件。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="CloudAccountUid">115 账号唯一标识。</param>
/// <param name="UpsertedAt">注入或更新时间（UTC）。</param>
public sealed record CookieSessionUpserted(
    Guid UserId,
    string CloudAccountUid,
    DateTime UpsertedAt) : DomainEvent;
