using System.Diagnostics;
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
/// 115 网页端 API 网关的基础设施实现。
/// </summary>
internal sealed class CloudDriveApiClient(
    IHttpClientFactory httpClientFactory,
    CookieSessionManager cookieSessionManager,
    IErrorFieldAdapter errorFieldAdapter,
    ICookieSignatureStrategy cookieSignatureStrategy,
    ICloudDriveRateLimiter rateLimiter,
    ICloudDriveSensitiveFieldsFilter sensitiveFieldsFilter,
    IOptions<CloudDriveAppConfig> configOptions,
    ILogger<CloudDriveApiClient> logger) : ICloudDriveOpenApiGateway
{
    /// <summary>
    /// 获取 115 响应模型使用的 JSON 序列化选项。
    /// </summary>
    internal JsonSerializerOptions JsonSerializerOptions { get; } = CloudDrive115JsonSerializerOptions.Defaults;

    /// <summary>
    /// 获取当前 115 云盘应用配置。
    /// </summary>
    internal CloudDriveAppConfig Config => configOptions.Value;

    /// <summary>
    /// 发送 115 网页端 API 请求。
    /// </summary>
    /// <typeparam name="T">响应反序列化目标类型。</typeparam>
    /// <param name="userId">当前系统用户标识。</param>
    /// <param name="cloudAccountUid">115 账号 UID。</param>
    /// <param name="method">HTTP 方法。</param>
    /// <param name="endpoint">相对或绝对请求端点。</param>
    /// <param name="context">115 API 请求上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>反序列化后的 115 API 响应。</returns>
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

    /// <summary>
    /// 发送 115 用户中心 API 请求。
    /// </summary>
    /// <typeparam name="T">响应反序列化目标类型。</typeparam>
    /// <param name="userId">当前系统用户标识。</param>
    /// <param name="cloudAccountUid">115 账号 UID。</param>
    /// <param name="method">HTTP 方法。</param>
    /// <param name="endpoint">相对或绝对请求端点。</param>
    /// <param name="context">115 API 请求上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>反序列化后的 115 API 响应。</returns>
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

    /// <summary>
    /// 发送 115 云下载 API 请求，并合并 Cookie 签名字段。
    /// </summary>
    /// <typeparam name="T">响应反序列化目标类型。</typeparam>
    /// <param name="userId">当前系统用户标识。</param>
    /// <param name="cloudAccountUid">115 账号 UID。</param>
    /// <param name="method">HTTP 方法。</param>
    /// <param name="endpoint">云下载操作端点或 ac 操作名。</param>
    /// <param name="context">115 API 请求上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>反序列化后的 115 API 响应。</returns>
    public Task<T> SendOfflineApiRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        HttpMethod method,
        string endpoint,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentNullException.ThrowIfNull(context);

        string action = ResolveOfflineAction(endpoint, context.QueryParameters);

        // 签名输入必须来自最终表单字段副本，避免后续补 sign/time/uid 时污染调用方传入的上下文。
        IDictionary<string, string> formFields = MergeFormFields(context.FormFields);
        string signatureRequestBody = SerializeFormFields(formFields);
        CookieSignature signature = cookieSignatureStrategy.Generate(userId, cloudAccountUid, signatureRequestBody);

        // 云下载签名任一核心字段缺失时不能降级发送，否则上游会把配置错误伪装成普通业务失败。
        if (string.IsNullOrWhiteSpace(signature.Sign)
            || string.IsNullOrWhiteSpace(signature.Uid)
            || signature.Time <= 0)
        {
            throw new CloudDriveConfigurationException(
                "115 云下载签名策略不可用",
                "offline_signature_not_available");
        }

        formFields["sign"] = signature.Sign;
        formFields["time"] = signature.Time.ToString(System.Globalization.CultureInfo.InvariantCulture);
        formFields["uid"] = signature.Uid;

        // 115 云下载统一使用 ct=lixian + ac=操作名；传入完整 URL 时也要收敛到这个查询结构。
        IDictionary<string, string> queryParameters = MergeQueryParameters(context.QueryParameters);
        queryParameters["ct"] = "lixian";
        queryParameters["ac"] = action;

        var signedContext = new CloudDriveApiContext
        {
            TargetDomain = context.TargetDomain,
            QueryParameters = queryParameters,
            FormFields = formFields,
            Category = context.Category,
        };

        // Offline 命名客户端的 BaseAddress 已指向 /web/lixian/，实际动作由 ct/ac 查询参数决定。
        return SendAuthenticatedApiRequestAsync<T>(
            userId,
            cloudAccountUid,
            method,
            ".",
            signedContext,
            CloudDriveHttpClientNames.Offline,
            cancellationToken);
    }

    /// <summary>
    /// 发送 115 上传初始化请求。
    /// </summary>
    /// <typeparam name="T">响应反序列化目标类型。</typeparam>
    /// <param name="userId">当前系统用户标识。</param>
    /// <param name="cloudAccountUid">115 账号 UID。</param>
    /// <param name="context">115 API 请求上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>秒传结果或 OSS 直传凭证响应。</returns>
    public Task<T> SendUploadInitRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default)
    {
        return SendAuthenticatedApiRequestAsync<T>(
            userId,
            cloudAccountUid,
            HttpMethod.Post,
            "3.0/sampleinitupload.php",
            context,
            CloudDriveHttpClientNames.Upload,
            cancellationToken);
    }

    /// <summary>
    /// 发送 OSS 文件直传请求。
    /// </summary>
    /// <typeparam name="T">响应反序列化目标类型。</typeparam>
    /// <param name="userId">当前系统用户标识。</param>
    /// <param name="cloudAccountUid">115 账号 UID。</param>
    /// <param name="uploadUrl">OSS 上传地址。</param>
    /// <param name="fileStream">待上传文件流。</param>
    /// <param name="fileName">上传文件名。</param>
    /// <param name="formFields">OSS 表单字段。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>反序列化后的 OSS 上传响应。</returns>
    public async Task<T> UploadFileAsync<T>(
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

        // OSS 直传不需要 115 Cookie，但仍占用上传链路额度，需要纳入同一套用户级频控。
        using IDisposable permit = await rateLimiter.AcquireAsync(
            userId,
            cloudAccountUid,
            "upload",
            cancellationToken);

        // uploadUrl 来自上传初始化返回的绝对 OSS 地址，不能附加 115 BaseAddress 或 Cookie。
        HttpClient client = CreateClient(CloudDriveHttpClientNames.UploadDirect);
        using HttpRequestMessage request = HttpRequestBuilders.CreateMultipartForm(
            HttpMethod.Post,
            uploadUrl,
            multipart =>
            {
                if (formFields is not null)
                {
                    foreach (KeyValuePair<string, string> field in formFields)
                    {
                        multipart.Add(new StringContent(field.Value), field.Key);
                    }
                }

                var fileContent = new StreamContent(fileStream);
                multipart.Add(fileContent, "file", fileName);
            });

        Uri requestUri = ResolveRequestUri(client, request);
        var stopwatch = Stopwatch.StartNew();
        using HttpResponseMessage response = await SendRequestAsync(client, request, requestUri, cancellationToken);
        stopwatch.Stop();

        if (!response.IsSuccessStatusCode)
        {
            // OSS 失败响应可能是 XML/HTML，先按 HTTP 状态映射，避免误走 115 JSON 业务错误解析。
            LogApiCallSummary(request, requestUri, (int)response.StatusCode, null, stopwatch.ElapsedMilliseconds);

            throw new CloudDriveUploadException(
                $"115 OSS 直传失败 HTTP {(int)response.StatusCode}, Endpoint={requestUri.Host}",
                "cloud_drive_upload_failed");
        }

        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            throw new CloudDriveUploadException(
                $"115 OSS 直传返回空响应 Endpoint={requestUri.Host}",
                "cloud_drive_upload_empty_response");
        }

        using JsonDocument responseJson = ParseJsonDocument(responseContent, requestUri);
        CloudDriveApiError? apiError = errorFieldAdapter.TryParse(requestUri, responseJson);
        bool stateIndicatesFailure = IsStateFalse(responseJson);

        if (apiError is not null || stateIndicatesFailure)
        {
            // 上传接口可能 HTTP 200 但业务失败，统一映射为上传异常，避免调用方误判为普通 API 错误。
            LogApiCallSummary(
                request,
                requestUri,
                (int)response.StatusCode,
                apiError?.Code?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                stopwatch.ElapsedMilliseconds);

            throw new CloudDriveUploadException(
                apiError?.Message ?? $"115 OSS 直传返回业务失败 Endpoint={requestUri.Host}",
                "cloud_drive_upload_failed");
        }

        LogApiCallSummary(request, requestUri, (int)response.StatusCode, null, stopwatch.ElapsedMilliseconds);

        return DeserializeResponse<T>(responseContent, requestUri);
    }

    /// <summary>
    /// 根据命名客户端名称创建 HTTP 客户端。
    /// </summary>
    internal HttpClient CreateClient(string clientName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
        return httpClientFactory.CreateClient(clientName);
    }

    /// <summary>
    /// 根据用户中心目标域名解析命名客户端名称。
    /// </summary>
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

    /// <summary>
    /// 发送需要 Cookie 注入、限流、脱敏日志和错误映射的 115 API 请求。
    /// </summary>
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

        // 限流放在 Cookie 读取之前，避免频控场景下仍频繁访问会话存储或触发刷新逻辑。
        using IDisposable permit = await rateLimiter.AcquireAsync(
            userId,
            cloudAccountUid,
            context.Category,
            cancellationToken);

        // 每次请求都从会话管理器取最新 Cookie，保证失效标记和后续刷新结果能即时生效。
        string cookieHeader = await cookieSessionManager.GetValidCookieHeaderAsync(
            userId,
            cloudAccountUid,
            cancellationToken);

        HttpClient client = CreateClient(clientName);
        using HttpRequestMessage request = CreateRequest(method, endpoint, context);

        // 115 网页端仍依赖浏览器 Cookie 鉴权，不能把 Cookie 放进 query 或日志上下文。
        request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);

        Uri requestUri = ResolveRequestUri(client, request);
        var stopwatch = Stopwatch.StartNew();
        using HttpResponseMessage response = await SendRequestAsync(client, request, requestUri, cancellationToken);
        stopwatch.Stop();

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
            // 部分接口只有 state=false 没有标准错误码，必须同时检查适配器和通用 state 字段。
            LogApiCallSummary(
                request,
                requestUri,
                (int)response.StatusCode,
                apiError?.Code?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                stopwatch.ElapsedMilliseconds);

            await ThrowMappedBusinessExceptionAsync(
                apiError,
                responseJson,
                userId,
                cloudAccountUid,
                context.Category,
                requestUri,
                cancellationToken);
        }

        LogApiCallSummary(request, requestUri, (int)response.StatusCode, null, stopwatch.ElapsedMilliseconds);

        return DeserializeResponse<T>(responseContent, requestUri);
    }

    /// <summary>
    /// 根据请求上下文创建 HTTP 请求消息。
    /// </summary>
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

    /// <summary>
    /// 发送 HTTP 请求并将网络异常映射为 115 网络异常。
    /// </summary>
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

    /// <summary>
    /// 验证 HTTP 状态码并映射 115 业务异常。
    /// </summary>
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
            // 认证失败状态说明 Cookie 已不可继续使用，先持久化失效标记再抛领域异常。
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
            // 上游 HTTP 429 是强频控信号，反馈给限流器以便后续请求主动降速。
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

    /// <summary>
    /// 根据 115 业务错误字段抛出对应领域异常。
    /// </summary>
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
            // 有些接口用业务错误表达登录失效，同样需要落库失效，阻止后续继续复用旧 Cookie。
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
            // 业务响应中的 rate/limit 文案也代表上游频控，需要和 HTTP 429 使用同一反馈路径。
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

        if (apiError is null && !HasKnownErrorShape(responseJson))
        {
            // state=false 但没有任何已知错误字段时保留 unknown_error_shape，方便后续补充接口适配规则。
            throw new CloudDriveApiException(
                $"115 网页端 API 返回未知错误结构 Endpoint={requestUri.AbsolutePath}",
                "unknown_error_shape");
        }

        throw new CloudDriveApiException(
            apiError?.Message ?? $"115 网页端 API 返回业务失败 Endpoint={requestUri.AbsolutePath}");
    }

    /// <summary>
    /// 判断 115 响应中的 state 字段是否表示业务失败。
    /// </summary>
    private static bool IsStateFalse(JsonDocument responseJson)
    {
        if (!responseJson.RootElement.TryGetProperty("state", out JsonElement stateElement))
        {
            return false;
        }

        bool? state = JsonElementReaders.ReadBoolean(stateElement);
        return state == false;
    }

    /// <summary>
    /// 判断 115 业务错误是否表示 Cookie 登录态失效。
    /// </summary>
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

    /// <summary>
    /// 判断 115 业务错误是否表示上游频控。
    /// </summary>
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

    /// <summary>
    /// 读取 115 响应中已知错误字段的字符串值。
    /// </summary>
    private static string? ReadKnownErrorValue(JsonDocument responseJson, string propertyName)
    {
        return responseJson.RootElement.TryGetProperty(propertyName, out JsonElement property)
            ? JsonElementReaders.ReadString(property)
            : null;
    }

    /// <summary>
    /// 判断响应是否包含已知的 115 错误字段结构。
    /// </summary>
    private static bool HasKnownErrorShape(JsonDocument responseJson)
    {
        return responseJson.RootElement.TryGetProperty("errNo", out _)
            || responseJson.RootElement.TryGetProperty("errno", out _)
            || responseJson.RootElement.TryGetProperty("errCode", out _)
            || responseJson.RootElement.TryGetProperty("code", out _)
            || responseJson.RootElement.TryGetProperty("error", out _)
            || responseJson.RootElement.TryGetProperty("message", out _)
            || responseJson.RootElement.TryGetProperty("msg", out _);
    }

    /// <summary>
    /// 将 115 响应正文解析为 JSON 文档。
    /// </summary>
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

    /// <summary>
    /// 将 115 响应正文反序列化为目标类型。
    /// </summary>
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
            // JsonDocument 生命周期会在方法内结束，必须 Clone 根节点才能安全返回 JsonElement。
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

    /// <summary>
    /// 解析请求的最终绝对 URI。
    /// </summary>
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

    /// <summary>
    /// 将查询参数追加到请求端点。
    /// </summary>
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
            // 这里保留传入顺序，云下载签名所需的稳定排序只在表单签名输入中处理。
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

    /// <summary>
    /// 写入已脱敏的 115 API 调用摘要日志。
    /// </summary>
    private void LogApiCallSummary(
        HttpRequestMessage request,
        Uri requestUri,
        int statusCode,
        string? upstreamCode,
        long latencyMs)
    {
        string path = sensitiveFieldsFilter.Redact(requestUri.AbsolutePath);
        string? redactedUpstreamCode = upstreamCode is null ? null : sensitiveFieldsFilter.Redact(upstreamCode);

        // 日志只记录已脱敏摘要，不输出 query、Cookie、表单或响应正文，避免泄露账号凭证。
        if (statusCode >= 500)
        {
            logger.LogError(
                "115 API 调用失败 Method={Method}, Path={Path}, HttpStatus={HttpStatus}, UpstreamCode={UpstreamCode}, LatencyMs={LatencyMs}",
                request.Method.Method,
                path,
                statusCode,
                redactedUpstreamCode,
                latencyMs);
            return;
        }

        if (statusCode >= 400 || upstreamCode is not null)
        {
            logger.LogWarning(
                "115 API 调用异常 Method={Method}, Path={Path}, HttpStatus={HttpStatus}, UpstreamCode={UpstreamCode}, LatencyMs={LatencyMs}",
                request.Method.Method,
                path,
                statusCode,
                redactedUpstreamCode,
                latencyMs);
            return;
        }

        logger.LogInformation(
            "115 API 调用完成 Method={Method}, Path={Path}, HttpStatus={HttpStatus}, UpstreamCode={UpstreamCode}, LatencyMs={LatencyMs}",
            request.Method.Method,
            path,
            statusCode,
            redactedUpstreamCode,
            latencyMs);
    }

    /// <summary>
    /// 复制并标准化表单字段，避免修改调用方传入的集合。
    /// </summary>
    private static IDictionary<string, string> MergeFormFields(IDictionary<string, string>? formFields)
    {
        return formFields is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(formFields, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 复制并标准化查询参数，避免修改调用方传入的集合。
    /// </summary>
    private static IDictionary<string, string> MergeQueryParameters(IDictionary<string, string>? queryParameters)
    {
        return queryParameters is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(queryParameters, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 将表单字段按稳定顺序序列化为签名输入。
    /// </summary>
    private static string SerializeFormFields(IDictionary<string, string> formFields)
    {
        if (formFields.Count == 0)
        {
            return string.Empty;
        }

        List<string> pairs = [];
        foreach (KeyValuePair<string, string> field in formFields.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            // 签名输入使用 Ordinal 稳定排序，避免不同字典实现或区域性导致签名不一致。
            pairs.Add($"{field.Key}={field.Value}");
        }

        return string.Join('&', pairs);
    }

    /// <summary>
    /// 从端点或查询参数中解析云下载 ac 操作名。
    /// </summary>
    private static string ResolveOfflineAction(string endpoint, IDictionary<string, string>? queryParameters)
    {
        if (queryParameters is not null
            && queryParameters.TryGetValue("ac", out string? actionFromQuery)
            && !string.IsNullOrWhiteSpace(actionFromQuery))
        {
            // 调用方显式传入 ac 时优先使用，避免从 endpoint 推断覆盖更精确的操作名。
            return actionFromQuery;
        }

        string normalized = endpoint.Trim();
        int queryStart = normalized.IndexOf('?');

        if (queryStart >= 0)
        {
            // 兼容调用方传入 "xxx?ac=..." 的旧形式，并在后续统一改写到 queryParameters。
            string query = normalized[(queryStart + 1)..];
            foreach (string part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] pair = part.Split('=', 2);
                if (pair.Length == 2 && pair[0].Equals("ac", StringComparison.OrdinalIgnoreCase))
                {
                    return Uri.UnescapeDataString(pair[1]);
                }
            }

            normalized = normalized[..queryStart];
        }

        normalized = normalized.Trim('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new CloudDriveConfigurationException(
                "115 云下载请求缺少 ac 操作符",
                "cloud_drive_offline_action_missing");
        }

        return normalized;
    }

}
