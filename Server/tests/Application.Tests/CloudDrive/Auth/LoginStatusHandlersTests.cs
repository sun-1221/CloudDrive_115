using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;
using Application.Queries;
using Application.Tests.Support;

using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.Results;
using Domain.ValueObjects;

using MediatR;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Auth;

public sealed class LoginStatusHandlersTests
{
    [Fact]
    public async Task EnterCookieCooldownCommandHandler_WhenValid_MarksSessionInvalidated()
    {
        var repository = new FakeCookieSessionRepository();
        var manager = CreateManager(repository, featureEnabled: true);
        Guid userId = Guid.NewGuid();
        await manager.UpsertSessionAsync(userId, "115001", new CookieTriplet("uid", "cid", "ussid"));
        var handler = new EnterCookieCooldownCommandHandler(
            manager,
            Options.Create(new CloudDriveAppConfig { CloudDriveFeatureEnabled = true }));

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

        CookieSession session = (await repository.GetByAccountAsync(userId, "115001"))!;
        Assert.True(result.IsSuccess);
        Assert.Equal(CookieSessionStatus.Invalidated, session.Status);
        Assert.Equal("login_required", session.InvalidationReason);
        Assert.NotNull(session.CooldownUntil);
    }

    [Fact]
    public async Task GetLoginStatusQueryHandler_WhenGatewaySucceeds_ReturnsLoggedInTrue()
    {
        var repository = new FakeCookieSessionRepository();
        var gateway = new StubGateway(JsonDocument.Parse("""{"state":true}""").RootElement.Clone());
        var sender = new RecordingSender();
        var handler = CreateLoginHandler(repository, gateway, sender, featureEnabled: true);

        Result<CloudDriveLoginStatusDto> result = await handler.Handle(
            new GetLoginStatusQuery(Guid.NewGuid(), new CloudDriveAccountScopedInput { CloudAccountUid = "115001" }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsLoggedIn);
        Assert.Equal("files", gateway.LastEndpoint);
        Assert.Equal("auth", gateway.LastContext?.Category);
        Assert.Empty(sender.SentRequests);
    }

    [Fact]
    public async Task GetLoginStatusQueryHandler_WhenCookieInvalidated_SendsCooldownCommandAndReturnsFalse()
    {
        var repository = new FakeCookieSessionRepository();
        var gateway = new StubGateway(new CloudDriveCookieInvalidatedException(errorCode: "login_required"));
        var sender = new RecordingSender();
        var handler = CreateLoginHandler(repository, gateway, sender, featureEnabled: true);

        Guid userId = Guid.NewGuid();
        Result<CloudDriveLoginStatusDto> result = await handler.Handle(
            new GetLoginStatusQuery(userId, new CloudDriveAccountScopedInput { CloudAccountUid = "115001" }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsLoggedIn);
        EnterCookieCooldownCommand command = Assert.IsType<EnterCookieCooldownCommand>(Assert.Single(sender.SentRequests));
        Assert.Equal(userId, command.UserId);
        Assert.Equal("115001", command.Input.CloudAccountUid);
        Assert.Equal("login_required", command.Input.Reason);
    }

    [Fact]
    public async Task GetLoginStatusQueryHandler_WhenSessionInCooldown_SkipsGateway()
    {
        var repository = new FakeCookieSessionRepository();
        var manager = CreateManager(repository, featureEnabled: true);
        Guid userId = Guid.NewGuid();
        await manager.UpsertSessionAsync(userId, "115001", new CookieTriplet("uid", "cid", "ussid"));
        await manager.MarkInvalidatedAsync(userId, "115001", "login_required");

        var gateway = new StubGateway(JsonDocument.Parse("""{"state":true}""").RootElement.Clone());
        var sender = new RecordingSender();
        var handler = CreateLoginHandler(repository, gateway, sender, featureEnabled: true);

        Result<CloudDriveLoginStatusDto> result = await handler.Handle(
            new GetLoginStatusQuery(userId, new CloudDriveAccountScopedInput { CloudAccountUid = "115001" }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsLoggedIn);
        Assert.Equal("login_required", result.Value.FailureReason);
        Assert.Null(gateway.LastEndpoint);
        Assert.Empty(sender.SentRequests);
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
        ICloudDriveOpenApiGateway gateway,
        ISender sender,
        bool featureEnabled)
    {
        return new GetLoginStatusQueryHandler(
            repository,
            gateway,
            sender,
            Options.Create(new CloudDriveAppConfig { CloudDriveFeatureEnabled = featureEnabled }));
    }

    private sealed class RecordingSender : ISender
    {
        public List<object> SentRequests { get; } = [];

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            object response = request switch
            {
                EnterCookieCooldownCommand => Result.Success(),
                _ => throw new NotSupportedException(request.GetType().Name),
            };

            return Task.FromResult((TResponse)response);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            SentRequests.Add(request);
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
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

    private sealed class StubGateway(object responseOrException) : ICloudDriveOpenApiGateway
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
