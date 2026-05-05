using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;
using Application.Queries;
using Application.Tests.Support;

using Domain.Enums;
using Domain.Repositories;
using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Auth;

public sealed class CookieSessionMultiAccountContextTests
{
    [Fact]
    public async Task UpsertCookieSessionCommandHandler_WhenMultipleAccounts_IsolatesByUserAndCloudAccount()
    {
        // Arrange
        TestContext context = CreateContext();
        Guid userId = Guid.NewGuid();
        Guid otherUserId = Guid.NewGuid();

        // Act
        await context.UpsertHandler.Handle(CreateUpsertCommand(userId, "115001", "uid-1"), CancellationToken.None);
        await context.UpsertHandler.Handle(CreateUpsertCommand(userId, "115002", "uid-2"), CancellationToken.None);
        await context.UpsertHandler.Handle(CreateUpsertCommand(otherUserId, "115001", "uid-other"), CancellationToken.None);

        // Assert
        string firstHeader = await context.Manager.GetValidCookieHeaderAsync(userId, "115001");
        string secondHeader = await context.Manager.GetValidCookieHeaderAsync(userId, "115002");
        string otherHeader = await context.Manager.GetValidCookieHeaderAsync(otherUserId, "115001");
        Assert.Contains("UID=uid-1", firstHeader);
        Assert.Contains("UID=uid-2", secondHeader);
        Assert.Contains("UID=uid-other", otherHeader);
        Assert.DoesNotContain("uid-2", firstHeader);
        Assert.DoesNotContain("uid-1", secondHeader);
    }

    [Fact]
    public async Task RevokeCookieSessionCommandHandler_WhenMultipleAccounts_AffectsOnlyTargetAccount()
    {
        // Arrange
        TestContext context = CreateContext();
        Guid userId = Guid.NewGuid();
        await context.UpsertHandler.Handle(CreateUpsertCommand(userId, "115001", "uid-1"), CancellationToken.None);
        await context.UpsertHandler.Handle(CreateUpsertCommand(userId, "115002", "uid-2"), CancellationToken.None);
        context.MetadataCache.InvalidatedDirectories.Clear();
        context.RateLimiter.ResetCalls.Clear();

        // Act
        Result revokeResult = await context.RevokeHandler.Handle(
            new RevokeCookieSessionCommand(
                userId,
                new RevokeCookieSessionInput { CloudAccountUid = "115001" }),
            CancellationToken.None);

        // Assert
        Domain.Entities.CookieSession revoked = (await context.Repository.GetByAccountAsync(userId, "115001"))!;
        Domain.Entities.CookieSession active = (await context.Repository.GetByAccountAsync(userId, "115002"))!;
        Assert.True(revokeResult.IsSuccess);
        Assert.Equal(CookieSessionStatus.Revoked, revoked.Status);
        Assert.Equal(CookieSessionStatus.Active, active.Status);
        Assert.Contains((userId, "115001", "0"), context.MetadataCache.InvalidatedDirectories);
        Assert.Contains((userId, "115001", null), context.RateLimiter.ResetCalls);
        Assert.DoesNotContain(context.MetadataCache.InvalidatedDirectories, call => call.CloudAccountUid == "115002");
        Assert.DoesNotContain(context.RateLimiter.ResetCalls, call => call.CloudAccountUid == "115002");
    }

    [Fact]
    public async Task GetBoundAccountsQueryHandler_WhenMultipleUsers_ReturnsOnlyCurrentUserAccounts()
    {
        // Arrange
        TestContext context = CreateContext();
        Guid userId = Guid.NewGuid();
        Guid otherUserId = Guid.NewGuid();
        await context.UpsertHandler.Handle(CreateUpsertCommand(userId, "115001", "uid-1"), CancellationToken.None);
        await context.UpsertHandler.Handle(CreateUpsertCommand(userId, "115002", "uid-2"), CancellationToken.None);
        await context.UpsertHandler.Handle(CreateUpsertCommand(otherUserId, "115999", "uid-other"), CancellationToken.None);

        // Act
        Result<List<CloudDriveAccountSummaryDto>> result = await context.BoundAccountsHandler.Handle(
            new GetBoundAccountsQuery(userId),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["115001", "115002"], result.Value.Select(item => item.CloudAccountUid).Order());
        Assert.DoesNotContain(result.Value, item => item.CloudAccountUid == "115999");
    }

    [Fact]
    public async Task GetUserInfoQueryHandler_WhenAccessorNotSet_ThrowsInvalidOperation()
    {
        // Arrange
        TestContext context = CreateContext();
        Guid userId = Guid.NewGuid();
        await context.UpsertHandler.Handle(CreateUpsertCommand(userId, "115001", "uid-1"), CancellationToken.None);

        // Act & Assert — accessor 未设置上下文时直接抛异常
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.UserInfoHandler.Handle(
                new GetUserInfoQuery(userId),
                CancellationToken.None));
        Assert.Empty(context.Gateway.WebEndpoints);
    }

    private static UpsertCookieSessionCommand CreateUpsertCommand(Guid userId, string cloudAccountUid, string uid)
    {
        return new UpsertCookieSessionCommand(
            userId,
            new UpsertCookieSessionInput
            {
                CloudAccountUid = cloudAccountUid,
                Uid = uid,
                Cid = $"cid-{uid}",
                Ussid = $"ussid-{uid}",
            });
    }

    private static TestContext CreateContext()
    {
        IOptions<CloudDriveAppConfig> config = Options.Create(new CloudDriveAppConfig
        {
            CloudDriveFeatureEnabled = true,
        });
        var repository = new FakeCookieSessionRepository();
        var manager = new CookieSessionManager(
            repository,
            new FakeCloudDriveSecretProtector(),
            new FakeCookieInvalidationNotifier(),
            config,
            new FakeLogger<CookieSessionManager>());
        var metadataCache = new RecordingMetadataCache();
        var rateLimiter = new RecordingRateLimiter();
        var gateway = new RecordingGateway();
        gateway.RespondWeb("files/index_info", """{"state":true,"data":{"all_total":"10 GB"}}""");
        gateway.RespondWeb("user/vip_limit", """{"state":true,"data":{"level_name":"VIP"}}""");
        gateway.RespondUserCenter("proapi/3.0/index.php", """{"state":true,"data":{"user_name":"tester"}}""");
        gateway.RespondUserCenter(".", """{"state":true,"data":{"vip_count":1}}""");
        UserAggregationService userAggregationService = new(
            gateway,
            config,
            new FakeLogger<UserAggregationService>());

        return new TestContext(
            repository,
            manager,
            metadataCache,
            rateLimiter,
            gateway,
            new UpsertCookieSessionCommandHandler(manager, repository, metadataCache, rateLimiter, config),
            new RevokeCookieSessionCommandHandler(manager, metadataCache, rateLimiter, config),
            new GetBoundAccountsQueryHandler(manager, config),
            new GetUserInfoQueryHandler(repository, userAggregationService, new CloudDriveAccountContextAccessor(), config));
    }

    private sealed record TestContext(
        ICookieSessionRepository Repository,
        CookieSessionManager Manager,
        RecordingMetadataCache MetadataCache,
        RecordingRateLimiter RateLimiter,
        RecordingGateway Gateway,
        ICommandHandler<UpsertCookieSessionCommand, CloudDriveCookieSessionDto> UpsertHandler,
        ICommandHandler<RevokeCookieSessionCommand> RevokeHandler,
        IQueryHandler<GetBoundAccountsQuery, List<CloudDriveAccountSummaryDto>> BoundAccountsHandler,
        IQueryHandler<GetUserInfoQuery, CloudDriveUserInfoDto> UserInfoHandler);

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

    private sealed class RecordingGateway : ICloudDriveOpenApiGateway
    {
        private readonly Dictionary<string, JsonElement> _webResponses = new(StringComparer.Ordinal);
        private readonly Dictionary<string, JsonElement> _userCenterResponses = new(StringComparer.Ordinal);

        public List<string> WebEndpoints { get; } = [];

        public void RespondWeb(string endpoint, string json)
        {
            _webResponses[endpoint] = Parse(json);
        }

        public void RespondUserCenter(string endpoint, string json)
        {
            _userCenterResponses[endpoint] = Parse(json);
        }

        public Task<T> SendWebApiRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            HttpMethod method,
            string endpoint,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default)
        {
            WebEndpoints.Add(endpoint);
            return Task.FromResult((T)(object)_webResponses[endpoint]);
        }

        public Task<T> SendUserCenterRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            HttpMethod method,
            string endpoint,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult((T)(object)_userCenterResponses[endpoint]);
        }

        public Task<T> SendOfflineApiRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            HttpMethod method,
            string endpoint,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<T> SendUploadInitRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<T> UploadFileAsync<T>(
            Guid userId,
            string cloudAccountUid,
            string uploadUrl,
            Stream fileStream,
            string fileName,
            IDictionary<string, string>? formFields = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        private static JsonElement Parse(string json)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }
    }
}
