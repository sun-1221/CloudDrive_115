using Microsoft.Extensions.Hosting;

using SqlSugar;

namespace Infrastructure.Persistence;

/// <summary>
/// 应用启动时执行 SqlSugar CodeFirst 建表的后台服务。
/// </summary>
internal sealed class SqlSugarCodeFirstHostedService(
    ISqlSugarClient db,
    SqlSugarCodeFirstOptions options) : IHostedService
{
    /// <summary>
    /// 启动时初始化配置的实体表。
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // CodeFirst 只负责确保表结构存在，连接和实体集合由 SqlSugarSetup 的 DI 配置统一提供。
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
