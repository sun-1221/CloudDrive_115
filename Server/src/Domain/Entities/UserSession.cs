namespace Domain.Entities;

/// <summary>
/// 本地用户登录会话。JWT 只引用其标识，不承载 115 Cookie。
/// </summary>
public sealed class UserSession : AggregateRoot<Guid>
{
    /// <summary>ORM 反序列化构造函数。</summary>
    public UserSession()
    {
    }

    /// <summary>所属用户。</summary>
    public Guid UserId { get; private set; }

    /// <summary>本登录会话当前选择的 115 账号 UID。</summary>
    public string? ActiveCloudAccountUid { get; private set; }

    /// <summary>会话过期时间（UTC）。</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>撤销时间（UTC）。</summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>创建时间（UTC）。</summary>
    public DateTime CreationTime { get; private set; }

    /// <summary>最后使用时间（UTC）。</summary>
    public DateTime LastSeenAt { get; private set; }

    /// <summary>创建会话。</summary>
    public static UserSession Create(Guid userId, TimeSpan lifetime)
    {
        DateTime now = DateTime.UtcNow;
        return new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreationTime = now,
            LastSeenAt = now,
            ExpiresAt = now.Add(lifetime),
        };
    }

    /// <summary>判断会话是否仍可用。</summary>
    public bool IsActive(DateTime utcNow) => RevokedAt is null && utcNow < ExpiresAt;

    /// <summary>撤销会话。</summary>
    public void Revoke()
    {
        RevokedAt ??= DateTime.UtcNow;
        LastSeenAt = DateTime.UtcNow;
    }

    /// <summary>记录会话使用时间。</summary>
    public void Touch() => LastSeenAt = DateTime.UtcNow;

    /// <summary>设置本登录会话当前账号。</summary>
    public void SelectCloudAccount(string? cloudAccountUid)
    {
        ActiveCloudAccountUid = string.IsNullOrWhiteSpace(cloudAccountUid) ? null : cloudAccountUid;
        Touch();
    }
}
