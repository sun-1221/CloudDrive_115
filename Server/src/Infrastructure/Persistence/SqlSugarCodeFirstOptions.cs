namespace Infrastructure.Persistence;

internal sealed class SqlSugarCodeFirstOptions
{
    public SqlSugarCodeFirstOptions(IReadOnlyCollection<Type> entityTypes)
    {
        EntityTypes = entityTypes.ToArray();
    }

    public IReadOnlyList<Type> EntityTypes { get; }
}
