using Application.Abstractions;
using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Files;

using Domain.Repositories;

using Infrastructure.DependencyInjection;
using Infrastructure.Jobs;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Infrastructure.Tests.Support;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using SqlSugar;

namespace Infrastructure.Tests.DependencyInjection;

public sealed class CloudDriveInfrastructureRegistrationTests
{
    [Fact]
    public void AddCloudDriveInfrastructure_RegistersCoreServices()
    {
        // Arrange
        ServiceProvider provider = BuildProvider();

        // Act & Assert
        using IServiceScope scope = provider.CreateScope();
        IServiceProvider services = scope.ServiceProvider;

        Assert.NotNull(services.GetRequiredService<ICloudDriveOpenApiGateway>());
        Assert.NotNull(services.GetRequiredService<ICloudDriveSecretProtector>());
        Assert.NotNull(services.GetRequiredService<ICookieInvalidationNotifier>());
        Assert.NotNull(services.GetRequiredService<ICookieSignatureStrategy>());
        Assert.NotNull(services.GetRequiredService<IErrorFieldAdapter>());
        Assert.NotNull(services.GetRequiredService<ICloudDriveMetadataCache>());
        Assert.NotNull(services.GetRequiredService<ICloudDriveRateLimiter>());
        Assert.NotNull(services.GetRequiredService<ICloudDriveJobScheduler>());
        Assert.NotNull(services.GetRequiredService<ICloudDriveJobOrchestrator>());
        Assert.NotNull(services.GetRequiredService<ICookieSessionRepository>());
        Assert.NotNull(services.GetRequiredService<ICloudDriveJobRepository>());
        Assert.NotNull(services.GetRequiredService<CookieSessionManager>());
        Assert.NotNull(services.GetRequiredService<CloudDriveFileManager>());
    }

    [Fact]
    public void AddCloudDriveInfrastructure_BindsCloudDriveConfig()
    {
        // Arrange
        ServiceProvider provider = BuildProvider();

        // Act
        CloudDriveAppConfig config = provider.GetRequiredService<IOptions<CloudDriveAppConfig>>().Value;

        // Assert
        Assert.True(config.CloudDriveFeatureEnabled);
        Assert.Equal("https://webapi.test/", config.WebApiBaseUrl);
        Assert.Equal(9, config.DefaultRateLimit);
    }

    [Fact]
    public void AddCloudDriveInfrastructure_DefaultSignatureStrategyReturnsUnavailableSignature()
    {
        // Arrange
        ServiceProvider provider = BuildProvider();
        ICookieSignatureStrategy strategy = provider.GetRequiredService<ICookieSignatureStrategy>();

        // Act
        CookieSignature signature = strategy.Generate(Guid.NewGuid(), "115001", "page=1");

        // Assert
        Assert.Equal(default, signature);
    }

    private static ServiceProvider BuildProvider()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CloudDrive:CloudDriveFeatureEnabled"] = "true",
                ["CloudDrive:WebApiBaseUrl"] = "https://webapi.test/",
                ["CloudDrive:MyApiBaseUrl"] = "https://my.test/",
                ["CloudDrive:VipApiBaseUrl"] = "https://vip.test/",
                ["CloudDrive:PayApiBaseUrl"] = "https://pay.test/",
                ["CloudDrive:OfflineApiBaseUrl"] = "https://offline.test/",
                ["CloudDrive:DefaultRateLimit"] = "9",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDomainEventSource, FakeDomainEventSource>();
        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
        services.AddDataProtection();
        services.AddSqlSugar(config =>
        {
            config.ConnectionString = "Data Source=:memory:";
            config.DbType = DbType.Sqlite;
            config.IsAutoCloseConnection = false;
        });
        services.AddCloudDriveInfrastructure(configuration);
        services.AddQuartzJobs();

        return services.BuildServiceProvider();
    }
}
