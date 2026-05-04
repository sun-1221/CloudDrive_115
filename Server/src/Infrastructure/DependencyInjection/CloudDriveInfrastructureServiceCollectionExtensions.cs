using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Files;
using Application.CloudDrive.Jobs;

using Domain.Entities;

using Infrastructure.Caching;
using Infrastructure.CloudDrive;
using Infrastructure.Http;
using Infrastructure.Jobs;
using Infrastructure.Persistence;
using Infrastructure.Security;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.DependencyInjection;

/// <summary>
/// 115 网盘基础设施模块注册入口。
/// </summary>
public static class CloudDriveInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCloudDriveInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<CloudDriveAppConfig>(configuration.GetSection(CloudDriveAppConfig.SectionName));
        services.AddSingleton<IPostConfigureOptions<CloudDriveAppConfig>, CloudDriveAppConfigPostConfigureOptions>();

        services.AddCaching();
        services.AddCloudDriveHttpClients();
        services.AddSqlSugarCodeFirst(typeof(CookieSession), typeof(CloudDriveJob));

        services.AddScoped<ICloudDriveOpenApiGateway, CloudDriveApiClient>();
        services.AddScoped<ICloudDriveAccountContextResolver, CloudDriveAccountContextResolver>();
        services.AddScoped<CookieSessionManager>();
        services.AddScoped<CloudDriveFileManager>();
        services.AddScoped<ICloudDriveJobOrchestrator, CloudDriveJobOrchestrator>();
        services.AddScoped<ICloudDriveJobAuditLogger, CloudDriveJobAuditLogger>();

        services.AddSingleton<ICloudDriveSecretProtector, DataProtectionCloudDriveSecretProtector>();
        services.AddSingleton<ICookieInvalidationNotifier, LoggingCookieInvalidationNotifier>();
        services.AddSingleton<ICookieSignatureStrategy, NotImplementedCookieSignatureStrategy>();
        services.AddSingleton<ICloudDriveMetadataCache, CloudDriveMetadataCache>();
        services.AddSingleton<ICloudDriveRateLimiter, CloudDriveRateLimiter>();

        services.AddRepositories();

        return services;
    }
}
