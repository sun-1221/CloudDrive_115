using System.Reflection;

using Domain.Entities;

using Microsoft.Extensions.DependencyInjection;

using SqlSugar;

namespace Infrastructure.Persistence;

/// <summary>
/// SqlSugar 基础设施配置入口。负责注册 <see cref="ISqlSugarClient"/> 及 CodeFirst 自动建表调用。
/// </summary>
/// <remarks>
/// 注册时强制以下框架约定：<br/>
/// - <see cref="ConnectionConfig.IsAutoCloseConnection"/> = <see langword="true"/>（R5.2）<br/>
/// - <see cref="ConnectionConfig.InitKeyType"/> = <see cref="InitKeyType.Attribute"/>（R5.3）<br/>
/// - 生命周期 <see cref="ServiceLifetime.Scoped"/>（R5.1）。<br/>
/// 调用方通过 <paramref name="configureConnection"/> 提供连接字符串、数据库类型等部署相关配置。
/// </remarks>
public static class SqlSugarSetup
{
    /// <summary>
    /// 注册 <see cref="ISqlSugarClient"/> 为 Scoped 生命周期。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configureConnection">调用方用于设置连接字符串、<see cref="DbType"/> 等。</param>
    /// <returns>原始服务集合，支持链式调用。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="configureConnection"/> 为 <see langword="null"/> 时抛出。</exception>
    public static IServiceCollection AddSqlSugar(
        this IServiceCollection services,
        Action<ConnectionConfig> configureConnection)
    {
        ArgumentNullException.ThrowIfNull(configureConnection);

        services.AddScoped<ISqlSugarClient>(_ =>
        {
            var config = new ConnectionConfig
            {
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute,
            };

            // 先允许调用方设置连接信息，再叠加框架约定，避免调用方漏掉 DomainEvents 忽略规则。
            configureConnection(config);
            ApplyFrameworkConventions(config);
            return new SqlSugarClient(config);
        });

        return services;
    }

    /// <summary>
    /// 使用连接字符串和数据库类型名称注册 <see cref="ISqlSugarClient"/> 为 Scoped 生命周期。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="connectionString">数据库连接字符串。</param>
    /// <param name="dbTypeName">数据库类型名称；为空或无法识别时使用 <see cref="DbType.Sqlite"/>。</param>
    /// <returns>原始服务集合，支持链式调用。</returns>
    /// <exception cref="ArgumentException">当 <paramref name="connectionString"/> 为空白时抛出。</exception>
    public static IServiceCollection AddSqlSugar(
        this IServiceCollection services,
        string connectionString,
        string? dbTypeName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return services.AddSqlSugar(config =>
        {
            config.ConnectionString = connectionString;
            config.DbType = ParseDbType(dbTypeName);
        });
    }

    /// <summary>
    /// 应用启动时调用，为给定的实体类型执行 CodeFirst 自动建表（R5.4）。
    /// </summary>
    /// <param name="db">已配置的 <see cref="ISqlSugarClient"/>。</param>
    /// <param name="entityTypes">需要自动建表的聚合根 / 实体类型。</param>
    /// <exception cref="ArgumentNullException">参数为 <see langword="null"/> 时抛出。</exception>
    public static void InitTables(ISqlSugarClient db, params Type[] entityTypes)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(entityTypes);

        if (entityTypes.Length == 0)
        {
            return;
        }

        db.CodeFirst.InitTables(entityTypes);
    }

    /// <summary>
    /// 注册应用启动时执行的 SqlSugar CodeFirst 建表任务。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="entityTypes">需要自动建表的聚合根 / 实体类型。</param>
    /// <returns>原始服务集合，支持链式调用。</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="entityTypes"/> 为 <see langword="null"/> 时抛出。</exception>
    public static IServiceCollection AddSqlSugarCodeFirst(
        this IServiceCollection services,
        params Type[] entityTypes)
    {
        ArgumentNullException.ThrowIfNull(entityTypes);

        services.AddSingleton(new SqlSugarCodeFirstOptions(entityTypes));
        services.AddHostedService<SqlSugarCodeFirstHostedService>();

        return services;
    }

    /// <summary>
    /// 为给定的 <see cref="ConnectionConfig"/> 叠加框架级 SqlSugar 约定。
    /// 当前规则：忽略 <see cref="IEntity.DomainEvents"/> 属性，使其不作为表字段持久化。
    /// </summary>
    /// <param name="config">要增强的 SqlSugar 连接配置。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="config"/> 为 <see langword="null"/> 时抛出。</exception>
    public static void ApplyFrameworkConventions(ConnectionConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        ConfigureExternalServices? existing = config.ConfigureExternalServices;
        Action<PropertyInfo, EntityColumnInfo>? previousEntityService = existing?.EntityService;
        Action<Type, EntityInfo>? previousEntityNameService = existing?.EntityNameService;

        config.ConfigureExternalServices = new ConfigureExternalServices
        {
            EntityService = (property, column) =>
            {
                // 保留调用方已有映射回调，避免框架约定覆盖业务模块自定义列配置。
                previousEntityService?.Invoke(property, column);

                if (string.Equals(property.Name, nameof(IEntity.DomainEvents), StringComparison.Ordinal))
                {
                    // 领域事件是运行时状态，不属于持久化模型，必须从 CodeFirst 列映射中排除。
                    column.IsIgnore = true;
                }

                CloudDriveSqlSugarMapping.ApplyColumn(property, column);
            },
            EntityNameService = (entityType, entity) =>
            {
                // 表名回调同样先执行外部配置，再补充 115 云盘聚合的默认命名。
                previousEntityNameService?.Invoke(entityType, entity);
                CloudDriveSqlSugarMapping.ApplyEntityName(entityType, entity);
            },
        };
    }

    private static DbType ParseDbType(string? dbTypeName)
    {
        if (string.IsNullOrWhiteSpace(dbTypeName))
        {
            // 本项目本地测试默认使用 SQLite，未配置数据库类型时保持最小可运行环境。
            return DbType.Sqlite;
        }

        return Enum.TryParse(dbTypeName, ignoreCase: true, out DbType dbType)
            ? dbType
            : DbType.Sqlite;
    }
}
