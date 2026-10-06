namespace Domain.Entities;

/// <summary>
/// 本地个人用户聚合根。
/// </summary>
public sealed class User : AggregateRoot<Guid>
{
    /// <summary>
    /// ORM 反序列化构造函数。
    /// </summary>
    public User()
    {
    }

    /// <summary>登录名。</summary>
    public string UserName { get; private set; } = string.Empty;

    /// <summary>PBKDF2 密码哈希，不保存明文密码。</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>创建时间（UTC）。</summary>
    public DateTime CreationTime { get; private set; }

    /// <summary>最后修改时间（UTC）。</summary>
    public DateTime? LastModificationTime { get; private set; }

    /// <summary>创建用户。</summary>
    public static User Create(string userName, string passwordHash)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            PasswordHash = passwordHash,
            CreationTime = DateTime.UtcNow,
        };
    }

}
