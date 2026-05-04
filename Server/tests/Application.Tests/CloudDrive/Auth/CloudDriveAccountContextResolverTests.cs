using Application.CloudDrive.Auth;
using Application.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Tests.Support;

using Domain.Enums;
using Domain.Results;
using Domain.ValueObjects;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Auth;

public sealed class CloudDriveAccountContextResolverTests
{
    [Fact]
    public async Task ResolveAsync_WhenSingleActiveSession_ReturnsAccountContext()
    {
        var repository = new FakeCookieSessionRepository();
        var manager = CreateManager(repository);
        Guid userId = Guid.NewGuid();
        await manager.UpsertSessionAsync(userId, "115001", new CookieTriplet("uid", "cid", "ussid"));

        var resolver = new CloudDriveAccountContextResolver(repository);

        Result<CloudDriveAccountContext> result = await resolver.ResolveAsync(userId);

        Assert.True(result.IsSuccess);
        Assert.Equal("115001", result.Value.CloudAccountUid);
    }

    [Fact]
    public async Task ResolveAsync_WhenNoActiveSession_ReturnsUnavailableFailure()
    {
        var repository = new FakeCookieSessionRepository();
        var resolver = new CloudDriveAccountContextResolver(repository);

        Result<CloudDriveAccountContext> result = await resolver.ResolveAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_account_context_unavailable", result.Error.Code);
    }

    [Fact]
    public async Task ResolveAsync_WhenMultipleActiveSessions_ReturnsAmbiguousFailure()
    {
        var repository = new FakeCookieSessionRepository();
        var manager = CreateManager(repository);
        Guid userId = Guid.NewGuid();
        await manager.UpsertSessionAsync(userId, "115001", new CookieTriplet("uid1", "cid1", "ussid1"));
        await manager.UpsertSessionAsync(userId, "115002", new CookieTriplet("uid2", "cid2", "ussid2"));

        var resolver = new CloudDriveAccountContextResolver(repository);

        Result<CloudDriveAccountContext> result = await resolver.ResolveAsync(userId);

        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_account_context_ambiguous", result.Error.Code);
    }

    [Fact]
    public async Task ResolveAsync_WhenOnlyRevokedSessionExists_ReturnsUnavailableFailure()
    {
        var repository = new FakeCookieSessionRepository();
        var manager = CreateManager(repository);
        Guid userId = Guid.NewGuid();
        await manager.UpsertSessionAsync(userId, "115001", new CookieTriplet("uid", "cid", "ussid"));
        Domain.Entities.CookieSession session = (await repository.GetByAccountAsync(userId, "115001"))!;
        session.Revoke();
        await repository.UpdateAsync(session);

        var resolver = new CloudDriveAccountContextResolver(repository);

        Result<CloudDriveAccountContext> result = await resolver.ResolveAsync(userId);

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
