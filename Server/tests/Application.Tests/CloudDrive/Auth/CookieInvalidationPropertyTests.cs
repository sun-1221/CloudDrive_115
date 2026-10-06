using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.Tests.Support;

using Domain.Enums;
using Domain.Exceptions;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Auth;

/// <summary>
/// 属性 P2：Cookie 失效处理。
/// 验证 Cookie 失效标记、冷却期行为与异常抛出。
/// </summary>
public sealed class CookieInvalidationPropertyTests
{
    /// <summary>
    /// 验证活跃会话标记失效后会设置状态与冷却期。
    /// </summary>
    [Fact]
    public async Task MarkInvalidated_WhenSessionActive_SetsStatusAndCooldown()
    {
        // Arrange
        var (manager, repo, _, _) = CreateTestFixture();
        Guid userId = Guid.NewGuid();
        const string cloudAccountUid = "115-user-001";
        await SeedActiveSession(manager, userId, cloudAccountUid);

        // Act
        await manager.MarkInvalidatedAsync(userId, cloudAccountUid, "login_required");

        // Assert
        var sessions = await repo.GetByUserAsync(userId);
        var session = sessions.Single();
        Assert.Equal(CookieSessionStatus.Invalidated, session.Status);
        Assert.NotNull(session.CooldownUntil);
        Assert.Equal("login_required", session.InvalidationReason);
    }

    /// <summary>
    /// 验证已失效会话获取有效 Cookie 时抛出 Cookie 失效异常。
    /// </summary>
    [Fact]
    public async Task GetValidCookieHeader_WhenInvalidated_ThrowsCookieInvalidatedException()
    {
        // Arrange
        var (manager, _, _, _) = CreateTestFixture();
        Guid userId = Guid.NewGuid();
        const string cloudAccountUid = "115-user-001";
        await SeedActiveSession(manager, userId, cloudAccountUid);
        await manager.MarkInvalidatedAsync(userId, cloudAccountUid, "login_required");

        // Act & Assert
        await Assert.ThrowsAsync<CloudDriveCookieInvalidatedException>(
            () => manager.GetValidCookieHeaderAsync(userId, cloudAccountUid));
    }

    /// <summary>
    /// 验证会话不存在时获取有效 Cookie 会抛出 Cookie 失效异常。
    /// </summary>
    [Fact]
    public async Task GetValidCookieHeader_WhenSessionNotFound_ThrowsCookieInvalidatedException()
    {
        // Arrange
        var (manager, _, _, _) = CreateTestFixture();
        Guid userId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<CloudDriveCookieInvalidatedException>(
            () => manager.GetValidCookieHeaderAsync(userId, "nonexistent"));
    }

    /// <summary>
    /// 验证活跃会话标记失效时会通过通知器发送通知。
    /// </summary>
    [Fact]
    public async Task MarkInvalidated_WhenSessionActive_NotifiesViaNotifier()
    {
        // Arrange
        var (manager, _, notifier, _) = CreateTestFixture();
        Guid userId = Guid.NewGuid();
        const string cloudAccountUid = "115-user-001";
        await SeedActiveSession(manager, userId, cloudAccountUid);

        // Act
        await manager.MarkInvalidatedAsync(userId, cloudAccountUid, "http_401");

        // Assert
        Assert.Single(notifier.Notifications);
        Assert.Equal("http_401", notifier.Notifications[0].Reason);
    }

    /// <summary>
    /// 验证指定冷却秒数时会覆盖默认冷却时长。
    /// </summary>
    [Fact]
    public async Task EnterCooldown_WhenCooldownSecondsProvided_OverridesDefaultCooldownSeconds()
    {
        // Arrange
        var (manager, repo, _, _) = CreateTestFixture();
        Guid userId = Guid.NewGuid();
        const string cloudAccountUid = "115-user-001";
        await SeedActiveSession(manager, userId, cloudAccountUid);

        // Act
        await manager.EnterCooldownAsync(userId, cloudAccountUid, "manual_cooldown", cooldownSeconds: 120);

        // Assert
        var sessions = await repo.GetByUserAsync(userId);
        var session = sessions.Single();
        Assert.NotNull(session.CooldownUntil);

        // 冷却期应至少在当前时间 + 119 秒之后（考虑测试执行时间）
        Assert.True(session.CooldownUntil.Value > DateTime.UtcNow.AddSeconds(100));
    }

    private static (CookieSessionManager Manager, FakeCookieSessionRepository Repo, FakeCookieInvalidationNotifier Notifier, FakeLogger<CookieSessionManager> Logger) CreateTestFixture()
    {
        var repo = new FakeCookieSessionRepository();
        var protector = new FakeCloudDriveSecretProtector();
        var notifier = new FakeCookieInvalidationNotifier();
        var config = Options.Create(new CloudDriveAppConfig());
        var logger = new FakeLogger<CookieSessionManager>();
        var manager = new CookieSessionManager(repo, protector, notifier, config, logger);
        return (manager, repo, notifier, logger);
    }

    private static async Task SeedActiveSession(CookieSessionManager manager, Guid userId, string cloudAccountUid)
    {
        await manager.UpsertSessionAsync(userId, cloudAccountUid,
            new Domain.ValueObjects.CookieTriplet("uid", "cid", "ussid"));
    }
}
