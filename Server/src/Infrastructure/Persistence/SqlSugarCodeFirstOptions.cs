namespace Infrastructure.Persistence;

/// <summary>
/// SqlSugar CodeFirst 启动任务使用的实体类型集合。
/// </summary>
internal sealed class SqlSugarCodeFirstOptions
{
    /// <summary>
    /// 初始化 CodeFirst 实体类型集合。
    /// </summary>
    public SqlSugarCodeFirstOptions(IReadOnlyCollection<Type> entityTypes)
    {
        // 复制为数组，避免外部集合在后台服务启动前被继续修改。
        EntityTypes = entityTypes.ToArray();
    }

    /// <summary>
    /// 获取需要执行 CodeFirst 建表的实体类型。
    /// </summary>
    public IReadOnlyList<Type> EntityTypes { get; }
}
