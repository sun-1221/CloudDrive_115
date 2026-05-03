namespace Application.CloudDrive;

/// <summary>
/// 115 网盘集成配置。通过 <c>IOptions&lt;CloudDriveAppConfig&gt;</c> + <c>IConfiguration</c> 注入。
/// Cookie 方案下无 AppId/AppSecret/CallbackUrl 等 OAuth 字段。
/// </summary>
public sealed class CloudDriveAppConfig
{
    /// <summary>
    /// 配置节名称。
    /// </summary>
    public const string SectionName = "CloudDrive";

    /// <summary>
    /// Cookie 负载加密的 key ring 名称。
    /// </summary>
    public string CookieEncryptionKeyName { get; set; } = "CloudDrive:CookiePayload";

    /// <summary>
    /// 云下载签名密钥在 <c>ICloudDriveSecretProtector</c> 中的 key ring 名称。
    /// 可为空表示未启用云下载。
    /// </summary>
    public string? SignatureKeyName { get; set; }

    /// <summary>
    /// Cookie 失效后的冷却期（秒），默认 60。
    /// </summary>
    public int CookieCooldownSeconds { get; set; } = 60;

    /// <summary>
    /// 网页端 API 基础域名。
    /// </summary>
    public string WebApiBaseUrl { get; set; } = "https://webapi.115.com/";

    /// <summary>
    /// 用户中心 API 基础域名。
    /// </summary>
    public string MyApiBaseUrl { get; set; } = "https://my.115.com/";

    /// <summary>
    /// VIP 中心 API 基础域名。
    /// </summary>
    public string VipApiBaseUrl { get; set; } = "https://vip.115.com/";

    /// <summary>
    /// 支付中心 API 基础域名。
    /// </summary>
    public string PayApiBaseUrl { get; set; } = "https://pay.115.com/";

    /// <summary>
    /// 云下载 API 基础域名。
    /// </summary>
    public string OfflineApiBaseUrl { get; set; } = "https://115.com/web/lixian/";

    /// <summary>
    /// 默认每秒最大请求数。
    /// </summary>
    public int DefaultRateLimit { get; set; } = 5;

    /// <summary>
    /// 按场景覆盖的 QPS 配置。
    /// </summary>
    public Dictionary<string, int>? RateLimitOverrides { get; set; }

    /// <summary>
    /// 超限后的处理方式。
    /// </summary>
    public RateLimitOverflowStrategy RateLimitOverflowStrategy { get; set; } = RateLimitOverflowStrategy.Queue;

    /// <summary>
    /// 限流排队的最大队列长度。
    /// </summary>
    public int RateLimitMaxQueueLength { get; set; } = 50;

    /// <summary>
    /// 官方返回限流信号后的最大退避重试次数。
    /// </summary>
    public int ThrottleRetryMaxAttempts { get; set; } = 3;

    /// <summary>
    /// 限流退避基础延迟（毫秒）。
    /// </summary>
    public int ThrottleRetryBaseDelayMilliseconds { get; set; } = 1000;

    /// <summary>
    /// 限流退避最大延迟（毫秒）。
    /// </summary>
    public int ThrottleRetryMaxDelayMilliseconds { get; set; } = 10000;

    /// <summary>
    /// 元数据缓存 TTL（秒）。
    /// </summary>
    public int CacheTtlSeconds { get; set; } = 300;

    /// <summary>
    /// 超过该阈值的上传默认走后台任务（字节）。
    /// </summary>
    public long LargeFileAsyncThresholdBytes { get; set; } = 100 * 1024 * 1024;

    /// <summary>
    /// Office 文档第三方预览服务地址（可选）。
    /// </summary>
    public string? OfficePreviewServiceUrl { get; set; }

    /// <summary>
    /// 115 集成功能是否启用。启动期校验通过后设为 true，配置缺失时为 false。
    /// </summary>
    public bool CloudDriveFeatureEnabled { get; set; }

    /// <summary>
    /// 当云下载签名功能启用时，启动期配置校验要求提供 <see cref="SignatureKeyName"/>。
    /// </summary>
    public bool OfflineSignatureEnabled { get; set; }

    /// <summary>
    /// 应用启动期配置完整性校验。核心配置缺失时禁用 115 集成功能，但不阻止应用启动。
    /// </summary>
    public void ApplyStartupValidation()
    {
        if (string.IsNullOrWhiteSpace(CookieEncryptionKeyName)
            || string.IsNullOrWhiteSpace(WebApiBaseUrl)
            || (OfflineSignatureEnabled && string.IsNullOrWhiteSpace(SignatureKeyName)))
        {
            CloudDriveFeatureEnabled = false;
        }
    }
}

/// <summary>
/// 限流超限处理策略。
/// </summary>
public enum RateLimitOverflowStrategy
{
    /// <summary>
    /// 排队等待。
    /// </summary>
    Queue,

    /// <summary>
    /// 立即拒绝。
    /// </summary>
    Reject
}
