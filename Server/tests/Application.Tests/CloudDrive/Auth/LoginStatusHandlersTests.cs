using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Tests.Support;

using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.Results;
using Domain.ValueObjects;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Auth;

/// <summary>
/// 115 网盘登录状态处理器测试。
/// </summary>
public sealed class LoginStatusHandlersTests
{
    /// <summary>
    /// 验证进入 Cookie 冷却命令会将会话标记为失效。
    /// </summary>
    [Fact]
    public async Task EnterCookieCooldownCommandHandler_WhenValid_MarksSessionInvalidated()
    {
        // Arrange（准备）
        var repository = new FakeCookieSessionRepository();
        var manager = CreateManager(repository, featureEnabled: true);
        Guid userId = Guid.NewGuid();
        await manager.UpsertSessionAsync(userId, "115001", new CookieTriplet("uid", "cid", "ussid"));
        var handler = new EnterCookieCooldownCommandHandler(
            manager,
            Options.Create(new CloudDriveAppConfig { CloudDriveFeatureEnabled = true }));

        // Act（执行）
        Result result = await handler.Handle(
            new EnterCookieCooldownCommand(
                userId,
                new EnterCookieCooldownInput
                {
                    CloudAccountUid = "115001",
                    Reason = "login_required",
                    CooldownSeconds = 30,
                }),
            CancellationToken.None);

        // Assert（断言）
        CookieSession session = (await repository.GetByAccountAsync(userId, "115001"))!;
        Assert.True(result.IsSuccess);
        Assert.Equal(CookieSessionStatus.Invalidated, session.Status);
        Assert.Equal("login_required", session.InvalidationReason);
        Assert.NotNull(session.CooldownUntil);
    }

    /// <summary>
    /// 验证网关调用成功时登录状态查询返回已登录。
    /// </summary>
    [Fact]
    public async Task GetLoginStatusQueryHandler_WhenGatewaySucceeds_ReturnsLoggedInTrue()
    {
        // Arrange（准备）
        var repository = new FakeCookieSessionRepository();
        var gateway = new StubGateway(JsonDocument.Parse("""{"state":true}""").RootElement.Clone());
        var handler = CreateLoginHandler(repository, gateway, featureEnabled: true);
        Guid userId = Guid.NewGuid();
        await CreateManager(repository, featureEnabled: true)
            .UpsertSessionAsync(userId, "115001", new CookieTriplet("uid", "cid", "ussid"));

        // Act（执行）
        Result<CloudDriveAuthCheckDto> result = await handler.Handle(
            new GetLoginStatusQuery(userId),
            CancellationToken.None);

        // Assert（断言）
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsAuthenticated);
        Assert.Equal("files", gateway.LastEndpoint);
        Assert.Equal("auth", gateway.LastContext?.Category);
    }

    /// <summary>
    /// 验证 Cookie 已失效时登录状态查询返回未登录且无副作用。
    /// </summary>
    [Fact]
    public async Task GetLoginStatusQueryHandler_WhenCookieInvalidated_ReturnsFalseWithoutSideEffects()
    {
        // Arrange（准备）
        var repository = new FakeCookieSessionRepository();
        var gateway = new StubGateway(new CloudDriveCookieInvalidatedException(errorCode: "login_required"));
        var handler = CreateLoginHandler(repository, gateway, featureEnabled: true);

        Guid userId = Guid.NewGuid();
        await CreateManager(repository, featureEnabled: true)
            .UpsertSessionAsync(userId, "115001", new CookieTriplet("uid", "cid", "ussid"));

        // Act（执行）
        Result<CloudDriveAuthCheckDto> result = await handler.Handle(
            new GetLoginStatusQuery(userId),
            CancellationToken.None);

        // Assert（断言）
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsAuthenticated);
        Assert.Equal("cookie_invalid", result.Value.FailureReason);
    }

    /// <summary>
    /// 验证会话处于冷却期时登录状态查询会跳过网关调用。
    /// </summary>
    [Fact]
    public async Task GetLoginStatusQueryHandler_WhenSessionInCooldown_SkipsGateway()
    {
        // Arrange（准备）
        var repository = new FakeCookieSessionRepository();
        var manager = CreateManager(repository, featureEnabled: true);
        Guid userId = Guid.NewGuid();
        await manager.UpsertSessionAsync(userId, "115001", new CookieTriplet("uid", "cid", "ussid"));
        await manager.MarkInvalidatedAsync(userId, "115001", "login_required");

        var gateway = new StubGateway(JsonDocument.Parse("""{"state":true}""").RootElement.Clone());
        var handler = CreateLoginHandler(repository, gateway, featureEnabled: true);

        // Act（执行）
        Result<CloudDriveAuthCheckDto> result = await handler.Handle(
            new GetLoginStatusQuery(userId),
            CancellationToken.None);

        // Assert（断言）
        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsAuthenticated);
        Assert.Equal("cookie_invalid", result.Value.FailureReason);
        Assert.Null(gateway.LastEndpoint);
    }

    private static CookieSessionManager CreateManager(FakeCookieSessionRepository repository, bool featureEnabled)
    {
        return new CookieSessionManager(
            repository,
            new FakeCloudDriveSecretProtector(),
            new FakeCookieInvalidationNotifier(),
            Options.Create(new CloudDriveAppConfig { CloudDriveFeatureEnabled = featureEnabled }),
            new FakeLogger<CookieSessionManager>());
    }

    private static GetLoginStatusQueryHandler CreateLoginHandler(
        ICookieSessionRepository repository,
        ICloudDriveWebApiGateway gateway,
        bool featureEnabled)
    {
        return new GetLoginStatusQueryHandler(
            repository,
            gateway,
            Options.Create(new CloudDriveAppConfig { CloudDriveFeatureEnabled = featureEnabled }));
    }

    private sealed class StubGateway(object responseOrException) : ICloudDriveWebApiGateway
    {
        public string? LastEndpoint { get; private set; }

        public CloudDriveApiContext? LastContext { get; private set; }

        public Task<T> SendWebApiRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            HttpMethod method,
            string endpoint,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default)
        {
            LastEndpoint = endpoint;
            LastContext = context;

            return responseOrException is Exception exception
                ? Task.FromException<T>(exception)
                : Task.FromResult((T)responseOrException);
        }

        public Task<T> SendUserCenterRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            HttpMethod method,
            string endpoint,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

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
    }
}
