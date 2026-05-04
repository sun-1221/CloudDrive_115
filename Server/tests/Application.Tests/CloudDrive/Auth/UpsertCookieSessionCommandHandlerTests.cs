using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;
using Application.Tests.Support;

using Domain.Enums;
using Domain.Events;
using Domain.Exceptions;
using Domain.Repositories;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Auth;

public sealed class UpsertCookieSessionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenFeatureDisabled_ThrowsConfigurationException()
    {
        TestContext context = CreateContext(featureEnabled: false);
        UpsertCookieSessionCommand command = CreateCommand();

        await Assert.ThrowsAsync<CloudDriveConfigurationException>(
            () => context.Handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenValid_UpsertsSessionInvalidatesRootCacheAndResetsRateLimiter()
    {
        TestContext context = CreateContext(featureEnabled: true);
        UpsertCookieSessionCommand command = CreateCommand();

        var result = await context.Handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("115001", result.Value.CloudAccountUid);
        Assert.Equal(CookieSessionStatus.Active, result.Value.Status);
        Assert.Contains((command.UserId, "115001", "0"), context.MetadataCache.InvalidatedDirectories);
        Assert.Contains((command.UserId, "115001", null), context.RateLimiter.ResetCalls);

        Domain.Entities.CookieSession session = (await context.Repository.GetByUserAsync(command.UserId)).Single();
        Assert.Contains(session.DomainEvents, domainEvent => domainEvent is CookieSessionUpserted);
    }

    private static UpsertCookieSessionCommand CreateCommand()
    {
        return new UpsertCookieSessionCommand(
            Guid.NewGuid(),
            new UpsertCookieSessionInput
            {
                CloudAccountUid = "115001",
                Uid = "uid",
                Cid = "cid",
                Ussid = "ussid",
            });
    }

    private static TestContext CreateContext(bool featureEnabled)
    {
        var repository = new FakeCookieSessionRepository();
        var manager = new CookieSessionManager(
            repository,
            new FakeCloudDriveSecretProtector(),
            new FakeCookieInvalidationNotifier(),
            Options.Create(new CloudDriveAppConfig
            {
                CloudDriveFeatureEnabled = featureEnabled,
            }),
            new FakeLogger<CookieSessionManager>());
        var metadataCache = new RecordingMetadataCache();
        var rateLimiter = new RecordingRateLimiter();
        ICommandHandler<UpsertCookieSessionCommand, CloudDriveCookieSessionDto> handler =
            new UpsertCookieSessionCommandHandler(
                manager,
                repository,
                metadataCache,
                rateLimiter,
                Options.Create(new CloudDriveAppConfig
                {
                    CloudDriveFeatureEnabled = featureEnabled,
                }));

        return new TestContext(handler, repository, metadataCache, rateLimiter);
    }

    private sealed record TestContext(
        ICommandHandler<UpsertCookieSessionCommand, CloudDriveCookieSessionDto> Handler,
        ICookieSessionRepository Repository,
        RecordingMetadataCache MetadataCache,
        RecordingRateLimiter RateLimiter);

    private sealed class RecordingMetadataCache : ICloudDriveMetadataCache
    {
        public List<(Guid UserId, string CloudAccountUid, string DirectoryId)> InvalidatedDirectories { get; } = [];

        public async Task<T?> GetOrSetAsync<T>(
            string cacheKey,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan? ttl = null,
            CancellationToken cancellationToken = default)
        {
            return await factory(cancellationToken);
        }

        public Task InvalidateDirectoryAsync(
            Guid userId,
            string cloudAccountUid,
            string directoryId,
            CancellationToken cancellationToken = default)
        {
            InvalidatedDirectories.Add((userId, cloudAccountUid, directoryId));
            return Task.CompletedTask;
        }

        public Task InvalidateFileAsync(
            Guid userId,
            string cloudAccountUid,
            string fileId,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingRateLimiter : ICloudDriveRateLimiter
    {
        public List<(Guid UserId, string CloudAccountUid, string? Category)> ResetCalls { get; } = [];

        public Task<IDisposable> AcquireAsync(
            Guid userId,
            string cloudAccountUid,
            string? category = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IDisposable>(NullDisposable.Instance);
        }

        public void RecordThrottleSignal(Guid userId, string cloudAccountUid, string? category = null) { }

        public void Reset(Guid userId, string cloudAccountUid, string? category = null)
        {
            ResetCalls.Add((userId, cloudAccountUid, category));
        }

        private sealed class NullDisposable : IDisposable
        {
            public static NullDisposable Instance { get; } = new();

            public void Dispose() { }
        }
    }
}
