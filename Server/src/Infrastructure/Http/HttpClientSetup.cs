using Infrastructure.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

using Polly;

namespace Infrastructure.Http;

/// <summary>
/// HttpClient 注册入口。基于 Polly v8 + <c>Microsoft.Extensions.Http.Resilience</c>，
/// 为读 / 写请求提供不同组合的韧性策略。
/// </summary>
/// <remarks>
/// 按设计 §5. IHttpClientFactory 与 Polly 韧性策略实现：<br/>
/// - 读请求：重试 + 超时 + 熔断（R9.3）<br/>
/// - 写请求：仅超时 + 熔断，避免在幂等性无法保证时重复提交（R9.4）<br/>
/// - 均以命名 HttpClient 形式注册（R9.1、R9.2）。<br/>
/// 业务模块应使用 <see cref="HttpRequestBuilders"/> 构造 query string、
/// <c>application/x-www-form-urlencoded</c>、<c>multipart/form-data</c> 与 JSON 请求（R9.5）。
/// </remarks>
public static class HttpClientSetup
{
    /// <summary>
    /// 获取读请求默认重试延迟。
    /// </summary>
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(1);
    /// <summary>
    /// 获取默认 HTTP 请求超时时间。
    /// </summary>
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);
    /// <summary>
    /// 获取熔断器默认采样窗口。
    /// </summary>
    private static readonly TimeSpan DefaultCircuitBreakSampling = TimeSpan.FromSeconds(30);
    /// <summary>
    /// 获取熔断器默认熔断持续时间。
    /// </summary>
    private static readonly TimeSpan DefaultCircuitBreakDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 注册面向读请求（幂等 GET / HEAD）的命名 HttpClient：重试 + 超时 + 熔断。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="name">HttpClient 名称。</param>
    /// <param name="configureClient">可选的 HttpClient 配置委托。</param>
    /// <returns><see cref="IHttpClientBuilder"/>，供调用方继续链式配置。</returns>
    public static IHttpClientBuilder AddReadResilientHttpClient(
        this IServiceCollection services,
        string name,
        Action<HttpClient>? configureClient = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        IHttpClientBuilder builder = configureClient is null
            ? services.AddHttpClient(name)
            : services.AddHttpClient(name, configureClient);

        builder.AddResilienceHandler($"{name}-read", pipeline =>
        {
            // 读请求按幂等处理，允许指数退避重试来吸收 115 偶发 5xx 或网络抖动。
            pipeline.AddRetry(CreateRetryOptions(name));
            pipeline.AddCircuitBreaker(CreateCircuitBreakerOptions());
            pipeline.AddTimeout(DefaultTimeout);
        });

        return builder;
    }

    /// <summary>
    /// 注册面向写请求（POST / PUT / DELETE 等非幂等请求）的命名 HttpClient：仅超时 + 熔断，
    /// 不启用自动重试，避免重复提交。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="name">HttpClient 名称。</param>
    /// <param name="configureClient">可选的 HttpClient 配置委托。</param>
    /// <returns><see cref="IHttpClientBuilder"/>，供调用方继续链式配置。</returns>
    public static IHttpClientBuilder AddWriteResilientHttpClient(
        this IServiceCollection services,
        string name,
        Action<HttpClient>? configureClient = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        IHttpClientBuilder builder = configureClient is null
            ? services.AddHttpClient(name)
            : services.AddHttpClient(name, configureClient);

        builder.AddResilienceHandler($"{name}-write", pipeline =>
        {
            // 写请求不自动重试，避免创建、删除、云下载等非幂等操作被重复提交。
            pipeline.AddCircuitBreaker(CreateCircuitBreakerOptions());
            pipeline.AddTimeout(DefaultTimeout);
        });

        return builder;
    }

    /// <summary>
    /// 创建读请求重试策略选项。
    /// </summary>
    private static HttpRetryStrategyOptions CreateRetryOptions(string clientName)
    {
        var options = new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = DefaultRetryDelay,
            OnRetry = args =>
            {
                CloudDriveMetrics.RecordHttpRetry(clientName);
                return default;
            },
        };

        options.DisableForUnsafeHttpMethods();
        return options;
    }

    /// <summary>
    /// 创建 HTTP 熔断策略选项。
    /// </summary>
    private static HttpCircuitBreakerStrategyOptions CreateCircuitBreakerOptions() => new()
    {
        // 采样窗口内失败率达到 100% 且至少 5 次请求 → 约等于 "连续 5 次失败"
        FailureRatio = 1.0,
        MinimumThroughput = 5,
        SamplingDuration = DefaultCircuitBreakSampling,
        BreakDuration = DefaultCircuitBreakDuration,
    };
}
