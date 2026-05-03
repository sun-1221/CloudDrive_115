using Microsoft.Extensions.Hosting;

using SqlSugar;

namespace Infrastructure.Persistence;

internal sealed class SqlSugarCodeFirstHostedService(
    ISqlSugarClient db,
    SqlSugarCodeFirstOptions options) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SqlSugarSetup.InitTables(db, options.EntityTypes.ToArray());
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
