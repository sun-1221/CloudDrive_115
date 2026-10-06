using Domain.Entities;

using FsCheck.Xunit;

using Infrastructure.Persistence;
using Infrastructure.Tests.Support;

using SqlSugar;

namespace Infrastructure.Tests.Persistence;

/// <summary>
/// 设计文档 Property 7：Repository CRUD 往返一致性。
/// <para>
/// *For any* 有效聚合根，通过 <c>Repository</c> 执行 Insert 后再通过
/// <c>GetByIdAsync</c> 查询，应返回与原始实体等价的对象。
/// </para>
/// <para>
/// 基于内存 SQLite（按 integration-testing-standards.md:132 允许用于 CRUD 属性测试）。
/// </para>
/// </summary>
public sealed class RepositoryCrudPropertyTests : IDisposable
{
    private readonly SqlSugarClient _db;
    private readonly FakeDomainEventSource _eventSource;
    private readonly Repository<TestPersistedEntity, Guid> _repository;

    public RepositoryCrudPropertyTests()
    {
        var config = new ConnectionConfig
        {
            ConnectionString = "Data Source=:memory:",
            DbType = DbType.Sqlite,

            // in-memory SQLite 在连接关闭后数据会丢失，故需保持连接
            IsAutoCloseConnection = false,
            InitKeyType = InitKeyType.Attribute,
        };
        SqlSugarSetup.ApplyFrameworkConventions(config);
        _db = new SqlSugarClient(config);
        _db.CodeFirst.InitTables<TestPersistedEntity>();

        _eventSource = new FakeDomainEventSource();
        _repository = new Repository<TestPersistedEntity, Guid>(_db, _eventSource);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    /// <summary>
    /// Property 7：Insert 后 GetById 返回与原始实体等价的对象，且仓储会通过
    /// <c>IDomainEventSource.Track</c> 追踪该实体（R5.7）。
    /// </summary>
    [Property(MaxTest = 30)]
    public async Task<bool> InsertThenGetById_ReturnsEquivalentEntity(int nameSeed)
    {
        // Arrange（准备）
        _eventSource.Clear();
        string name = $"name-{nameSeed}";
        var entity = new TestPersistedEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
        };

        // Act（执行）
        int rows = await _repository.InsertAsync(entity, CancellationToken.None);
        TestPersistedEntity? loaded = await _repository.GetByIdAsync(entity.Id, CancellationToken.None);

        // Assert（断言） - 插入 1 行、Get 非空、Id 与 Name 等价、实体被 Track
        return rows == 1
            && loaded is not null
            && loaded.Id == entity.Id
            && loaded.Name == entity.Name
            && _eventSource.Tracked.Count == 1
            && ReferenceEquals(_eventSource.Tracked[0], entity);
    }

    [Fact]
    public async Task UpdateAsync_WhenEntityExists_ModifiesFieldAndTracksEntity()
    {
        // Arrange（准备）
        _eventSource.Clear();
        var entity = new TestPersistedEntity { Id = Guid.NewGuid(), Name = "original" };
        _ = await _repository.InsertAsync(entity, CancellationToken.None);
        _eventSource.Clear();

        entity.Name = "updated";

        // Act（执行）
        int rows = await _repository.UpdateAsync(entity, CancellationToken.None);
        TestPersistedEntity? loaded = await _repository.GetByIdAsync(entity.Id, CancellationToken.None);

        // Assert（断言）
        Assert.Equal(1, rows);
        Assert.NotNull(loaded);
        Assert.Equal("updated", loaded.Name);
        Assert.Single(_eventSource.Tracked);
    }

    [Fact]
    public async Task DeleteAsync_WhenEntityExists_RemovesRowAndTracksEntity()
    {
        // Arrange（准备）
        _eventSource.Clear();
        var entity = new TestPersistedEntity { Id = Guid.NewGuid(), Name = "to-delete" };
        _ = await _repository.InsertAsync(entity, CancellationToken.None);
        _eventSource.Clear();

        // Act（执行）
        int rows = await _repository.DeleteAsync(entity, CancellationToken.None);
        TestPersistedEntity? loaded = await _repository.GetByIdAsync(entity.Id, CancellationToken.None);

        // Assert（断言）
        Assert.Equal(1, rows);
        Assert.Null(loaded);
        Assert.Single(_eventSource.Tracked);
    }

    /// <summary>
    /// 测试用持久化聚合根。使用 <see cref="Guid"/> 主键避免 property 测试间 ID 冲突。
    /// </summary>
    [SugarTable("test_persisted_entities")]
    public sealed class TestPersistedEntity : AggregateRoot<Guid>
    {
        [SugarColumn(IsPrimaryKey = true)]
        public new Guid Id
        {
            get => base.Id;
            set => base.Id = value;
        }

        [SugarColumn(Length = 100)]
        public string Name { get; set; } = string.Empty;
    }
}
