using Domain.Enums;
using Domain.Events;

namespace Domain.Entities;

/// <summary>
/// 115 Cookie 会话聚合根。存储用户的 115 网页端 Cookie 认证信息（UID/CID/USSID + 可选额外字段），
/// 三元组经加密后持久化至 <see cref="EncryptedCookiePayload"/>，原文不暴露为实体属性。
/// </summary>
/// <remarks>
/// <para>
/// Cookie 由运维/管理员通过受保护端点注入，系统不主动发起授权流程。
/// 失效后仅标记状态、进入冷却期并发布 <c>CookieInvalidated</c>，不尝试自动恢复。
/// </para>
/// <para>
/// 保留 <c>public</c> 无参构造函数供 SqlSugar ORM 反序列化使用，业务代码通过领域方法操作状态。
/// </para>
/// </remarks>
public sealed class CookieSession : AggregateRoot<Guid>
{
    /// <summary>
    /// ORM 反序列化专用。业务代码请使用 <see cref="Create"/> 工厂方法。
    /// </summary>
    public CookieSession()
    {
    }

    /// <summary>
    /// 系统用户 ID。
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// 115 账号唯一标识（UID）。
    /// </summary>
    public string CloudAccountUid { get; private set; } = string.Empty;

    /// <summary>
    /// 加密后的 Cookie 认证负载（JSON 序列化的 <c>{ uid, cid, ussid, extraFields }</c>）。
    /// </summary>
    public string EncryptedCookiePayload { get; private set; } = string.Empty;

    /// <summary>
    /// 加密密钥版本（用于 key ring 轮换时的向后兼容解密）。
    /// </summary>
    public int KeyVersion { get; private set; }

    /// <summary>
    /// 会话状态。
    /// </summary>
    public CookieSessionStatus Status { get; private set; } = CookieSessionStatus.Active;

    /// <summary>
    /// 失效原因描述（仅 <see cref="CookieSessionStatus.Invalidated"/> 时有值）。
    /// </summary>
    public string? InvalidationReason { get; private set; }

    /// <summary>
    /// 冷却期截止时间（UTC）。
    /// </summary>
    public DateTime? CooldownUntil { get; private set; }

    /// <summary>
    /// 上次登录态探测时间（UTC）。
    /// </summary>
    public DateTime? LastProbedAt { get; private set; }

    /// <summary>
    /// 创建（首次注入）时间（UTC）。
    /// </summary>
    public DateTime CreationTime { get; private set; }

    /// <summary>
    /// 最后修改时间（UTC）。
    /// </summary>
    public DateTime? LastModificationTime { get; private set; }

    /// <summary>
    /// 创建新的 Cookie 会话。
    /// </summary>
    /// <param name="userId">系统用户 ID。</param>
    /// <param name="cloudAccountUid">115 账号唯一标识。</param>
    /// <param name="encryptedPayload">加密后的 Cookie 负载。</param>
    /// <param name="keyVersion">加密密钥版本。</param>
    /// <returns>新创建的 <see cref="CookieSession"/> 实例。</returns>
    public static CookieSession Create(
        Guid userId,
        string cloudAccountUid,
        string encryptedPayload,
        int keyVersion)
    {
        var session = new CookieSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CloudAccountUid = cloudAccountUid,
            EncryptedCookiePayload = encryptedPayload,
            KeyVersion = keyVersion,
            Status = CookieSessionStatus.Active,
            CreationTime = DateTime.UtcNow,
        };

        session.RaiseDomainEvent(new CookieSessionUpserted(
            userId,
            cloudAccountUid,
            session.CreationTime));

        return session;
    }

    /// <summary>
    /// 更新加密后的 Cookie 负载（运维重新注入时调用）。
    /// </summary>
    /// <param name="encryptedPayload">新的加密负载。</param>
    /// <param name="keyVersion">新的密钥版本。</param>
    public void UpdatePayload(string encryptedPayload, int keyVersion)
    {
        EncryptedCookiePayload = encryptedPayload;
        KeyVersion = keyVersion;
        Status = CookieSessionStatus.Active;
        InvalidationReason = null;
        CooldownUntil = null;
        LastModificationTime = DateTime.UtcNow;

        RaiseDomainEvent(new CookieSessionUpserted(
            UserId,
            CloudAccountUid,
            LastModificationTime.Value));
    }

    /// <summary>
    /// 标记 Cookie 失效并进入冷却期。
    /// </summary>
    /// <param name="reason">失效原因。</param>
    /// <param name="cooldownSeconds">冷却秒数（默认 60）。</param>
    public void MarkInvalidated(string reason, int cooldownSeconds = 60)
    {
        if (Status == CookieSessionStatus.Revoked)
        {
            return;
        }

        Status = CookieSessionStatus.Invalidated;
        InvalidationReason = reason;
        CooldownUntil = DateTime.UtcNow.AddSeconds(cooldownSeconds);
        LastModificationTime = DateTime.UtcNow;

        RaiseDomainEvent(new CookieInvalidated(
            CloudAccountUid,
            reason,
            DateTime.UtcNow,
            CooldownUntil));
    }

    /// <summary>
    /// 进入冷却期（可覆盖默认冷却时长）。
    /// </summary>
    /// <param name="cooldownSeconds">冷却秒数。</param>
    public void EnterCooldown(int cooldownSeconds)
        => EnterCooldown("cooldown_entered", cooldownSeconds);

    /// <summary>
    /// 标记 Cookie 失效并进入冷却期（可覆盖默认冷却时长）。
    /// </summary>
    /// <param name="reason">失效原因。</param>
    /// <param name="cooldownSeconds">冷却秒数。</param>
    public void EnterCooldown(string reason, int cooldownSeconds)
    {
        if (Status == CookieSessionStatus.Revoked)
        {
            return;
        }

        DateTime utcNow = DateTime.UtcNow;
        string effectiveReason = string.IsNullOrWhiteSpace(reason)
            ? "cooldown_entered"
            : reason;

        Status = CookieSessionStatus.Invalidated;
        InvalidationReason = effectiveReason;
        CooldownUntil = utcNow.AddSeconds(cooldownSeconds);
        LastModificationTime = utcNow;

        RaiseDomainEvent(new CookieInvalidated(
            CloudAccountUid,
            effectiveReason,
            utcNow,
            CooldownUntil));
    }

    /// <summary>
    /// 判断当前是否处于冷却期内。
    /// </summary>
    /// <param name="utcNow">当前 UTC 时间。</param>
    /// <returns>处于冷却期内返回 <see langword="true"/>。</returns>
    public bool IsInCooldown(DateTime utcNow)
    {
        return CooldownUntil.HasValue && utcNow < CooldownUntil.Value;
    }

    /// <summary>
    /// 主动撤销（运维解绑）。
    /// </summary>
    public void Revoke()
    {
        Status = CookieSessionStatus.Revoked;
        LastModificationTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 刷新登录态探测时间戳。
    /// </summary>
    /// <param name="timestamp">探测时间（UTC）。</param>
    public void RefreshProbe(DateTime timestamp)
    {
        LastProbedAt = timestamp;
        LastModificationTime = DateTime.UtcNow;
    }
}
