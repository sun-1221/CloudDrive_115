using Application.Abstractions.CloudDrive;
using Application.CloudDrive;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Http;

/// <summary>
/// 注册按 115 域名分组的命名 HTTP 客户端。
/// </summary>
public static class CloudDriveHttpClientSetup
{
    /// <summary>
    /// 注册 115 网页端 API 访问所需的命名 HTTP 客户端。
    /// </summary>
    /// <param name="services">用于注册命名 HTTP 客户端的服务集合。</param>
    /// <returns>返回同一个 <see cref="IServiceCollection"/> 实例，便于继续链式注册。</returns>
    /// <exception cref="ArgumentNullException">
    /// 当 <paramref name="services"/> 为 <see langword="null"/> 时抛出。
    /// </exception>
    public static IServiceCollection AddCloudDriveHttpClients(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddReadResilientHttpClient(CloudDriveHttpClientNames.Web)
            .ConfigureHttpClient((provider, client) =>
            {
                client.BaseAddress = GetBaseAddress(provider, config => config.WebApiBaseUrl);
            });

        services.AddReadResilientHttpClient(CloudDriveHttpClientNames.My)
            .ConfigureHttpClient((provider, client) =>
            {
                client.BaseAddress = GetBaseAddress(provider, config => config.MyApiBaseUrl);
            });

        services.AddReadResilientHttpClient(CloudDriveHttpClientNames.Vip)
            .ConfigureHttpClient((provider, client) =>
            {
                client.BaseAddress = GetBaseAddress(provider, config => config.VipApiBaseUrl);
            });

        services.AddReadResilientHttpClient(CloudDriveHttpClientNames.Pay)
            .ConfigureHttpClient((provider, client) =>
            {
                client.BaseAddress = GetBaseAddress(provider, config => config.PayApiBaseUrl);
            });

        services.AddWriteResilientHttpClient(CloudDriveHttpClientNames.Offline)
            .ConfigureHttpClient((provider, client) =>
            {
                client.BaseAddress = GetBaseAddress(provider, config => config.OfflineApiBaseUrl);
            });

        services.AddWriteResilientHttpClient(CloudDriveHttpClientNames.Upload)
            .ConfigureHttpClient(client =>
            {
                // 上传初始化固定走 uplb.115.com，不跟随 WebApiBaseUrl，避免环境配置误导上传入口。
                client.BaseAddress = new Uri("https://uplb.115.com/");
            });

        // OSS 直传 URL 由上传初始化返回绝对地址，因此命名客户端只提供策略，不配置 BaseAddress。
        services.AddWriteResilientHttpClient(CloudDriveHttpClientNames.UploadDirect);
        services.AddSingleton<ICloudDriveSensitiveFieldsFilter, CloudDriveSensitiveFieldsFilter>();
        services.AddSingleton<IErrorFieldAdapter, ErrorFieldAdapter>();

        return services;
    }

    /// <summary>
    /// 从配置中获取命名 HTTP 客户端的基础地址。
    /// </summary>
    private static Uri GetBaseAddress(IServiceProvider provider, Func<CloudDriveAppConfig, string> selector)
    {
        CloudDriveAppConfig config = provider.GetRequiredService<IOptions<CloudDriveAppConfig>>().Value;
        string value = selector(config);
        return new Uri(value, UriKind.Absolute);
    }
}
