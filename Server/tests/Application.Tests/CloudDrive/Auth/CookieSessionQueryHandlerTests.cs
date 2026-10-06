using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;
using Application.Queries;
using Application.Tests.Support;

using Domain.Enums;
using Domain.Results;
using Domain.ValueObjects;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Auth;

/// <summary>
/// Cookie 会话查询与撤销处理器测试。
/// </summary>
public sealed class CookieSessionQueryHandlerTests
{
    /// <summary>
    /// 验证绑定账号查询仅返回当前用户的账号摘要。
    /// </summary>
    [Fact]
    public async Task GetBoundAccountsQueryHandler_WhenMultipleUsers_ReturnsOnlyCurrentUserSummaries()
    {
        // Arrange
        var repository = new FakeCookieSessionRepository();
        CookieSessionManager manager = CreateManager(repository);
        Guid userId = Guid.NewGuid();
        Guid otherUserId = Guid.NewGuid();
        await manager.UpsertSessionAsync(userId, "115001", new CookieTriplet("uid-1", "cid-1", "ussid-1"));
        await manager.UpsertSessionAsync(userId, "115002", new CookieTriplet("uid-2", "cid-2", "ussid-2"));
        await manager.UpsertSessionAsync(otherUserId, "115999", new CookieTriplet("uid-9", "cid-9", "ussid-9"));

        IQueryHandler<GetBoundAccountsQuery, List<CloudDriveAccountSummaryDto>> handler =
            new GetBoundAccountsQueryHandler(manager, CreateEnabledConfig());

        // Act
        Result<List<CloudDriveAccountSummaryDto>> result = await handler.Handle(
            new GetBoundAccountsQuery(userId),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["115001", "115002"], result.Value.Select(item => item.CloudAccountUid).Order());
        Assert.All(result.Value, item => Assert.Equal(CookieSessionStatus.Active, item.Status));
        Assert.All(result.Value, item => Assert.NotEqual(default, item.CreationTime));
        Assert.Null(typeof(CloudDriveAccountSummaryDto).GetProperty("EncryptedCookiePayload"));
    }

    /// <summary>
    /// 验证撤销 Cookie 会话仅影响目标账号并清理本地状态。
    /// </summary>
    [Fact]
    public async Task RevokeCookieSessionCommandHandler_WhenValid_RevokesOnlyTargetAccountAndClearsLocalState()
    {
        // Arrange
        var repository = new FakeCookieSessionRepository();
        CookieSessionManager manager = CreateManager(repository);
        Guid userId = Guid.NewGuid();
        await manager.UpsertSessionAsync(userId, "115001", new CookieTriplet("uid-1", "cid-1", "ussid-1"));
        await manager.UpsertSessionAsync(userId, "115002", new CookieTriplet("uid-2", "cid-2", "ussid-2"));
        var metadataCache = new RecordingMetadataCache();
        var rateLimiter = new RecordingRateLimiter();
        ICommandHandler<RevokeCookieSessionCommand> handler = new RevokeCookieSessionCommandHandler(
            manager,
            metadataCache,
            rateLimiter,
            CreateEnabledConfig());

        // Act
        Result result = await handler.Handle(
            new RevokeCookieSessionCommand(
                userId,
                new RevokeCookieSessionInput { CloudAccountUid = "115001" }),
            CancellationToken.None);

        // Assert
        Domain.Entities.CookieSession target = (await repository.GetByAccountAsync(userId, "115001"))!;
        Domain.Entities.CookieSession untouched = (await repository.GetByAccountAsync(userId, "115002"))!;
        Assert.True(result.IsSuccess);
        Assert.Equal(CookieSessionStatus.Revoked, target.Status);
        Assert.Equal(CookieSessionStatus.Active, untouched.Status);
        Assert.Equal([(userId, "115001", "0")], metadataCache.InvalidatedDirectories);
        Assert.Equal([(userId, "115001", null)], rateLimiter.ResetCalls);
    }

    /// <summary>
    /// 验证目标云盘账号缺失时撤销命令返回失败且无副作用。
    /// </summary>
    [Fact]
    public async Task RevokeCookieSessionCommandHandler_WhenCloudAccountMissing_ReturnsFailureWithoutSideEffects()
    {
        // Arrange
        var repository = new FakeCookieSessionRepository();
        CookieSessionManager manager = CreateManager(repository);
        var metadataCache = new RecordingMetadataCache();
        var rateLimiter = new RecordingRateLimiter();
        ICommandHandler<RevokeCookieSessionCommand> handler = new RevokeCookieSessionCommandHandler(
            manager,
            metadataCache,
            rateLimiter,
            CreateEnabledConfig());

        // Act
        Result result = await handler.Handle(
            new RevokeCookieSessionCommand(
                Guid.NewGuid(),
                new RevokeCookieSessionInput { CloudAccountUid = " " }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_cloud_account_uid_required", result.Error.Code);
        Assert.Empty(metadataCache.InvalidatedDirectories);
        Assert.Empty(rateLimiter.ResetCalls);
    }

    /// <summary>
    /// 验证活跃会话可聚合并返回用户信息。
    /// </summary>
    [Fact]
    public async Task GetUserInfoQueryHandler_WhenSessionActive_ReturnsAggregatedUserInfo()
    {
        // Arrange
        var repository = new FakeCookieSessionRepository();
        CookieSessionManager manager = CreateManager(repository);
        Guid userId = Guid.NewGuid();
        await manager.UpsertSessionAsync(userId, "115001", new CookieTriplet("uid-1", "cid-1", "ussid-1"));

        var gateway = new RecordingUserGateway();
        gateway.RespondWeb("files/index_info", """{"state":true,"data":{"all_total":"10 GB","all_use":"2 GB","all_remain":"8 GB","all_total_size":10,"all_use_size":2,"all_remain_size":8}}""");
        gateway.RespondWeb("user/vip_limit", """{"state":true,"data":{"level_name":"VIP","expire":"1893456000"}}""");
        gateway.RespondUserCenter(".", """{"state":true,"data":{"vip_count":2,"00_balance":88}}""");

        UserAggregationService service = CreateUserAggregationService(gateway);
        var accessor = new CloudDriveAccountContextAccessor();
        accessor.SetContext(new CloudDriveAccountContext(userId, "115001", "115001"));
        IQueryHandler<GetUserInfoQuery, CloudDriveUserInfoDto> handler = new GetUserInfoQueryHandler(
            repository,
            service,
            accessor,
            CreateEnabledConfig());

        // Act
        Result<CloudDriveUserInfoDto> result = await handler.Handle(
            new GetUserInfoQuery(userId),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_api_not_collected", result.Error.Code);
        Assert.Empty(gateway.WebEndpoints);
        Assert.Empty(gateway.UserCenterEndpoints);
    }

    /// <summary>
    /// 验证非活跃会话查询用户信息时返回 Cookie 未激活失败。
    /// </summary>
    [Fact]
    public async Task GetUserInfoQueryHandler_WhenSessionNotActive_ReturnsCookieNotActive()
    {
        // Arrange
        var repository = new FakeCookieSessionRepository();
        CookieSessionManager manager = CreateManager(repository);
        Guid userId = Guid.NewGuid();
        await manager.UpsertSessionAsync(userId, "115001", new CookieTriplet("uid-1", "cid-1", "ussid-1"));
        await manager.MarkInvalidatedAsync(userId, "115001", "login_required");

        var gateway = new RecordingUserGateway();
        UserAggregationService service = CreateUserAggregationService(gateway);
        var accessor = new CloudDriveAccountContextAccessor();
        accessor.SetContext(new CloudDriveAccountContext(userId, "115001", "115001"));
        IQueryHandler<GetUserInfoQuery, CloudDriveUserInfoDto> handler = new GetUserInfoQueryHandler(
            repository,
            service,
            accessor,
            CreateEnabledConfig());

        // Act
        Result<CloudDriveUserInfoDto> result = await handler.Handle(
            new GetUserInfoQuery(userId),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("cookie_not_active", result.Error.Code);
        Assert.Empty(gateway.WebEndpoints);
        Assert.Empty(gateway.UserCenterEndpoints);
    }

    /// <summary>
    /// 验证账号上下文未设置时用户信息查询抛出无效操作异常。
    /// </summary>
    [Fact]
    public async Task GetUserInfoQueryHandler_WhenAccessorNotSet_ThrowsInvalidOperation()
    {
        // Arrange（准备）
        var repository = new FakeCookieSessionRepository();
        var gateway = new RecordingUserGateway();
        UserAggregationService service = CreateUserAggregationService(gateway);
        IQueryHandler<GetUserInfoQuery, CloudDriveUserInfoDto> handler = new GetUserInfoQueryHandler(
            repository,
            service,
            new CloudDriveAccountContextAccessor(),
            CreateEnabledConfig());

        // Act（执行）
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new GetUserInfoQuery(Guid.NewGuid()),
                CancellationToken.None));

        // Assert（断言）
        Assert.Empty(gateway.WebEndpoints);
        Assert.Empty(gateway.UserCenterEndpoints);
    }

    private static CookieSessionManager CreateManager(FakeCookieSessionRepository repository)
    {
        return new CookieSessionManager(
            repository,
            new FakeCloudDriveSecretProtector(),
            new FakeCookieInvalidationNotifier(),
            CreateEnabledConfig(),
            new FakeLogger<CookieSessionManager>());
    }

    private static UserAggregationService CreateUserAggregationService(ICloudDriveWebApiGateway gateway)
    {
        return new UserAggregationService(
            gateway,
            CreateEnabledConfig(),
            new FakeLogger<UserAggregationService>());
    }

    private static IOptions<CloudDriveAppConfig> CreateEnabledConfig()
    {
        return Options.Create(new CloudDriveAppConfig
        {
            CloudDriveFeatureEnabled = true,
        });
    }

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

    private sealed class RecordingUserGateway : ICloudDriveWebApiGateway
    {
        private readonly Dictionary<string, JsonElement> _webResponses = new(StringComparer.Ordinal);
        private readonly Dictionary<string, JsonElement> _userCenterResponses = new(StringComparer.Ordinal);

        public List<string> WebEndpoints { get; } = [];

        public List<string> UserCenterEndpoints { get; } = [];

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
            UserCenterEndpoints.Add(endpoint);
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
