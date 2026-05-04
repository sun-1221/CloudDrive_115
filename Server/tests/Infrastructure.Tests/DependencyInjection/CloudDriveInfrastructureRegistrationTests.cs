using Application.Abstractions;
using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Files;

using Domain.Repositories;

using Infrastructure.DependencyInjection;
using Infrastructure.Http;
using Infrastructure.Persistence;
using Infrastructure.Tests.Support;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Quartz;
using SqlSugar;

namespace Infrastructure.Tests.DependencyInjection;

public sealed class CloudDriveInfrastructureRegistrationTests
{
    [Fact]
    public void AddCloudDriveInfrastructure_RegistersCoreServices()
    {
        // Arrange
        ServiceCollection serviceCollection = CreateCloudDriveServices();
        Assert.Contains(serviceCollection, descriptor => descriptor.ServiceType == typeof(ICloudDriveJobScheduler));
        Assert.Contains(
            serviceCollection,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType?.Name == "CloudDriveJobRecoveryHostedService");

        using ServiceProvider provider = serviceCollection.BuildServiceProvider();

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
        Assert.NotNull(services.GetRequiredService<ICloudDriveJobOrchestrator>());
        Assert.NotNull(services.GetRequiredService<ICookieSessionRepository>());
        Assert.NotNull(services.GetRequiredService<ICloudDriveJobRepository>());
        Assert.NotNull(services.GetRequiredService<CookieSessionManager>());
        Assert.NotNull(services.GetRequiredService<CloudDriveFileManager>());
        Assert.NotNull(services.GetRequiredService<UserAggregationService>());
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

    [Fact]
    public void AddCloudDrive_RegistersModuleEntryPointAndQuartzJobs()
    {
        // Arrange
        IConfiguration configuration = CreateConfiguration();
        var services = new ServiceCollection();
        services.AddLogging();
        ServiceProvider? provider = null;

        try
        {
            // Act
            services.AddCloudDrive(configuration);
            provider = services.BuildServiceProvider();

            // Assert
            using IServiceScope scope = provider.CreateScope();
            IServiceProvider scopedServices = scope.ServiceProvider;
            Assert.NotNull(scopedServices.GetRequiredService<ICloudDriveOpenApiGateway>());
            Assert.NotNull(scopedServices.GetRequiredService<ICloudDriveJobScheduler>());
            Assert.NotNull(scopedServices.GetRequiredService<CookieSessionManager>());

            ISchedulerFactory schedulerFactory = provider.GetRequiredService<ISchedulerFactory>();
            IScheduler scheduler = schedulerFactory.GetScheduler().GetAwaiter().GetResult();
            Assert.True(scheduler.CheckExists(new JobKey("CloudDriveUploadJob", "cloud-drive")).GetAwaiter().GetResult());
            Assert.True(scheduler.CheckExists(new JobKey("CloudDriveBatchOperationJob", "cloud-drive")).GetAwaiter().GetResult());
            Assert.True(scheduler.CheckExists(new JobKey("CloudDriveClearRecycleBinJob", "cloud-drive")).GetAwaiter().GetResult());
            Assert.True(scheduler.CheckExists(new JobKey("CloudDriveTranscodeJob", "cloud-drive")).GetAwaiter().GetResult());
        }
        finally
        {
            Quartz.Logging.LogContext.SetCurrentLogProvider(NullLoggerFactory.Instance);
            provider?.Dispose();
        }
    }

    [Theory]
    [InlineData("CloudDrive:CookieEncryptionKeyName")]
    [InlineData("CloudDrive:WebApiBaseUrl")]
    [InlineData("CloudDrive:SignatureKeyName")]
    public void AddCloudDriveInfrastructure_WhenRequiredConfigMissing_DisablesFeatureAndLogsError(string missingKey)
    {
        // Arrange
        Dictionary<string, string?> values = CreateConfigValues();
        values["CloudDrive:OfflineSignatureEnabled"] = "true";
        values[missingKey] = string.Empty;

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var sink = new RecordingLoggerSink();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(new RecordingLoggerProvider(sink)));
        services.AddSingleton<IDomainEventSource, FakeDomainEventSource>();
        services.AddDataProtection();
        services.AddSqlSugar(config =>
        {
            config.ConnectionString = "Data Source=:memory:";
            config.DbType = DbType.Sqlite;
            config.IsAutoCloseConnection = false;
        });

        // Act
        services.AddCloudDriveInfrastructure(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        CloudDriveAppConfig config = provider.GetRequiredService<IOptions<CloudDriveAppConfig>>().Value;

        // Assert
        Assert.False(config.CloudDriveFeatureEnabled);
        Assert.Contains(sink.Entries, entry =>
            entry.Level == LogLevel.Error
            && entry.Message.Contains("115 网盘模块配置不完整", StringComparison.Ordinal)
            && entry.Message.Contains(missingKey["CloudDrive:".Length..], StringComparison.Ordinal));
    }

    [Fact]
    public void AddCloudDriveInfrastructure_RegistersNamedHttpClientsAndInfrastructureImplementations()
    {
        // Arrange
        using ServiceProvider provider = BuildProvider();
        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();

        // Act & Assert
        Assert.Equal(new Uri("https://webapi.test/"), factory.CreateClient(CloudDriveHttpClientNames.Web).BaseAddress);
        Assert.Equal(new Uri("https://my.test/"), factory.CreateClient(CloudDriveHttpClientNames.My).BaseAddress);
        Assert.Equal(new Uri("https://vip.test/"), factory.CreateClient(CloudDriveHttpClientNames.Vip).BaseAddress);
        Assert.Equal(new Uri("https://pay.test/"), factory.CreateClient(CloudDriveHttpClientNames.Pay).BaseAddress);
        Assert.Equal(new Uri("https://offline.test/"), factory.CreateClient(CloudDriveHttpClientNames.Offline).BaseAddress);

        Assert.IsType<CloudDriveSensitiveFieldsFilter>(
            provider.GetRequiredService<ICloudDriveSensitiveFieldsFilter>());
        Assert.IsType<ErrorFieldAdapter>(
            provider.GetRequiredService<IErrorFieldAdapter>());
    }

    private static ServiceProvider BuildProvider()
    {
        return CreateCloudDriveServices().BuildServiceProvider();
    }

    private static ServiceCollection CreateCloudDriveServices()
    {
        IConfiguration configuration = CreateConfiguration();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCloudDrive(configuration);

        return services;
    }

    private static IConfiguration CreateConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(CreateConfigValues())
        .Build();

    private static Dictionary<string, string?> CreateConfigValues() => new()
    {
        ["CloudDrive:CloudDriveFeatureEnabled"] = "true",
        ["CloudDrive:CookieEncryptionKeyName"] = "cookie-key",
        ["CloudDrive:SignatureKeyName"] = "signature-key",
        ["CloudDrive:WebApiBaseUrl"] = "https://webapi.test/",
        ["CloudDrive:MyApiBaseUrl"] = "https://my.test/",
        ["CloudDrive:VipApiBaseUrl"] = "https://vip.test/",
        ["CloudDrive:PayApiBaseUrl"] = "https://pay.test/",
        ["CloudDrive:OfflineApiBaseUrl"] = "https://offline.test/",
        ["CloudDrive:DefaultRateLimit"] = "9",
        ["ConnectionStrings:Default"] = "Data Source=:memory:",
        ["Database:Type"] = "Sqlite",
    };

    private sealed class RecordingLoggerProvider(RecordingLoggerSink sink) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new RecordingLogger(sink);

        public void Dispose()
        {
        }
    }

    private sealed class RecordingLogger(RecordingLoggerSink sink) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            sink.Entries.Add(new RecordingLogEntry(logLevel, formatter(state, exception)));
        }
    }

    private sealed class RecordingLoggerSink
    {
        public List<RecordingLogEntry> Entries { get; } = [];
    }

    private sealed record RecordingLogEntry(LogLevel Level, string Message);
}
