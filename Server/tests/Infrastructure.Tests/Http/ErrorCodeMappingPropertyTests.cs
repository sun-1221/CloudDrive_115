using System.Net;
using System.Text;
using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Auth;

using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.ValueObjects;

using Infrastructure.Http;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Tests.Http;

public sealed class ErrorCodeMappingPropertyTests
{
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, typeof(CloudDriveCookieInvalidatedException))]
    [InlineData(HttpStatusCode.Forbidden, typeof(CloudDriveCookieInvalidatedException))]
    [InlineData(HttpStatusCode.NotFound, typeof(CloudDriveFileNotFoundException))]
    [InlineData((HttpStatusCode)429, typeof(CloudDriveRateLimitException))]
    [InlineData(HttpStatusCode.InternalServerError, typeof(CloudDriveApiException))]
    public async Task SendWebApiRequestAsync_WhenHttpError_MapsToExpectedExceptionAsync(
        HttpStatusCode statusCode,
        Type expectedExceptionType)
    {
        // 准备
        TestClientContext context = await TestClientContext.CreateAsync(new QueueHandler(
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("""{"state":false,"error":"failed"}""", Encoding.UTF8, "application/json"),
            }));

        // 执行
        Exception exception = await Record.ExceptionAsync(
            () => context.Client.SendWebApiRequestAsync<JsonElement>(
                context.UserId,
                context.CloudAccountUid,
                HttpMethod.Get,
                "files",
                new CloudDriveApiContext { Category = "files" },
                CancellationToken.None));

        // 断言
        Assert.NotNull(exception);
        Assert.IsType(expectedExceptionType, exception);

        if (statusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            CookieSession? session = await context.Repository.GetByAccountAsync(context.UserId, context.CloudAccountUid);
            Assert.NotNull(session);
            Assert.Equal(CookieSessionStatus.Invalidated, session.Status);
        }

        if ((int)statusCode == 429)
        {
            Assert.Single(context.RateLimiter.ThrottleSignals);
        }
    }

    [Theory]
    [InlineData("""{"state":false,"errNo":"login_required","error":"login_required"}""", typeof(CloudDriveCookieInvalidatedException))]
    [InlineData("""{"state":false,"errNo":429,"error":"rate limit"}""", typeof(CloudDriveRateLimitException))]
    [InlineData("""{"state":false,"errNo":404,"error":"missing"}""", typeof(CloudDriveFileNotFoundException))]
    [InlineData("""{"state":false,"errNo":500,"error":"server failed"}""", typeof(CloudDriveApiException))]
    public async Task SendWebApiRequestAsync_WhenBusinessError_MapsToExpectedExceptionAsync(
        string json,
        Type expectedExceptionType)
    {
        // 准备
        TestClientContext context = await TestClientContext.CreateAsync(new QueueHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }));

        // 执行
        Exception exception = await Record.ExceptionAsync(
            () => context.Client.SendWebApiRequestAsync<JsonElement>(
                context.UserId,
                context.CloudAccountUid,
                HttpMethod.Get,
                "files",
                new CloudDriveApiContext { Category = "files" },
                CancellationToken.None));

        // 断言
        Assert.NotNull(exception);
        Assert.IsType(expectedExceptionType, exception);

        if (expectedExceptionType == typeof(CloudDriveCookieInvalidatedException))
        {
            CookieSession? session = await context.Repository.GetByAccountAsync(context.UserId, context.CloudAccountUid);
            Assert.NotNull(session);
            Assert.Equal(CookieSessionStatus.Invalidated, session.Status);
            Assert.Single(context.Notifier.Notifications);
        }

        if (expectedExceptionType == typeof(CloudDriveRateLimitException))
        {
            Assert.Single(context.RateLimiter.ThrottleSignals);
        }
    }

    [Fact]
    public async Task SendWebApiRequestAsync_WhenNetworkTimeout_ThrowsTimeoutNetworkExceptionAsync()
    {
        // 准备
        TestClientContext context = await TestClientContext.CreateAsync(new ThrowingHandler(
            new TaskCanceledException("timeout")));

        // 执行
        CloudDriveNetworkException exception = await Assert.ThrowsAsync<CloudDriveNetworkException>(
            () => context.Client.SendWebApiRequestAsync<JsonElement>(
                context.UserId,
                context.CloudAccountUid,
                HttpMethod.Get,
                "files",
                new CloudDriveApiContext(),
                CancellationToken.None));

        // 断言
        Assert.Equal("cloud_drive_api_timeout", exception.ErrorCode);
    }

    [Fact]
    public async Task SendWebApiRequestAsync_WhenConnectionFails_ThrowsNetworkExceptionAsync()
    {
        // 准备
        TestClientContext context = await TestClientContext.CreateAsync(new ThrowingHandler(
            new HttpRequestException("connection failed")));

        // 执行
        CloudDriveNetworkException exception = await Assert.ThrowsAsync<CloudDriveNetworkException>(
            () => context.Client.SendWebApiRequestAsync<JsonElement>(
                context.UserId,
                context.CloudAccountUid,
                HttpMethod.Get,
                "files",
                new CloudDriveApiContext(),
                CancellationToken.None));

        // 断言
        Assert.Equal("cloud_drive_api_network_error", exception.ErrorCode);
    }

    [Theory]
    [InlineData("unexpected")]
    [InlineData("unknownField")]
    [InlineData("raw")]
    public void ErrorFieldAdapter_WhenUnknownStateFalseShape_ReturnsUnknownErrorShape(string propertyName)
    {
        // 准备
        var adapter = new ErrorFieldAdapter();
        using JsonDocument document = JsonDocument.Parse($$"""{"state":false,"{{propertyName}}":"value"}""");

        // 执行
        CloudDriveApiError? error = adapter.TryParse(new Uri("https://webapi.115.com/unregistered"), document);

        // 断言
        Assert.NotNull(error);
        Assert.Equal("unknown_error_shape", error.RawField);
    }

    [Fact]
    public async Task AddReadResilientHttpClient_WhenTransientFailure_RetriesAsync()
    {
        // 准备
        var services = new ServiceCollection();
        var handler = new QueueHandler(
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"state":true}""", Encoding.UTF8, "application/json"),
            });

        services.AddReadResilientHttpClient("read-test")
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        using ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("read-test");
        client.BaseAddress = new Uri("https://webapi.115.com/");

        // 执行
        HttpResponseMessage response = await client.GetAsync("files", CancellationToken.None);

        // 断言
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(handler.SendCount >= 2);
    }

    [Fact]
    public async Task AddWriteResilientHttpClient_WhenTransientFailure_DoesNotRetryAsync()
    {
        // 准备
        var services = new ServiceCollection();
        var handler = new QueueHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        services.AddWriteResilientHttpClient("write-test")
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        using ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("write-test");
        client.BaseAddress = new Uri("https://webapi.115.com/");

        // 执行
        HttpResponseMessage response = await client.PostAsync("files/add", new StringContent(""), CancellationToken.None);

        // 断言
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(1, handler.SendCount);
    }

    private sealed class TestClientContext
    {
        private TestClientContext(
            CloudDriveApiClient client,
            FakeCookieSessionRepository repository,
            FakeCookieInvalidationNotifier notifier,
            CountingRateLimiter rateLimiter,
            Guid userId,
            string cloudAccountUid)
        {
            Client = client;
            Repository = repository;
            Notifier = notifier;
            RateLimiter = rateLimiter;
            UserId = userId;
            CloudAccountUid = cloudAccountUid;
        }

        public CloudDriveApiClient Client { get; }

        public FakeCookieSessionRepository Repository { get; }

        public FakeCookieInvalidationNotifier Notifier { get; }

        public CountingRateLimiter RateLimiter { get; }

        public Guid UserId { get; }

        public string CloudAccountUid { get; }

        public static async Task<TestClientContext> CreateAsync(HttpMessageHandler handler)
        {
            Guid userId = Guid.NewGuid();
            const string cloudAccountUid = "115001";
            var repository = new FakeCookieSessionRepository();
            var notifier = new FakeCookieInvalidationNotifier();
            var rateLimiter = new CountingRateLimiter();
            IOptions<CloudDriveAppConfig> options = Options.Create(new CloudDriveAppConfig
            {
                CookieEncryptionKeyName = "test",
                WebApiBaseUrl = "https://webapi.115.com/",
                CloudDriveFeatureEnabled = true,
            });

            var cookieSessionManager = new CookieSessionManager(
                repository,
                new PlainTextSecretProtector(),
                notifier,
                options,
                NullLogger<CookieSessionManager>.Instance);

            await cookieSessionManager.UpsertSessionAsync(
                userId,
                cloudAccountUid,
                new CookieTriplet("uid", "cid", "ussid"),
                cancellationToken: CancellationToken.None);

            var client = new CloudDriveApiClient(
                new SingleHandlerHttpClientFactory(handler),
                cookieSessionManager,
                new ErrorFieldAdapter(),
                new NoopCookieSignatureStrategy(),
                rateLimiter,
                new PassThroughSensitiveFieldsFilter(),
                options,
                NullLogger<CloudDriveApiClient>.Instance);

            return new TestClientContext(client, repository, notifier, rateLimiter, userId, cloudAccountUid);
        }
    }

    private sealed class SingleHandlerHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = name switch
                {
                    CloudDriveHttpClientNames.Offline => new Uri("https://115.com/web/lixian/"),
                    CloudDriveHttpClientNames.Upload => new Uri("https://uplb.115.com/"),
                    _ => new Uri("https://webapi.115.com/"),
                },
            };
        }
    }

    private sealed class QueueHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses;

        public QueueHandler(params HttpResponseMessage[] responses)
        {
            _responses = new Queue<HttpResponseMessage>(responses);
        }

        public int SendCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.FromResult(_responses.Count > 0
                ? _responses.Dequeue()
                : new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromException<HttpResponseMessage>(exception);
        }
    }

    private sealed class CountingRateLimiter : ICloudDriveRateLimiter
    {
        public List<(Guid UserId, string CloudAccountUid, string? Category)> ThrottleSignals { get; } = [];

        public Task<IDisposable> AcquireAsync(
            Guid userId,
            string cloudAccountUid,
            string? category = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IDisposable>(NullDisposable.Instance);
        }

        public void RecordThrottleSignal(Guid userId, string cloudAccountUid, string? category = null)
        {
            ThrottleSignals.Add((userId, cloudAccountUid, category));
        }

        public void Reset(Guid userId, string cloudAccountUid, string? category = null) { }
    }

    private sealed class NoopCookieSignatureStrategy : ICookieSignatureStrategy
    {
        public CookieSignature Generate(Guid userId, string cloudAccountUid, string requestBody) =>
            new("sign", 1, "uid");
    }

    private sealed class PassThroughSensitiveFieldsFilter : ICloudDriveSensitiveFieldsFilter
    {
        public string Redact(string? value) => value ?? string.Empty;

        public IReadOnlyDictionary<string, string?> RedactFields(IReadOnlyDictionary<string, string?> fields) => fields;
    }

    private sealed class PlainTextSecretProtector : ICloudDriveSecretProtector
    {
        public string Protect(string keyName, string plaintext) => plaintext;

        public string Unprotect(string keyName, string ciphertext, int? keyVersion = null) => ciphertext;
    }

    private sealed class FakeCookieInvalidationNotifier : ICookieInvalidationNotifier
    {
        public List<(Guid UserId, string CloudAccountUid, string Reason)> Notifications { get; } = [];

        public Task NotifyAsync(
            Guid userId,
            string cloudAccountUid,
            string reason,
            CancellationToken cancellationToken = default)
        {
            Notifications.Add((userId, cloudAccountUid, reason));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCookieSessionRepository : ICookieSessionRepository
    {
        private readonly Dictionary<Guid, CookieSession> _store = [];

        public Task<CookieSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _store.TryGetValue(id, out CookieSession? session);
            return Task.FromResult(session);
        }

        public Task<IReadOnlyList<CookieSession>> GetListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CookieSession>>(_store.Values.ToList().AsReadOnly());

        public Task<int> InsertAsync(CookieSession entity, CancellationToken cancellationToken = default)
        {
            _store[entity.Id] = entity;
            return Task.FromResult(1);
        }

        public Task<int> UpdateAsync(CookieSession entity, CancellationToken cancellationToken = default)
        {
            _store[entity.Id] = entity;
            return Task.FromResult(1);
        }

        public Task<int> DeleteAsync(CookieSession entity, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.Remove(entity.Id) ? 1 : 0);

        public Task<CookieSession?> GetByAccountAsync(
            Guid userId,
            string cloudAccountUid,
            CancellationToken cancellationToken = default)
        {
            CookieSession? session = _store.Values.FirstOrDefault(
                item => item.UserId == userId && item.CloudAccountUid == cloudAccountUid);
            return Task.FromResult(session);
        }

        public Task<IReadOnlyList<CookieSession>> GetByUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<CookieSession> sessions = _store.Values
                .Where(item => item.UserId == userId)
                .ToList()
                .AsReadOnly();
            return Task.FromResult(sessions);
        }
    }

    private sealed class NullDisposable : IDisposable
    {
        public static NullDisposable Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
