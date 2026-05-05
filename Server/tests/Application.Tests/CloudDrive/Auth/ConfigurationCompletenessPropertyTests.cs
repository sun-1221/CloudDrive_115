using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;
using Application.Queries;
using Application.Tests.Support;

using Domain.Exceptions;
using Domain.Repositories;
using Domain.Results;
using Domain.ValueObjects;

using FsCheck.Xunit;

using MediatR;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Auth;

/// <summary>
/// 属性 P8：配置完整性校验。
/// </summary>
public sealed class ConfigurationCompletenessPropertyTests
{
    [Property]
    public void ApplyStartupValidation_WhenCoreConfigMissing_DisablesFeature(
        string? cookieEncryptionKeyName,
        string? webApiBaseUrl)
    {
        // Arrange
        var config = new CloudDriveAppConfig
        {
            CloudDriveFeatureEnabled = true,
            CookieEncryptionKeyName = NormalizeConfigValue(cookieEncryptionKeyName)!,
            WebApiBaseUrl = NormalizeConfigValue(webApiBaseUrl)!,
        };

        // Act
        config.ApplyStartupValidation();

        // Assert
        bool missingRequiredConfig = string.IsNullOrWhiteSpace(config.CookieEncryptionKeyName)
            || string.IsNullOrWhiteSpace(config.WebApiBaseUrl);
        Assert.Equal(!missingRequiredConfig, config.CloudDriveFeatureEnabled);
    }

    [Property]
    public void ApplyStartupValidation_WhenOfflineSignatureEnabledAndSignatureKeyMissing_DisablesFeature(
        string? signatureKeyName)
    {
        // Arrange
        var config = new CloudDriveAppConfig
        {
            CloudDriveFeatureEnabled = true,
            CookieEncryptionKeyName = "cookie-key",
            WebApiBaseUrl = "https://webapi.115.com/",
            OfflineSignatureEnabled = true,
            SignatureKeyName = NormalizeConfigValue(signatureKeyName),
        };

        // Act
        config.ApplyStartupValidation();

        // Assert
        Assert.Equal(!string.IsNullOrWhiteSpace(config.SignatureKeyName), config.CloudDriveFeatureEnabled);
    }

    [Fact]
    public async Task AuthHandlers_WhenFeatureDisabled_ThrowConfigurationException()
    {
        // Arrange
        DisabledHandlerContext context = await CreateDisabledHandlerContextAsync();

        // Act & Assert
        await Assert.ThrowsAsync<CloudDriveConfigurationException>(
            () => context.UpsertHandler.Handle(
                new UpsertCookieSessionCommand(
                    context.UserId,
                    new UpsertCookieSessionInput
                    {
                        CloudAccountUid = "115001",
                        Uid = "uid",
                        Cid = "cid",
                        Ussid = "ussid",
                    }),
                CancellationToken.None));

        await Assert.ThrowsAsync<CloudDriveConfigurationException>(
            () => context.LoginStatusHandler.Handle(
                new GetLoginStatusQuery(context.UserId),
                CancellationToken.None));

        await Assert.ThrowsAsync<CloudDriveConfigurationException>(
            () => context.EnterCooldownHandler.Handle(
                new EnterCookieCooldownCommand(
                    context.UserId,
                    new EnterCookieCooldownInput
                    {
                        CloudAccountUid = "115001",
                        Reason = "login_required",
                    }),
                CancellationToken.None));

        await Assert.ThrowsAsync<CloudDriveConfigurationException>(
            () => context.BoundAccountsHandler.Handle(
                new GetBoundAccountsQuery(context.UserId),
                CancellationToken.None));

        await Assert.ThrowsAsync<CloudDriveConfigurationException>(
            () => context.UserInfoHandler.Handle(
                new GetUserInfoQuery(context.UserId),
                CancellationToken.None));

        await Assert.ThrowsAsync<CloudDriveConfigurationException>(
            () => context.RevokeHandler.Handle(
                new RevokeCookieSessionCommand(
                    context.UserId,
                    new RevokeCookieSessionInput { CloudAccountUid = "115001" }),
                CancellationToken.None));

        Assert.Empty(context.Gateway.WebEndpoints);
        Assert.Empty(context.MetadataCache.InvalidatedDirectories);
        Assert.Empty(context.RateLimiter.ResetCalls);
    }

    [Fact]
    public async Task AuthHandlers_WhenFeatureEnabled_RunWithoutConfigurationException()
    {
        // Arrange
        EnabledHandlerContext context = await CreateEnabledHandlerContextAsync();

        // Act
        Result<CloudDriveCookieSessionDto> upsertResult = await context.UpsertHandler.Handle(
            new UpsertCookieSessionCommand(
                context.UserId,
                new UpsertCookieSessionInput
                {
                    CloudAccountUid = "115001",
                    Uid = "uid",
                    Cid = "cid",
                    Ussid = "ussid",
                }),
            CancellationToken.None);
        Result<List<CloudDriveAccountSummaryDto>> boundAccountsResult = await context.BoundAccountsHandler.Handle(
            new GetBoundAccountsQuery(context.UserId),
            CancellationToken.None);
        Result<CloudDriveAuthCheckDto> loginStatusResult = await context.LoginStatusHandler.Handle(
            new GetLoginStatusQuery(context.UserId),
            CancellationToken.None);
        Result<CloudDriveUserInfoDto> userInfoResult = await context.UserInfoHandler.Handle(
            new GetUserInfoQuery(context.UserId),
            CancellationToken.None);
        Result revokeResult = await context.RevokeHandler.Handle(
            new RevokeCookieSessionCommand(
                context.UserId,
                new RevokeCookieSessionInput { CloudAccountUid = "115001" }),
            CancellationToken.None);

        // Assert
        Assert.True(upsertResult.IsSuccess);
        Assert.True(boundAccountsResult.IsSuccess);
        Assert.True(loginStatusResult.IsSuccess);
        Assert.True(userInfoResult.IsSuccess);
        Assert.True(revokeResult.IsSuccess);
    }

    private static string? NormalizeConfigValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        return trimmed.Length > 128 ? trimmed[..128] : trimmed;
    }

    private static async Task<DisabledHandlerContext> CreateDisabledHandlerContextAsync()
    {
        Guid userId = Guid.NewGuid();
        IOptions<CloudDriveAppConfig> enabledManagerConfig = Options.Create(new CloudDriveAppConfig
        {
            CloudDriveFeatureEnabled = true,
        });
        IOptions<CloudDriveAppConfig> disabledHandlerConfig = Options.Create(new CloudDriveAppConfig
        {
            CloudDriveFeatureEnabled = false,
        });
        var repository = new FakeCookieSessionRepository();
        var manager = new CookieSessionManager(
            repository,
            new FakeCloudDriveSecretProtector(),
            new FakeCookieInvalidationNotifier(),
            enabledManagerConfig,
            new FakeLogger<CookieSessionManager>());
        await manager.UpsertSessionAsync(userId, "115001", new CookieTriplet("uid", "cid", "ussid"));

        var metadataCache = new RecordingMetadataCache();
        var rateLimiter = new RecordingRateLimiter();
        var gateway = new RecordingGateway();
        var sender = new RecordingSender();
        UserAggregationService userAggregationService = new(
            gateway,
            disabledHandlerConfig,
            new FakeLogger<UserAggregationService>());

        return new DisabledHandlerContext(
            userId,
            gateway,
            metadataCache,
            rateLimiter,
            new UpsertCookieSessionCommandHandler(manager, repository, metadataCache, rateLimiter, disabledHandlerConfig),
            new GetLoginStatusQueryHandler(repository, gateway, disabledHandlerConfig),
            new EnterCookieCooldownCommandHandler(manager, disabledHandlerConfig),
            new GetBoundAccountsQueryHandler(manager, disabledHandlerConfig),
            new GetUserInfoQueryHandler(repository, userAggregationService, new CloudDriveAccountContextAccessor(), disabledHandlerConfig),
            new RevokeCookieSessionCommandHandler(manager, metadataCache, rateLimiter, disabledHandlerConfig));
    }

    private static async Task<EnabledHandlerContext> CreateEnabledHandlerContextAsync()
    {
        Guid userId = Guid.NewGuid();
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
        gateway.RespondWeb("files", """{"state":true}""");
        gateway.RespondWeb("files/index_info", """{"state":true,"data":{"all_total":"10 GB"}}""");
        gateway.RespondWeb("user/vip_limit", """{"state":true,"data":{"level_name":"VIP"}}""");
        gateway.RespondUserCenter("proapi/3.0/index.php", """{"state":true,"data":{"user_name":"tester"}}""");
        gateway.RespondUserCenter(".", """{"state":true,"data":{"vip_count":1}}""");
        var sender = new RecordingSender();
        UserAggregationService userAggregationService = new(
            gateway,
            config,
            new FakeLogger<UserAggregationService>());

        await manager.UpsertSessionAsync(userId, "115001", new CookieTriplet("uid", "cid", "ussid"));

        return new EnabledHandlerContext(
            userId,
            new UpsertCookieSessionCommandHandler(manager, repository, metadataCache, rateLimiter, config),
            new GetLoginStatusQueryHandler(repository, gateway, config),
            new GetBoundAccountsQueryHandler(manager, config),
            new GetUserInfoQueryHandler(repository, userAggregationService, CreateAccessor(userId), config),
            new RevokeCookieSessionCommandHandler(manager, metadataCache, rateLimiter, config));
    }

    private sealed record DisabledHandlerContext(
        Guid UserId,
        RecordingGateway Gateway,
        RecordingMetadataCache MetadataCache,
        RecordingRateLimiter RateLimiter,
        ICommandHandler<UpsertCookieSessionCommand, CloudDriveCookieSessionDto> UpsertHandler,
        IQueryHandler<GetLoginStatusQuery, CloudDriveAuthCheckDto> LoginStatusHandler,
        ICommandHandler<EnterCookieCooldownCommand> EnterCooldownHandler,
        IQueryHandler<GetBoundAccountsQuery, List<CloudDriveAccountSummaryDto>> BoundAccountsHandler,
        IQueryHandler<GetUserInfoQuery, CloudDriveUserInfoDto> UserInfoHandler,
        ICommandHandler<RevokeCookieSessionCommand> RevokeHandler);

    private static CloudDriveAccountContextAccessor CreateAccessor(Guid userId)
    {
        var accessor = new CloudDriveAccountContextAccessor();
        accessor.SetContext(new CloudDriveAccountContext(userId, "115001", "115001"));
        return accessor;
    }

    private sealed record EnabledHandlerContext(
        Guid UserId,
        ICommandHandler<UpsertCookieSessionCommand, CloudDriveCookieSessionDto> UpsertHandler,
        IQueryHandler<GetLoginStatusQuery, CloudDriveAuthCheckDto> LoginStatusHandler,
        IQueryHandler<GetBoundAccountsQuery, List<CloudDriveAccountSummaryDto>> BoundAccountsHandler,
        IQueryHandler<GetUserInfoQuery, CloudDriveUserInfoDto> UserInfoHandler,
        ICommandHandler<RevokeCookieSessionCommand> RevokeHandler);

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

    private sealed class RecordingSender : ISender
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            object response = Result.Success();
            return Task.FromResult((TResponse)response);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<object?>(null);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
