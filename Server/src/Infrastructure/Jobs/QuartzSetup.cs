using Application.Abstractions.CloudDrive;

using Microsoft.Extensions.DependencyInjection;

using Quartz;

namespace Infrastructure.Jobs;

/// <summary>
/// Quartz.NET 基础设施配置入口。
/// </summary>
/// <remarks>
/// <para>
/// 按设计 §2. Quartz.NET 调度实现：<br/>
/// - <c>AddQuartz()</c> + <c>AddQuartzHostedService()</c> 注册核心服务与宿主（R6.1）<br/>
/// - Quartz 4 已将 DI/Hosting 集成并入核心包，使用 Microsoft DI JobFactory，
///   Job 通过构造函数注入依赖，禁用服务定位器模式（R6.2）<br/>
/// - 首期使用默认内存 JobStore（R6.4）。生产环境可切换 AdoJobStore 等持久化方案<br/>
/// - <c>WaitForJobsToComplete = true</c> 确保优雅关闭。
/// </para>
/// <para>
/// 框架层约束（由业务 Job 自行遵守，无法在注册时强制）：<br/>
/// - <see cref="JobDataMap"/> 仅承载最小调度标识（如 <c>JobId</c>），业务载荷与恢复检查点
///   由 Job 通过仓储或缓存等持久化存储自行加载（R6.3、R6.5）<br/>
/// - Quartz Job 不经过 MediatR Pipeline；若需事务、领域事件分发或恢复检查点处理，
///   必须在 Job 内显式调用 <see cref="Application.Abstractions.ITransactionManager"/>
///   与 <see cref="Application.Abstractions.IDomainEventSource"/>（R6.6）。
/// </para>
/// </remarks>
public static class QuartzSetup
{
    /// <summary>
    /// 注册 Quartz.NET 核心服务与宿主服务。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configure">可选的 Quartz 配置扩展，供业务模块注册 Job、Trigger 等。</param>
    /// <returns>原始服务集合，支持链式调用。</returns>
    public static IServiceCollection AddQuartzJobs(
        this IServiceCollection services,
        Action<IQuartzBuilder>? configure = null)
    {
        services.AddScoped<ICloudDriveJobScheduler, QuartzCloudDriveJobScheduler>();
        services.AddScoped<CloudDriveJobRecoveryCoordinator>();
        services.AddHostedService<CloudDriveJobRecoveryHostedService>();

        services.AddQuartz(q =>
        {
            q.AddJob<CloudDriveUploadJob>(options => options
                .WithIdentity("CloudDriveUploadJob", "cloud-drive")
                .StoreDurably());
            q.AddJob<CloudDriveBatchOperationJob>(options => options
                .WithIdentity("CloudDriveBatchOperationJob", "cloud-drive")
                .StoreDurably());
            q.AddJob<CloudDriveClearRecycleBinJob>(options => options
                .WithIdentity("CloudDriveClearRecycleBinJob", "cloud-drive")
                .StoreDurably());
            q.AddJob<CloudDriveTranscodeJob>(options => options
                .WithIdentity("CloudDriveTranscodeJob", "cloud-drive")
                .StoreDurably());

            // 业务模块通过 configure 回调注册 Job / Trigger
            configure?.Invoke(q);
        });

        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        return services;
    }
}
