using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Tests.Support;

using Domain.Enums;
using Domain.Results;
using Domain.ValueObjects;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Auth;

/// <summary>
/// 115 网盘账号上下文解析器测试。
/// </summary>
public sealed class CloudDriveAccountContextResolverTests
{
    /// <summary>
    /// 验证仅存在单个活跃会话时返回账号上下文。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_WhenSingleActiveSession_ReturnsAccountContext()
    {
        // Arrange（准备）
        var repository = new FakeCookieSessionRepository();
        var manager = CreateManager(repository);
        Guid userId = Guid.NewGuid();
        await manager.UpsertSessionAsync(userId, "115001", new CookieTriplet("uid", "cid", "ussid"));

        var resolver = new CloudDriveAccountContextResolver(repository);

        // Act（执行）
        Result<CloudDriveAccountContext> result = await resolver.ResolveAsync(userId);

        // Assert（断言）
        Assert.True(result.IsSuccess);
        Assert.Equal("115001", result.Value.CloudAccountUid);
    }

    /// <summary>
    /// 验证不存在活跃会话时返回上下文不可用失败。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_WhenNoActiveSession_ReturnsUnavailableFailure()
    {
        // Arrange（准备）
        var repository = new FakeCookieSessionRepository();
        var resolver = new CloudDriveAccountContextResolver(repository);

        // Act（执行）
        Result<CloudDriveAccountContext> result = await resolver.ResolveAsync(Guid.NewGuid());

        // Assert（断言）
        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_account_context_unavailable", result.Error.Code);
    }

    /// <summary>
    /// 验证存在多个活跃会话时返回上下文歧义失败。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_WhenMultipleActiveSessions_ReturnsAmbiguousFailure()
    {
        // Arrange（准备）
        var repository = new FakeCookieSessionRepository();
        var manager = CreateManager(repository);
        Guid userId = Guid.NewGuid();
        await manager.UpsertSessionAsync(userId, "115001", new CookieTriplet("uid1", "cid1", "ussid1"));
        await manager.UpsertSessionAsync(userId, "115002", new CookieTriplet("uid2", "cid2", "ussid2"));

        var resolver = new CloudDriveAccountContextResolver(repository);

        // Act（执行）
        Result<CloudDriveAccountContext> result = await resolver.ResolveAsync(userId);

        // Assert（断言）
        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_account_context_ambiguous", result.Error.Code);
    }

    /// <summary>
    /// 验证仅存在已撤销会话时返回上下文不可用失败。
    /// </summary>
    [Fact]
    public async Task ResolveAsync_WhenOnlyRevokedSessionExists_ReturnsUnavailableFailure()
    {
        // Arrange（准备）
        var repository = new FakeCookieSessionRepository();
        var manager = CreateManager(repository);
        Guid userId = Guid.NewGuid();
        await manager.UpsertSessionAsync(userId, "115001", new CookieTriplet("uid", "cid", "ussid"));
        Domain.Entities.CookieSession session = (await repository.GetByAccountAsync(userId, "115001"))!;
        session.Revoke();
        await repository.UpdateAsync(session);

        var resolver = new CloudDriveAccountContextResolver(repository);

        // Act（执行）
        Result<CloudDriveAccountContext> result = await resolver.ResolveAsync(userId);

        // Assert（断言）
        Assert.True(result.IsFailure);
        Assert.Equal(CookieSessionStatus.Revoked, session.Status);
        Assert.Equal("cloud_drive_account_context_unavailable", result.Error.Code);
    }

    private static CookieSessionManager CreateManager(FakeCookieSessionRepository repository)
    {
        return new CookieSessionManager(
            repository,
            new FakeCloudDriveSecretProtector(),
            new FakeCookieInvalidationNotifier(),
            Options.Create(new CloudDriveAppConfig { CloudDriveFeatureEnabled = true }),
            new FakeLogger<CookieSessionManager>());
    }
}
