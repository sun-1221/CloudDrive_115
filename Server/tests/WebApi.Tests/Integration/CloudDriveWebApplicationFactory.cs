using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Domain.Enums;
using Domain.Entities;
using Domain.Repositories;

namespace WebApi.Tests.Integration;

/// <summary>
/// 集成测试专用的 <see cref="WebApplicationFactory{TEntryPoint}"/>。
/// 通过 <c>appsettings.Testing.json</c> 提供最小化的测试配置（JWT 密钥、内存 SQLite），确保应用能正常启动。
/// </summary>
public sealed class CloudDriveWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 设置 Testing 环境使 WebApplicationBuilder 自动加载 appsettings.Testing.json
        builder.UseEnvironment("Testing");

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });

        builder.ConfigureServices(services =>
        {
            // 移除 Quartz HostedService，避免内存 SQLite 连接隔离导致启动失败
            ServiceDescriptor[] hostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToArray();

            foreach (ServiceDescriptor descriptor in hostedServices)
            {
                services.Remove(descriptor);
            }

            services.RemoveAll<ICloudDriveWebApiGateway>();
            services.RemoveAll<ICloudDriveJobOrchestrator>();
            services.RemoveAll<ICloudDriveMetadataCache>();
            services.RemoveAll<IUserSessionRepository>();
            services.AddSingleton<IUserSessionRepository, TestUserSessionRepository>();

            CloudDriveSideEffectProbe probe = new();
            services.AddSingleton(probe);
            services.AddSingleton<ICloudDriveWebApiGateway>(probe);
            services.AddSingleton<ICloudDriveJobOrchestrator>(probe);
            services.AddSingleton<ICloudDriveMetadataCache>(probe);
        });
    }
}
internal sealed class TestUserSessionRepository : IUserSessionRepository
{
    public Task<UserSession?> GetActiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<UserSession?>(UserSession.Create(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            TimeSpan.FromHours(1)));
    }

    public Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => GetActiveAsync(id, cancellationToken);

    public Task<IReadOnlyList<UserSession>> GetListAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<UserSession>>([]);

    public Task<int> InsertAsync(UserSession entity, CancellationToken cancellationToken = default)
        => Task.FromResult(1);

    public Task<int> UpdateAsync(UserSession entity, CancellationToken cancellationToken = default)
        => Task.FromResult(1);

    public Task<int> DeleteAsync(UserSession entity, CancellationToken cancellationToken = default)
        => Task.FromResult(1);
}
