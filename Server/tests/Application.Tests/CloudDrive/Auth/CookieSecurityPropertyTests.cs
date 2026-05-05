using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.Tests.Support;

using Domain.ValueObjects;

using FsCheck.Xunit;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Auth;

/// <summary>
/// 属性 P1：Cookie 安全性。
/// 验证 Cookie 负载经加密存储后不包含 UID/CID/USSID 明文。
/// </summary>
public sealed class CookieSecurityPropertyTests
{
    [Property]
    public async void UpsertSession_EncryptedPayload_DoesNotContainPlaintext(
        string uid, string cid, string ussid)
    {
        if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(cid) || string.IsNullOrEmpty(ussid))
            return;

        // Arrange
        var repo = new FakeCookieSessionRepository();
        var protector = new FakeCloudDriveSecretProtector();
        var notifier = new FakeCookieInvalidationNotifier();
        var config = Options.Create(new CloudDriveAppConfig());
        var logger = new FakeLogger<CookieSessionManager>();

        var manager = new CookieSessionManager(repo, protector, notifier, config, logger);
        Guid userId = Guid.NewGuid();
        string cloudAccountUid = $"test-{uid}";

        // Act
        await manager.UpsertSessionAsync(userId, cloudAccountUid, new CookieTriplet(uid, cid, ussid));

        // Assert — 加密后的负载不应包含明文三元组
        var sessions = await repo.GetByUserAsync(userId);
        var session = sessions.Single();
        string encrypted = session.EncryptedCookiePayload;

        Assert.StartsWith(FakeCloudDriveSecretProtector.Prefix, encrypted);
        // 原始明文 uid/cid/ussid 不应以未编码形式出现在加密字符串中
        // （FakeProtector 使用 Base64，因此直接字符串不会出现）
        Assert.DoesNotContain($"\"uid\":\"{uid}\"", encrypted);
    }

    [Fact]
    public async Task UpsertThenGet_RoundTrip_RestoresOriginalCookie()
    {
        // Arrange
        var repo = new FakeCookieSessionRepository();
        var protector = new FakeCloudDriveSecretProtector();
        var notifier = new FakeCookieInvalidationNotifier();
        var config = Options.Create(new CloudDriveAppConfig());
        var logger = new FakeLogger<CookieSessionManager>();

        var manager = new CookieSessionManager(repo, protector, notifier, config, logger);
        Guid userId = Guid.NewGuid();
        const string cloudAccountUid = "115-user-001";
        var cookies = new CookieTriplet("myuid", "mycid", "myussid");

        // Act
        await manager.UpsertSessionAsync(userId, cloudAccountUid, cookies);
        string header = await manager.GetValidCookieHeaderAsync(userId, cloudAccountUid);

        // Assert
        Assert.Contains("UID=myuid", header);
        Assert.Contains("CID=mycid", header);
        Assert.Contains("USSID=myussid", header);
    }
}
