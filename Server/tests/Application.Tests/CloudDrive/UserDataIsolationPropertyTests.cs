using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.Tests.Support;

using Domain.ValueObjects;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive;

/// <summary>
/// 属性 P10：用户数据隔离。
/// 验证不同 userId/cloudAccountUid 的 Cookie 会话互不影响。
/// </summary>
public sealed class UserDataIsolationPropertyTests
{
    [Fact]
    public async Task CookieSessions_WhenUsersDiffer_HaveIndependentSessions()
    {
        // Arrange
        var repo = new FakeCookieSessionRepository();
        var protector = new FakeCloudDriveSecretProtector();
        var notifier = new FakeCookieInvalidationNotifier();
        var config = Options.Create(new CloudDriveAppConfig());
        var logger = new FakeLogger<CookieSessionManager>();
        var manager = new CookieSessionManager(repo, protector, notifier, config, logger);

        Guid userA = Guid.NewGuid();
        Guid userB = Guid.NewGuid();
        const string account = "shared-account";

        // Act
        await manager.UpsertSessionAsync(userA, account, new CookieTriplet("uidA", "cidA", "ussidA"));
        await manager.UpsertSessionAsync(userB, account, new CookieTriplet("uidB", "cidB", "ussidB"));

        // Assert — 每个用户只看到自己的会话
        var sessionsA = await manager.GetBoundAccountsAsync(userA);
        var sessionsB = await manager.GetBoundAccountsAsync(userB);

        Assert.Single(sessionsA);
        Assert.Single(sessionsB);

        // 验证 Cookie 内容互不干扰
        string headerA = await manager.GetValidCookieHeaderAsync(userA, account);
        string headerB = await manager.GetValidCookieHeaderAsync(userB, account);

        Assert.Contains("UID=uidA", headerA);
        Assert.Contains("UID=uidB", headerB);
        Assert.DoesNotContain("uidB", headerA);
        Assert.DoesNotContain("uidA", headerB);
    }

    [Fact]
    public async Task DifferentAccounts_SameUser_AreIndependent()
    {
        // Arrange
        var repo = new FakeCookieSessionRepository();
        var protector = new FakeCloudDriveSecretProtector();
        var notifier = new FakeCookieInvalidationNotifier();
        var config = Options.Create(new CloudDriveAppConfig());
        var logger = new FakeLogger<CookieSessionManager>();
        var manager = new CookieSessionManager(repo, protector, notifier, config, logger);

        Guid userId = Guid.NewGuid();

        // Act
        await manager.UpsertSessionAsync(userId, "account-1", new CookieTriplet("uid1", "cid1", "ussid1"));
        await manager.UpsertSessionAsync(userId, "account-2", new CookieTriplet("uid2", "cid2", "ussid2"));

        // 使 account-1 失效
        await manager.MarkInvalidatedAsync(userId, "account-1", "test_invalidation");

        // Assert — account-2 不受影响
        bool isAuth1 = await manager.IsAuthorizedAsync(userId, "account-1");
        bool isAuth2 = await manager.IsAuthorizedAsync(userId, "account-2");

        Assert.False(isAuth1);
        Assert.True(isAuth2);
    }
}
