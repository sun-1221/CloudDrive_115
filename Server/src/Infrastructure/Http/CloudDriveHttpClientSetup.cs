using Application.CloudDrive;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Http;

/// <summary>
/// Registers Named HttpClient instances for 115 web API domain groups.
/// </summary>
public static class CloudDriveHttpClientSetup
{
    /// <summary>
    /// Registers 115 web API clients grouped by domain.
    /// </summary>
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

        return services;
    }

    private static Uri GetBaseAddress(IServiceProvider provider, Func<CloudDriveAppConfig, string> selector)
    {
        CloudDriveAppConfig config = provider.GetRequiredService<IOptions<CloudDriveAppConfig>>().Value;
        string value = selector(config);
        return new Uri(value, UriKind.Absolute);
    }
}
