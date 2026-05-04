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
    public async Task SendWebApiRequestAsync_WhenQueryRequest_AddsCookieAndRateLimitAsync()
    {
        // 准备
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

        // 执行
        TestResponse result = await client.SendWebApiRequestAsync<TestResponse>(
            userId,
            cloudAccountUid,
            HttpMethod.Get,
            "files",
            context,
            CancellationToken.None);

        // 断言
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
    public async Task SendUserCenterRequestAsync_WhenPayDomainWithJsonBody_RoutesAndSendsJsonAsync()
    {
        // 准备
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

        // 执行
        TestResponse result = await client.SendUserCenterRequestAsync<TestResponse>(
            userId,
            cloudAccountUid,
            HttpMethod.Post,
            "?ct=balance&ac=getBalance",
            context,
            CancellationToken.None);

        // 断言
        Assert.Equal("balance", result.Name);
        Assert.Equal(CloudDriveHttpClientNames.Pay, handler.ClientNames.Single());

        HttpRequestMessage request = handler.Requests.Single();
        Assert.Equal("https://pay.115.com/?ct=balance&ac=getBalance", request.RequestUri!.ToString());
        Assert.NotNull(request.Content);
        Assert.Equal("application/json", request.Content.Headers.ContentType!.MediaType);
        Assert.Contains("\"ping\":\"pong\"", await request.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SendWebApiRequestAsync_WhenHttpUnauthorized_MarksCookieInvalidatedAsync()
    {
        // 准备
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

        // 执行
        CloudDriveCookieInvalidatedException exception = await Assert.ThrowsAsync<CloudDriveCookieInvalidatedException>(
            () => client.SendWebApiRequestAsync<TestResponse>(
                userId,
                cloudAccountUid,
                HttpMethod.Get,
                "files",
                new CloudDriveApiContext(),
                CancellationToken.None));

        // 断言
        Assert.Equal("cloud_drive_cookie_invalidated", exception.ErrorCode);
        CookieSession? session = await repository.GetByAccountAsync(userId, cloudAccountUid);
        Assert.NotNull(session);
        Assert.Equal(CookieSessionStatus.Invalidated, session.Status);
        Assert.Equal("http_401", session.InvalidationReason);
        Assert.Single(notifier.Notifications);
    }

    [Fact]
    public async Task SendOfflineApiRequestAsync_WhenSignedRequest_AddsLixianQueryAndSignatureBodyAsync()
    {
        // 准备
        Guid userId = Guid.NewGuid();
        const string cloudAccountUid = "115001";
        var handler = new CaptureHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"state":true,"name":"offline"}""", Encoding.UTF8, "application/json"),
            });
        var signatureStrategy = new CaptureCookieSignatureStrategy(new CookieSignature("signed-value", 1776518000, "115-uid"));
        CloudDriveApiClient client = await CreateClientAsync(
            handler,
            new CountingRateLimiter(),
            userId,
            cloudAccountUid,
            cookieSignatureStrategy: signatureStrategy);

        var context = new CloudDriveApiContext
        {
            Category = "offline",
            FormFields = new Dictionary<string, string>
            {
                ["page"] = "1",
                ["stat"] = "0",
            },
        };

        // 执行
        TestResponse result = await client.SendOfflineApiRequestAsync<TestResponse>(
            userId,
            cloudAccountUid,
            HttpMethod.Post,
            "task_lists",
            context,
            CancellationToken.None);

        // 断言
        Assert.Equal("offline", result.Name);
        Assert.Equal(CloudDriveHttpClientNames.Offline, handler.ClientNames.Single());
        Assert.Equal("page=1&stat=0", signatureStrategy.LastRequestBody);

        HttpRequestMessage request = handler.Requests.Single();
        Assert.Equal("https://115.com/web/lixian/?ct=lixian&ac=task_lists", request.RequestUri!.ToString());
        Assert.True(request.Headers.TryGetValues("Cookie", out IEnumerable<string>? cookieValues));
        Assert.Equal("UID=uid; CID=cid; SEID=ussid", cookieValues.Single());

        string body = await request.Content!.ReadAsStringAsync();
        Assert.Contains("page=1", body);
        Assert.Contains("stat=0", body);
        Assert.Contains("sign=signed-value", body);
        Assert.Contains("time=1776518000", body);
        Assert.Contains("uid=115-uid", body);
    }

    [Fact]
    public async Task SendOfflineApiRequestAsync_WhenSignatureUnavailable_ThrowsConfigurationExceptionAsync()
    {
        // 准备
        Guid userId = Guid.NewGuid();
        const string cloudAccountUid = "115001";
        var handler = new CaptureHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        CloudDriveApiClient client = await CreateClientAsync(
            handler,
            new CountingRateLimiter(),
            userId,
            cloudAccountUid,
            cookieSignatureStrategy: new CaptureCookieSignatureStrategy(default));

        // 执行
        CloudDriveConfigurationException exception = await Assert.ThrowsAsync<CloudDriveConfigurationException>(
            () => client.SendOfflineApiRequestAsync<TestResponse>(
                userId,
                cloudAccountUid,
                HttpMethod.Post,
                "task_lists",
                new CloudDriveApiContext(),
                CancellationToken.None));

        // 断言
        Assert.Equal("offline_signature_not_available", exception.ErrorCode);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SendUploadInitRequestAsync_WhenFormRequest_UsesUploadClientAndCookieAsync()
    {
        // 准备
        Guid userId = Guid.NewGuid();
        const string cloudAccountUid = "115001";
        var handler = new CaptureHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"state":true,"name":"upload-init"}""", Encoding.UTF8, "text/html"),
            });
        CloudDriveApiClient client = await CreateClientAsync(handler, new CountingRateLimiter(), userId, cloudAccountUid);

        var context = new CloudDriveApiContext
        {
            Category = "upload",
            FormFields = new Dictionary<string, string>
            {
                ["userid"] = "115-uid",
                ["filename"] = "demo.txt",
                ["filesize"] = "4",
                ["target"] = "U_1_0",
            },
        };

        // 执行
        TestResponse result = await client.SendUploadInitRequestAsync<TestResponse>(
            userId,
            cloudAccountUid,
            context,
            CancellationToken.None);

        // 断言
        Assert.Equal("upload-init", result.Name);
        Assert.Equal(CloudDriveHttpClientNames.Upload, handler.ClientNames.Single());

        HttpRequestMessage request = handler.Requests.Single();
        Assert.Equal("https://uplb.115.com/3.0/sampleinitupload.php", request.RequestUri!.ToString());
        Assert.True(request.Headers.TryGetValues("Cookie", out IEnumerable<string>? cookieValues));
        Assert.Equal("UID=uid; CID=cid; SEID=ussid", cookieValues.Single());

        string body = await request.Content!.ReadAsStringAsync();
        Assert.Contains("userid=115-uid", body);
        Assert.Contains("target=U_1_0", body);
    }

    [Fact]
    public async Task UploadFileAsync_WhenOssUpload_DoesNotInjectCookieAndSendsMultipartAsync()
    {
        // 准备
        Guid userId = Guid.NewGuid();
        const string cloudAccountUid = "115001";
        var handler = new CaptureHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"state":true,"name":"uploaded"}""", Encoding.UTF8, "application/json"),
            });
        CloudDriveApiClient client = await CreateClientAsync(handler, new CountingRateLimiter(), userId, cloudAccountUid);

        await using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("demo"));
        var formFields = new Dictionary<string, string>
        {
            ["key"] = "object-key",
            ["policy"] = "policy",
            ["OSSAccessKeyId"] = "access",
            ["signature"] = "signature",
            ["callback"] = "callback",
        };

        // 执行
        TestResponse result = await client.UploadFileAsync<TestResponse>(
            userId,
            cloudAccountUid,
            "https://fhnfile.oss-cn-shenzhen.aliyuncs.com/",
            fileStream,
            "demo.txt",
            formFields,
            CancellationToken.None);

        // 断言
        Assert.Equal("uploaded", result.Name);
        Assert.Equal(CloudDriveHttpClientNames.UploadDirect, handler.ClientNames.Single());

        HttpRequestMessage request = handler.Requests.Single();
        Assert.Equal("https://fhnfile.oss-cn-shenzhen.aliyuncs.com/", request.RequestUri!.ToString());
        Assert.False(request.Headers.Contains("Cookie"));
        Assert.NotNull(request.Content);
        Assert.StartsWith("multipart/form-data", request.Content.Headers.ContentType!.MediaType);

        string body = await request.Content.ReadAsStringAsync();
        Assert.Contains("name=key", body);
        Assert.Contains("object-key", body);
        Assert.Contains("name=file", body);
        Assert.Contains("filename=demo.txt", body);
        Assert.Contains("demo", body);
    }

    [Fact]
    public async Task UploadFileAsync_WhenCallbackBusinessFailure_ThrowsUploadExceptionAsync()
    {
        // 准备
        Guid userId = Guid.NewGuid();
        const string cloudAccountUid = "115001";
        var handler = new CaptureHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"state":false,"code":502,"message":"upload rejected"}""", Encoding.UTF8, "application/json"),
            });
        var errorAdapter = new StaticErrorFieldAdapter(
            new CloudDriveApiError(502, "upload rejected", "message", new Uri("https://oss.example.com/")));
        CloudDriveApiClient client = await CreateClientAsync(
            handler,
            new CountingRateLimiter(),
            userId,
            cloudAccountUid,
            errorFieldAdapter: errorAdapter);

        await using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("demo"));

        // 执行
        CloudDriveUploadException exception = await Assert.ThrowsAsync<CloudDriveUploadException>(
            () => client.UploadFileAsync<TestResponse>(
                userId,
                cloudAccountUid,
                "https://oss.example.com/",
                fileStream,
                "demo.txt",
                cancellationToken: CancellationToken.None));

        // 断言
        Assert.Equal("cloud_drive_upload_failed", exception.ErrorCode);
    }

    [Fact]
    public async Task SendWebApiRequestAsync_WhenUnknownBusinessErrorShape_ThrowsUnknownErrorShapeAsync()
    {
        // 准备
        Guid userId = Guid.NewGuid();
        const string cloudAccountUid = "115001";
        var handler = new CaptureHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"state":false,"unexpected":"failure"}""", Encoding.UTF8, "application/json"),
            });
        CloudDriveApiClient client = await CreateClientAsync(handler, new CountingRateLimiter(), userId, cloudAccountUid);

        // 执行
        CloudDriveApiException exception = await Assert.ThrowsAsync<CloudDriveApiException>(
            () => client.SendWebApiRequestAsync<TestResponse>(
                userId,
                cloudAccountUid,
                HttpMethod.Get,
                "files",
                new CloudDriveApiContext(),
                CancellationToken.None));

        // 断言
        Assert.Equal("unknown_error_shape", exception.ErrorCode);
    }

    [Fact]
    public async Task SendWebApiRequestAsync_WhenBusinessLoginRequired_MarksCookieInvalidatedAsync()
    {
        // 准备
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

        // 执行
        await Assert.ThrowsAsync<CloudDriveCookieInvalidatedException>(
            () => client.SendWebApiRequestAsync<TestResponse>(
                userId,
                cloudAccountUid,
                HttpMethod.Get,
                "files",
                new CloudDriveApiContext(),
                CancellationToken.None));

        // 断言
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
        IErrorFieldAdapter? errorFieldAdapter = null,
        ICookieSignatureStrategy? cookieSignatureStrategy = null)
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
            cookieSignatureStrategy ?? new NoopCookieSignatureStrategy(),
            rateLimiter,
            new PassThroughSensitiveFieldsFilter(),
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
                    CloudDriveHttpClientNames.Upload => new Uri("https://uplb.115.com/"),
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

        public void Reset(Guid userId, string cloudAccountUid, string? category = null) { }
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

    private sealed class CaptureCookieSignatureStrategy(CookieSignature signature) : ICookieSignatureStrategy
    {
        public string? LastRequestBody { get; private set; }

        public CookieSignature Generate(Guid userId, string cloudAccountUid, string requestBody)
        {
            LastRequestBody = requestBody;
            return signature;
        }
    }

    private sealed class PlainTextSecretProtector : ICloudDriveSecretProtector
    {
        public string Protect(string keyName, string plaintext) => plaintext;

        public string Unprotect(string keyName, string ciphertext, int? keyVersion = null) => ciphertext;
    }

    private sealed class PassThroughSensitiveFieldsFilter : ICloudDriveSensitiveFieldsFilter
    {
        public string Redact(string? value) => value ?? string.Empty;

        public IReadOnlyDictionary<string, string?> RedactFields(IReadOnlyDictionary<string, string?> fields) => fields;
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

        public Task<IReadOnlyList<CookieSession>> GetAvailableSessionsAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<CookieSession> sessions = _store.Values
                .Where(item => item.Status is Domain.Enums.CookieSessionStatus.Active
                    or Domain.Enums.CookieSessionStatus.Invalidated)
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
