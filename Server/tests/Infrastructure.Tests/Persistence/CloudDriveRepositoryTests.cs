using Domain.Entities;
using Domain.Enums;

using Infrastructure.Persistence;
using Infrastructure.Tests.Support;

using SqlSugar;

namespace Infrastructure.Tests.Persistence;

public sealed class CloudDriveRepositoryTests : IDisposable
{
    private readonly SqlSugarClient _db;
    private readonly FakeDomainEventSource _eventSource = new();

    public CloudDriveRepositoryTests()
    {
        var config = new ConnectionConfig
        {
            ConnectionString = "Data Source=:memory:",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = false,
            InitKeyType = InitKeyType.Attribute,
        };
        SqlSugarSetup.ApplyFrameworkConventions(config);
        _db = new SqlSugarClient(config);
        SqlSugarSetup.InitTables(_db, typeof(CookieSession), typeof(CloudDriveJob));
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task CookieSessionRepository_InsertAndGetByAccountAsync_PersistsAndTracks()
    {
        // 准备
        var repository = new CookieSessionRepository(_db, _eventSource);
        Guid userId = Guid.NewGuid();
        CookieSession session = CookieSession.Create(userId, "115001", "encrypted-payload", 1);

        // 执行
        int rows = await repository.InsertAsync(session, CancellationToken.None);
        CookieSession? loaded = await repository.GetByAccountAsync(userId, "115001", CancellationToken.None);
        IReadOnlyList<CookieSession> userSessions = await repository.GetByUserAsync(userId, CancellationToken.None);

        // 断言
        Assert.Equal(1, rows);
        Assert.NotNull(loaded);
        Assert.Equal(session.Id, loaded.Id);
        Assert.Single(userSessions);
        Assert.Single(_eventSource.Tracked);
        Assert.Same(session, _eventSource.Tracked[0]);
    }

    [Fact]
    public async Task CookieSessionRepository_UpdateAsync_TracksChangedEntity()
    {
        // 准备
        var repository = new CookieSessionRepository(_db, _eventSource);
        CookieSession session = CookieSession.Create(Guid.NewGuid(), "115001", "old", 1);
        _ = await repository.InsertAsync(session, CancellationToken.None);
        _eventSource.Clear();

        session.UpdatePayload("new", 2);

        // 执行
        int rows = await repository.UpdateAsync(session, CancellationToken.None);
        CookieSession? loaded = await repository.GetByIdAsync(session.Id, CancellationToken.None);

        // 断言
        Assert.Equal(1, rows);
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.KeyVersion);
        Assert.Single(_eventSource.Tracked);
    }

    [Fact]
    public async Task CloudDriveJobRepository_QueryByUserAndStatuses_ReturnsMatchingJobs()
    {
        // 准备
        var repository = new CloudDriveJobRepository(_db, _eventSource);
        Guid userId = Guid.NewGuid();
        Guid otherUserId = Guid.NewGuid();

        CloudDriveJob pending = CloudDriveJob.Create(userId, "115001", CloudDriveJobType.Upload, payloadJson: "{}");
        CloudDriveJob running = CloudDriveJob.Create(userId, "115001", CloudDriveJobType.Transcode);
        running.Start();
        CloudDriveJob other = CloudDriveJob.Create(otherUserId, "115001", CloudDriveJobType.Upload);

        _ = await repository.InsertAsync(pending, CancellationToken.None);
        _ = await repository.InsertAsync(running, CancellationToken.None);
        _ = await repository.InsertAsync(other, CancellationToken.None);
        _eventSource.Clear();

        // 执行
        IReadOnlyList<CloudDriveJob> userJobs = await repository.GetByUserAsync(userId, CancellationToken.None);
        CloudDriveJob? scopedJob = await repository.GetByIdForUserAsync(userId, pending.Id, CancellationToken.None);
        IReadOnlyList<CloudDriveJob> resumableJobs = await repository.GetByStatusesAsync(
            [CloudDriveJobStatus.Pending, CloudDriveJobStatus.Running],
            CancellationToken.None);

        // 断言
        Assert.Equal(2, userJobs.Count);
        Assert.NotNull(scopedJob);
        Assert.Equal(pending.Id, scopedJob.Id);
        Assert.Contains(resumableJobs, job => job.Id == pending.Id);
        Assert.Contains(resumableJobs, job => job.Id == running.Id);
        Assert.Contains(resumableJobs, job => job.Id == other.Id);
    }

    [Fact]
    public async Task CloudDriveJobRepository_UpdateAndDeleteAsync_TracksEntity()
    {
        // 准备
        var repository = new CloudDriveJobRepository(_db, _eventSource);
        CloudDriveJob job = CloudDriveJob.Create(Guid.NewGuid(), "115001", CloudDriveJobType.Upload);
        _ = await repository.InsertAsync(job, CancellationToken.None);
        _eventSource.Clear();

        job.Start();

        // 执行
        int updateRows = await repository.UpdateAsync(job, CancellationToken.None);
        int deleteRows = await repository.DeleteAsync(job, CancellationToken.None);
        CloudDriveJob? loaded = await repository.GetByIdAsync(job.Id, CancellationToken.None);

        // 断言
        Assert.Equal(1, updateRows);
        Assert.Equal(1, deleteRows);
        Assert.Null(loaded);
        Assert.Equal(2, _eventSource.Tracked.Count);
    }
}
