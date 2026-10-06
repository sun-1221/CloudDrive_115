using Domain.Entities;
using Domain.Enums;
using Domain.Events;

namespace Domain.Tests.Entities;

/// <summary>
/// 115 Cookie 会话聚合根测试。
/// </summary>
public sealed class CookieSessionTests
{
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string TestCloudAccountUid = "115-user-001";
    private const string TestEncryptedPayload = "encrypted-cookie-payload";
    private const int TestKeyVersion = 1;

    /// <summary>
    /// 验证活跃会话标记失效时会追加 Cookie 失效领域事件。
    /// </summary>
    [Fact]
    public void MarkInvalidated_WhenSessionIsActive_AppendsCookieInvalidatedEvent()
    {
        // Arrange
        CookieSession session = CreateActiveSession();
        const string reason = "login_required";

        // Act
        session.MarkInvalidated(reason, cooldownSeconds: 60);

        // Assert
        Assert.Equal(CookieSessionStatus.Invalidated, session.Status);
        Assert.Equal(reason, session.InvalidationReason);
        Assert.NotNull(session.CooldownUntil);
        CookieInvalidated domainEvent = Assert.IsType<CookieInvalidated>(
            session.DomainEvents.Single(domainEvent => domainEvent is CookieInvalidated));
        Assert.Equal(TestCloudAccountUid, domainEvent.CloudAccountUid);
        Assert.Equal(reason, domainEvent.Reason);
        Assert.Equal(session.CooldownUntil, domainEvent.CooldownUntil);
    }

    /// <summary>
    /// 验证当前时间早于冷却截止时间时会判定处于冷却期。
    /// </summary>
    [Fact]
    public void IsInCooldown_WhenNowIsBeforeCooldownUntil_ReturnsTrue()
    {
        // Arrange
        CookieSession session = CreateActiveSession();
        session.MarkInvalidated("login_required", cooldownSeconds: 60);
        DateTime utcNow = session.CooldownUntil!.Value.AddSeconds(-1);

        // Act
        bool isInCooldown = session.IsInCooldown(utcNow);

        // Assert
        Assert.True(isInCooldown);
    }

    /// <summary>
    /// 验证活跃会话撤销后状态变为已撤销。
    /// </summary>
    [Fact]
    public void Revoke_WhenSessionIsActive_ChangesStatusToRevoked()
    {
        // Arrange
        CookieSession session = CreateActiveSession();

        // Act
        session.Revoke();

        // Assert
        Assert.Equal(CookieSessionStatus.Revoked, session.Status);
        Assert.NotNull(session.LastModificationTime);
    }

    private static CookieSession CreateActiveSession()
    {
        return CookieSession.Create(
            TestUserId,
            TestCloudAccountUid,
            TestEncryptedPayload,
            TestKeyVersion);
    }
}
