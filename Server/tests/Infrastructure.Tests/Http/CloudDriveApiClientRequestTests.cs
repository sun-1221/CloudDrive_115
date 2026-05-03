using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Auth;

using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;

using Infrastructure.Http;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Tests.Http;

public sealed class CloudDriveApiClientRequestTests
{
    [Fact]
    public async Task SendWebApiRequestAsync_AddsQueryCookieAndRateLimit()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        const string cloudAccountUid = "115001";
        var handler = new CaptureHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"state":true,"name":"ok"}""", Encoding.UTF8, "application/json"),
            });
        var rateLimiter = new CountingRateLimiter();
        CloudDriveApiClient client = await CreateClientAsync(handler, rateLimiter, userId, cloudAccountUid);

        var context = new CloudDriveApiContext
        {
            Category = "files",
            QueryParameters = new Dictionary<string, string>
            {
                ["cid"] = "0",
                ["limit"] = "1",
            },
        };

        // Act
        TestResponse result = await client.SendWebApiRequestAsync<TestResponse>(
            userId,
            cloudAccountUid,
            HttpMethod.Get,
            "files",
            context,
            CancellationToken.None);

        // Assert
        Assert.Equal("ok", result.Name);
        Assert.Equal(1, rateLimiter.AcquireCount);
        Assert.Equal((userId, cloudAccountUid, "files"), rateLimiter.LastAcquire);

        HttpRequestMessage request = handler.Requests.Single();
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://webapi.115.com/files?cid=0&limit=1", request.RequestUri!.ToString());
        Assert.True(request.Headers.TryGetValues("Cookie", out IEnumerable<string>? cookieValues));
        Assert.Equal("UID=uid; CID=cid; SEID=ussid", cookieValues.Single());
    }

    [Fact]
    public async Task SendUserCenterRequestAsync_RoutesPayDomainAndSendsJsonBody()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        const string cloudAccountUid = "115001";
        var handler = new CaptureHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"state":true,"name":"balance"}""", Encoding.UTF8, "application/json"),
            });
        CloudDriveApiClient client = await CreateClientAsync(handler, new CountingRateLimiter(), userId, cloudAccountUid);

        var context = new CloudDriveApiContext
        {
            TargetDomain = "pay.115.com",
            JsonBody = new { ping = "pong" },
        };

        // Act
        TestResponse result = await client.SendUserCenterRequestAsync<TestResponse>(
            userId,
            cloudAccountUid,
            HttpMethod.Post,
            "?ct=balance&ac=getBalance",
            context,
            CancellationToken.None);

        // Assert
        Assert.Equal("balance", result.Name);
        Assert.Equal(CloudDriveHttpClientNames.Pay, handler.ClientNames.Single());

        HttpRequestMessage request = handler.Requests.Single();
        Assert.Equal("https://pay.115.com/?ct=balance&ac=getBalance", request.RequestUri!.ToString());
        Assert.NotNull(request.Content);
        Assert.Equal("application/json", request.Content.Headers.ContentType!.MediaType);
        Assert.Contains("\"ping\":\"pong\"", await request.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SendWebApiRequestAsync_WhenHttpUnauthorized_MarksCookieInvalidated()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        const string cloudAccountUid = "115001";
        var handler = new CaptureHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var repository = new FakeCookieSessionRepository();
        var notifier = new FakeCookieInvalidationNotifier();
        CloudDriveApiClient client = await CreateClientAsync(
            handler,
            new CountingRateLimiter(),
            userId,
            cloudAccountUid,
            repository,
            notifier);

        // Act
        CloudDriveCookieInvalidatedException exception = await Assert.ThrowsAsync<CloudDriveCookieInvalidatedException>(
            () => client.SendWebApiRequestAsync<TestResponse>(
                userId,
                cloudAccountUid,
                HttpMethod.Get,
                "files",
                new CloudDriveApiContext(),
                CancellationToken.None));

        // Assert
        Assert.Equal("cloud_drive_cookie_invalidated", exception.ErrorCode);
        CookieSession? session = await repository.GetByAccountAsync(userId, cloudAccountUid);
        Assert.NotNull(session);
        Assert.Equal(CookieSessionStatus.Invalidated, session.Status);
        Assert.Equal("http_401", session.InvalidationReason);
        Assert.Single(notifier.Notifications);
    }

    [Fact]
    public async Task SendWebApiRequestAsync_WhenBusinessLoginRequired_MarksCookieInvalidated()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        const string cloudAccountUid = "115001";
        var handler = new CaptureHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"state":false,"errNo":"login_required","error":"login_required"}""",
                    Encoding.UTF8,
                    "application/json"),
            });
        var repository = new FakeCookieSessionRepository();
        var notifier = new FakeCookieInvalidationNotifier();
        var errorAdapter = new StaticErrorFieldAdapter(
            new CloudDriveApiError(null, "login_required", "errNo", new Uri("https://webapi.115.com/files")));
        CloudDriveApiClient client = await CreateClientAsync(
            handler,
            new CountingRateLimiter(),
            userId,
            cloudAccountUid,
            repository,
            notifier,
            errorAdapter);

        // Act
        await Assert.ThrowsAsync<CloudDriveCookieInvalidatedException>(
            () => client.SendWebApiRequestAsync<TestResponse>(
                userId,
                cloudAccountUid,
                HttpMethod.Get,
                "files",
                new CloudDriveApiContext(),
                CancellationToken.None));

        // Assert
        CookieSession? session = await repository.GetByAccountAsync(userId, cloudAccountUid);
        Assert.NotNull(session);
        Assert.Equal(CookieSessionStatus.Invalidated, session.Status);
        Assert.Equal("login_required", session.InvalidationReason);
        Assert.Single(notifier.Notifications);
    }

    private static async Task<CloudDriveApiClient> CreateClientAsync(
        CaptureHttpMessageHandler handler,
        CountingRateLimiter rateLimiter,
        Guid userId,
        string cloudAccountUid,
        FakeCookieSessionRepository? repository = null,
        FakeCookieInvalidationNotifier? notifier = null,
        IErrorFieldAdapter? errorFieldAdapter = null)
    {
        repository ??= new FakeCookieSessionRepository();
        notifier ??= new FakeCookieInvalidationNotifier();
        var secretProtector = new PlainTextSecretProtector();
        IOptions<CloudDriveAppConfig> options = Options.Create(new CloudDriveAppConfig
        {
            CookieEncryptionKeyName = "test",
            WebApiBaseUrl = "https://webapi.115.com/",
            MyApiBaseUrl = "https://my.115.com/",
            VipApiBaseUrl = "https://vip.115.com/",
            PayApiBaseUrl = "https://pay.115.com/",
            OfflineApiBaseUrl = "https://115.com/web/lixian/",
            CloudDriveFeatureEnabled = true,
        });

        var cookieSessionManager = new CookieSessionManager(
            repository,
            secretProtector,
            notifier,
            options,
            NullLogger<CookieSessionManager>.Instance);

        await cookieSessionManager.UpsertSessionAsync(
            userId,
            cloudAccountUid,
            new Domain.ValueObjects.CookieTriplet("uid", "cid", "ussid"),
            cancellationToken: CancellationToken.None);

        return new CloudDriveApiClient(
            new FakeHttpClientFactory(handler),
            cookieSessionManager,
            errorFieldAdapter ?? new StaticErrorFieldAdapter(null),
            new NoopCookieSignatureStrategy(),
            rateLimiter,
            options,
            NullLogger<CloudDriveApiClient>.Instance);
    }

    private sealed record TestResponse(
        [property: JsonPropertyName("state")] bool State,
        [property: JsonPropertyName("name")] string? Name);

    private sealed class CaptureHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string> ClientNames { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(await CloneRequestAsync(request, cancellationToken));
            return response;
        }

        private static async Task<HttpRequestMessage> CloneRequestAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (request.Content is not null)
            {
                byte[] content = await request.Content.ReadAsByteArrayAsync(cancellationToken);
                clone.Content = new ByteArrayContent(content);
                foreach (KeyValuePair<string, IEnumerable<string>> header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return clone;
        }
    }

    private sealed class FakeHttpClientFactory(CaptureHttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            handler.ClientNames.Add(name);
            return new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = name switch
                {
                    CloudDriveHttpClientNames.Web => new Uri("https://webapi.115.com/"),
                    CloudDriveHttpClientNames.My => new Uri("https://my.115.com/"),
                    CloudDriveHttpClientNames.Vip => new Uri("https://vip.115.com/"),
                    CloudDriveHttpClientNames.Pay => new Uri("https://pay.115.com/"),
                    CloudDriveHttpClientNames.Offline => new Uri("https://115.com/web/lixian/"),
                    _ => new Uri("https://webapi.115.com/"),
                },
            };
        }
    }

    private sealed class CountingRateLimiter : ICloudDriveRateLimiter
    {
        public int AcquireCount { get; private set; }

        public (Guid UserId, string CloudAccountUid, string? Category) LastAcquire { get; private set; }

        public List<(Guid UserId, string CloudAccountUid, string? Category)> ThrottleSignals { get; } = [];

        public Task<IDisposable> AcquireAsync(
            Guid userId,
            string cloudAccountUid,
            string? category = null,
            CancellationToken cancellationToken = default)
        {
            AcquireCount++;
            LastAcquire = (userId, cloudAccountUid, category);
            return Task.FromResult<IDisposable>(NullDisposable.Instance);
        }

        public void RecordThrottleSignal(Guid userId, string cloudAccountUid, string? category = null)
        {
            ThrottleSignals.Add((userId, cloudAccountUid, category));
        }
    }

    private sealed class StaticErrorFieldAdapter(CloudDriveApiError? error) : IErrorFieldAdapter
    {
        public CloudDriveApiError? TryParse(Uri requestUri, JsonDocument responseJson) => error;
    }

    private sealed class NoopCookieSignatureStrategy : ICookieSignatureStrategy
    {
        public CookieSignature Generate(Guid userId, string cloudAccountUid, string requestBody) =>
            new("sign", 1, "uid");
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

        public Task<int> DeleteAsync(CookieSession entity, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_store.Remove(entity.Id) ? 1 : 0);
        }

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
