using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs;

/// <summary>
/// 应用启动时协调 115 网盘未完成后台任务。
/// </summary>
internal sealed class CloudDriveJobRecoveryHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<CloudDriveJobRecoveryHostedService> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        CloudDriveJobRecoveryCoordinator coordinator =
            scope.ServiceProvider.GetRequiredService<CloudDriveJobRecoveryCoordinator>();
        int jobCount = await coordinator.RecoverAsync(cancellationToken);

        logger.LogInformation("115 后台任务启动恢复协调完成 Count={Count}", jobCount);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
