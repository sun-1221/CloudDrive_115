using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Auth;

using Domain.Exceptions;

using Infrastructure.Http.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Http;

/// <summary>
/// Infrastructure implementation of the 115 web API gateway.
/// </summary>
internal sealed class CloudDriveApiClient(
    IHttpClientFactory httpClientFactory,
    CookieSessionManager cookieSessionManager,
    IErrorFieldAdapter errorFieldAdapter,
    ICookieSignatureStrategy cookieSignatureStrategy,
    ICloudDriveRateLimiter rateLimiter,
    IOptions<CloudDriveAppConfig> configOptions,
    ILogger<CloudDriveApiClient> logger) : ICloudDriveOpenApiGateway
{
    internal JsonSerializerOptions JsonSerializerOptions { get; } = CloudDrive115JsonSerializerOptions.Defaults;

    internal CloudDriveAppConfig Config => configOptions.Value;

    public Task<T> SendWebApiRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        HttpMethod method,
        string endpoint,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default)
    {
        return SendAuthenticatedApiRequestAsync<T>(
            userId,
            cloudAccountUid,
            method,
            endpoint,
            context,
            CloudDriveHttpClientNames.Web,
            cancellationToken);
    }

    public Task<T> SendUserCenterRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        HttpMethod method,
        string endpoint,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default)
    {
        string clientName = ResolveUserCenterClientName(context.TargetDomain);

        return SendAuthenticatedApiRequestAsync<T>(
            userId,
            cloudAccountUid,
            method,
            endpoint,
            context,
            clientName,
            cancellationToken);
    }

    public Task<T> SendOfflineApiRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        HttpMethod method,
        string endpoint,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default)
    {
        return SendNotImplementedAsync<T>(
            userId,
            cloudAccountUid,
            method,
            endpoint,
            context,
            CloudDriveHttpClientNames.Offline,
            cancellationToken);
    }

    public Task<T> SendUploadInitRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default)
    {
        return SendNotImplementedAsync<T>(
            userId,
            cloudAccountUid,
            HttpMethod.Post,
            "3.0/sampleinitupload.php",
            context,
            CloudDriveHttpClientNames.Web,
            cancellationToken);
    }

    public Task<T> UploadFileAsync<T>(
        Guid userId,
        string cloudAccountUid,
        string uploadUrl,
        Stream fileStream,
        string fileName,
        IDictionary<string, string>? formFields = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uploadUrl);
        ArgumentNullException.ThrowIfNull(fileStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        logger.LogDebug(
            "CloudDriveApiClient OSS upload skeleton invoked UserId={UserId}, CloudAccountUid={CloudAccountUid}, FileName={FileName}",
            userId,
            cloudAccountUid,
            fileName);

        throw new CloudDriveApiException(
            "OSS 直传请求封装将在基础设施任务 4.4 中实现",
            "cloud_drive_api_client_upload_not_implemented");
    }

    internal HttpClient CreateClient(string clientName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
        return httpClientFactory.CreateClient(clientName);
    }

    internal static string ResolveUserCenterClientName(string? targetDomain)
    {
        if (string.IsNullOrWhiteSpace(targetDomain))
        {
            return CloudDriveHttpClientNames.My;
        }

        string normalized = targetDomain.Trim().ToLowerInvariant();

        if (normalized.Contains("vip.115.com", StringComparison.Ordinal))
        {
            return CloudDriveHttpClientNames.Vip;
        }

        if (normalized.Contains("pay.115.com", StringComparison.Ordinal))
        {
            return CloudDriveHttpClientNames.Pay;
        }

        if (normalized.Contains("my.115.com", StringComparison.Ordinal))
        {
            return CloudDriveHttpClientNames.My;
        }

        if (normalized.Contains("webapi.115.com", StringComparison.Ordinal))
        {
            return CloudDriveHttpClientNames.Web;
        }

        return CloudDriveHttpClientNames.My;
    }

    private Task<T> SendNotImplementedAsync<T>(
        Guid userId,
        string cloudAccountUid,
        HttpMethod method,
        string endpoint,
        CloudDriveApiContext context,
        string clientName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cloudAccountUid);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        _ = cookieSessionManager;
        _ = errorFieldAdapter;
        _ = cookieSignatureStrategy;

        logger.LogDebug(
            "CloudDriveApiClient skeleton invoked ClientName={ClientName}, UserId={UserId}, CloudAccountUid={CloudAccountUid}, Method={Method}, Endpoint={Endpoint}",
            clientName,
            userId,
            cloudAccountUid,
            method,
            endpoint);

        throw new CloudDriveApiException(
            "115 网页端请求封装将在基础设施任务 4.3/4.4 中实现",
            "cloud_drive_api_client_request_not_implemented");
    }

    private async Task<T> SendAuthenticatedApiRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        HttpMethod method,
        string endpoint,
        CloudDriveApiContext context,
        string clientName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cloudAccountUid);
        ArgumentNullException.ThrowIfNull(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentNullException.ThrowIfNull(context);

        using IDisposable permit = await rateLimiter.AcquireAsync(
            userId,
            cloudAccountUid,
            context.Category,
            cancellationToken);

        string cookieHeader = await cookieSessionManager.GetValidCookieHeaderAsync(
            userId,
            cloudAccountUid,
            cancellationToken);

        HttpClient client = CreateClient(clientName);
        using HttpRequestMessage request = CreateRequest(method, endpoint, context);
        request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);

        Uri requestUri = ResolveRequestUri(client, request);
        using HttpResponseMessage response = await SendRequestAsync(client, request, requestUri, cancellationToken);

        await EnsureHttpSuccessAsync(
            response,
            userId,
            cloudAccountUid,
            context.Category,
            requestUri,
            cancellationToken);

        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return DeserializeResponse<T>(responseContent, requestUri);
        }

        using JsonDocument responseJson = ParseJsonDocument(responseContent, requestUri);
        CloudDriveApiError? apiError = errorFieldAdapter.TryParse(requestUri, responseJson);
        bool stateIndicatesFailure = IsStateFalse(responseJson);

        if (apiError is not null || stateIndicatesFailure)
        {
            await ThrowMappedBusinessExceptionAsync(
                apiError,
                responseJson,
                userId,
                cloudAccountUid,
                context.Category,
                requestUri,
                cancellationToken);
        }

        return DeserializeResponse<T>(responseContent, requestUri);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint, CloudDriveApiContext context)
    {
        string requestUri = AppendQueryString(endpoint, context.QueryParameters);

        if (context.FormFields is { Count: > 0 })
        {
            return HttpRequestBuilders.CreateFormUrlEncoded(method, requestUri, context.FormFields);
        }

        if (context.JsonBody is not null)
        {
            return HttpRequestBuilders.CreateJson(method, requestUri, context.JsonBody, JsonSerializerOptions);
        }

        return new HttpRequestMessage(method, requestUri);
    }

    private async Task<HttpResponseMessage> SendRequestAsync(
        HttpClient client,
        HttpRequestMessage request,
        Uri requestUri,
        CancellationToken cancellationToken)
    {
        try
        {
            return await client.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CloudDriveNetworkException(
                $"115 网页端 API 请求超时 Endpoint={requestUri.AbsolutePath}",
                ex,
                "cloud_drive_api_timeout");
        }
        catch (HttpRequestException ex)
        {
            throw new CloudDriveNetworkException(
                $"115 网页端 API 通信失败 Endpoint={requestUri.AbsolutePath}",
                ex,
                "cloud_drive_api_network_error");
        }
    }

    private async Task EnsureHttpSuccessAsync(
        HttpResponseMessage response,
        Guid userId,
        string cloudAccountUid,
        string? category,
        Uri requestUri,
        CancellationToken cancellationToken)
    {
        int statusCode = (int)response.StatusCode;

        if (statusCode is 401 or 403)
        {
            await cookieSessionManager.MarkInvalidatedAsync(
                userId,
                cloudAccountUid,
                $"http_{statusCode}",
                cancellationToken);

            throw new CloudDriveCookieInvalidatedException(
                $"115 Cookie 会话已失效 Endpoint={requestUri.AbsolutePath}, HttpStatus={statusCode}",
                "cloud_drive_cookie_invalidated");
        }

        if (statusCode == 429)
        {
            rateLimiter.RecordThrottleSignal(userId, cloudAccountUid, category);
            throw new CloudDriveRateLimitException(
                $"115 网页端 API 触发频控 Endpoint={requestUri.AbsolutePath}",
                "cloud_drive_upstream_rate_limited");
        }

        if (statusCode == 404)
        {
            throw new CloudDriveFileNotFoundException(
                $"115 网页端资源不存在 Endpoint={requestUri.AbsolutePath}");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new CloudDriveApiException(
                $"115 网页端 API 返回失败 HTTP {statusCode}, Endpoint={requestUri.AbsolutePath}");
        }
    }

    private async Task ThrowMappedBusinessExceptionAsync(
        CloudDriveApiError? apiError,
        JsonDocument responseJson,
        Guid userId,
        string cloudAccountUid,
        string? category,
        Uri requestUri,
        CancellationToken cancellationToken)
    {
        if (IsLoginInvalidationError(apiError, responseJson))
        {
            await cookieSessionManager.MarkInvalidatedAsync(
                userId,
                cloudAccountUid,
                apiError?.Message ?? "login_required",
                cancellationToken);

            throw new CloudDriveCookieInvalidatedException(
                $"115 Cookie 会话已失效 Endpoint={requestUri.AbsolutePath}",
                "cloud_drive_cookie_invalidated");
        }

        if (IsRateLimitError(apiError, responseJson))
        {
            rateLimiter.RecordThrottleSignal(userId, cloudAccountUid, category);
            throw new CloudDriveRateLimitException(
                $"115 网页端 API 触发频控 Endpoint={requestUri.AbsolutePath}",
                "cloud_drive_upstream_rate_limited");
        }

        if (apiError?.Code == 404)
        {
            throw new CloudDriveFileNotFoundException(
                apiError.Message ?? $"115 网页端资源不存在 Endpoint={requestUri.AbsolutePath}");
        }

        throw new CloudDriveApiException(
            apiError?.Message ?? $"115 网页端 API 返回业务失败 Endpoint={requestUri.AbsolutePath}");
    }

    private static bool IsStateFalse(JsonDocument responseJson)
    {
        if (!responseJson.RootElement.TryGetProperty("state", out JsonElement stateElement))
        {
            return false;
        }

        bool? state = JsonElementReaders.ReadBoolean(stateElement);
        return state == false;
    }

    private static bool IsLoginInvalidationError(CloudDriveApiError? apiError, JsonDocument responseJson)
    {
        if (apiError?.Code is 401 or 403)
        {
            return true;
        }

        string combined = string.Join(
            ' ',
            apiError?.Message,
            ReadKnownErrorValue(responseJson, "errNo"),
            ReadKnownErrorValue(responseJson, "errno"),
            ReadKnownErrorValue(responseJson, "errCode"),
            ReadKnownErrorValue(responseJson, "code"),
            ReadKnownErrorValue(responseJson, "error"),
            ReadKnownErrorValue(responseJson, "message"),
            ReadKnownErrorValue(responseJson, "msg"));

        return combined.Contains("login_required", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("auth_expired", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("未登录", StringComparison.Ordinal)
            || combined.Contains("登录", StringComparison.Ordinal);
    }

    private static bool IsRateLimitError(CloudDriveApiError? apiError, JsonDocument responseJson)
    {
        if (apiError?.Code == 429)
        {
            return true;
        }

        string combined = string.Join(
            ' ',
            apiError?.Message,
            ReadKnownErrorValue(responseJson, "errNo"),
            ReadKnownErrorValue(responseJson, "errno"),
            ReadKnownErrorValue(responseJson, "errCode"),
            ReadKnownErrorValue(responseJson, "code"),
            ReadKnownErrorValue(responseJson, "error"),
            ReadKnownErrorValue(responseJson, "message"),
            ReadKnownErrorValue(responseJson, "msg"));

        return combined.Contains("rate", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("limit", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("too_many", StringComparison.OrdinalIgnoreCase)
            || combined.Contains("频控", StringComparison.Ordinal)
            || combined.Contains("频繁", StringComparison.Ordinal);
    }

    private static string? ReadKnownErrorValue(JsonDocument responseJson, string propertyName)
    {
        return responseJson.RootElement.TryGetProperty(propertyName, out JsonElement property)
            ? JsonElementReaders.ReadString(property)
            : null;
    }

    private JsonDocument ParseJsonDocument(string responseContent, Uri requestUri)
    {
        try
        {
            return JsonDocument.Parse(responseContent);
        }
        catch (JsonException ex)
        {
            throw new CloudDriveApiException(
                $"115 网页端 API 返回非 JSON 响应 Endpoint={requestUri.AbsolutePath}",
                ex,
                "cloud_drive_api_invalid_json");
        }
    }

    private T DeserializeResponse<T>(string responseContent, Uri requestUri)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)responseContent;
        }

        if (string.IsNullOrWhiteSpace(responseContent))
        {
            throw new CloudDriveApiException(
                $"115 网页端 API 返回空响应 Endpoint={requestUri.AbsolutePath}",
                "cloud_drive_api_empty_response");
        }

        if (typeof(T) == typeof(JsonDocument))
        {
            return (T)(object)JsonDocument.Parse(responseContent);
        }

        if (typeof(T) == typeof(JsonElement))
        {
            using JsonDocument document = JsonDocument.Parse(responseContent);
            return (T)(object)document.RootElement.Clone();
        }

        try
        {
            T? result = JsonSerializer.Deserialize<T>(responseContent, JsonSerializerOptions);
            return result ?? throw new CloudDriveApiException(
                $"115 网页端 API 反序列化结果为空 Endpoint={requestUri.AbsolutePath}",
                "cloud_drive_api_null_response");
        }
        catch (JsonException ex)
        {
            throw new CloudDriveApiException(
                $"115 网页端 API 响应反序列化失败 Endpoint={requestUri.AbsolutePath}",
                ex,
                "cloud_drive_api_deserialization_failed");
        }
    }

    private static Uri ResolveRequestUri(HttpClient client, HttpRequestMessage request)
    {
        if (request.RequestUri is null)
        {
            throw new CloudDriveApiException(
                "115 网页端 API 请求 URI 缺失",
                "cloud_drive_api_request_uri_missing");
        }

        if (request.RequestUri.IsAbsoluteUri)
        {
            return request.RequestUri;
        }

        if (client.BaseAddress is null)
        {
            throw new CloudDriveConfigurationException(
                "115 网页端 API HttpClient 未配置 BaseAddress",
                "cloud_drive_api_base_address_missing");
        }

        return new Uri(client.BaseAddress, request.RequestUri);
    }

    private static string AppendQueryString(
        string endpoint,
        IDictionary<string, string>? queryParameters)
    {
        if (queryParameters is null || queryParameters.Count == 0)
        {
            return endpoint;
        }

        List<string> pairs = [];
        foreach (KeyValuePair<string, string> parameter in queryParameters)
        {
            string encodedKey = Uri.EscapeDataString(parameter.Key);
            string encodedValue = Uri.EscapeDataString(parameter.Value);
            pairs.Add($"{encodedKey}={encodedValue}");
        }

        if (pairs.Count == 0)
        {
            return endpoint;
        }

        char separator = endpoint.Contains('?') ? '&' : '?';
        return $"{endpoint}{separator}{string.Join('&', pairs)}";
    }
}
