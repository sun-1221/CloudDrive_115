using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using SqlSugar;

namespace Infrastructure.Persistence;

/// <summary>
/// 应用启动时执行 SqlSugar CodeFirst 建表的后台服务。
/// </summary>
internal sealed class SqlSugarCodeFirstHostedService(
    IServiceScopeFactory scopeFactory,
    SqlSugarCodeFirstOptions options) : IHostedService
{
    /// <summary>
    /// 启动时初始化配置的实体表。
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // HostedService 是 Singleton，不能直接持有 Scoped 的 ISqlSugarClient；
        // 启动建表时创建短生命周期作用域，避免捕获请求级数据库客户端。
        using IServiceScope scope = scopeFactory.CreateScope();
        ISqlSugarClient db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        SqlSugarSetup.InitTables(db, options.EntityTypes.ToArray());
        return Task.CompletedTask;
    }

    /// <summary>
    /// 停止服务时不需要额外清理。
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
