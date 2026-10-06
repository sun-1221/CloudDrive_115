using Application.Behaviors;
using Application.Commands;

using Infrastructure.Jobs;
using Infrastructure.Persistence;
using Infrastructure.Security;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.DependencyInjection;

/// <summary>
/// 115 网盘业务模块总注册入口。
/// </summary>
public static class CloudDriveServiceCollectionExtensions
{
    /// <summary>
    /// 注册 115 网盘模块的 Application、Infrastructure、Quartz 与机密保护装配。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">应用配置。</param>
    /// <returns>原始服务集合，支持链式调用。</returns>
    public static IServiceCollection AddCloudDrive(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddApplicationServices(
            typeof(ICommand).Assembly,
            [typeof(CloudDriveAccountContextBehavior<,>)]);
        services.AddCloudDriveInfrastructure(configuration);
        services.AddQuartzJobs();

        string? connectionString = configuration.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSqlSugar(
                connectionString,
                configuration["Database:Type"]);
        }

        services.AddSecretsProtection(configuration["DataProtection:KeyStoragePath"]);

        return services;
    }
}
