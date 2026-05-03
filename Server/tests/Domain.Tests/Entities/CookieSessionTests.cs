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
        CookieInvalidated domainEvent = Assert.IsType<CookieInvalidated>(Assert.Single(session.DomainEvents));
        Assert.Equal(TestCloudAccountUid, domainEvent.CloudAccountUid);
        Assert.Equal(reason, domainEvent.Reason);
        Assert.Equal(session.CooldownUntil, domainEvent.CooldownUntil);
    }

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
