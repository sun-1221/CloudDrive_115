using Domain.Entities;
using Domain.Repositories;

using SqlSugar;

namespace Infrastructure.Persistence;

internal sealed class UserSessionRepository(ISqlSugarClient db, Application.Abstractions.IDomainEventSource domainEventSource)
    : IUserSessionRepository
{
    public async Task<UserSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await db.Queryable<UserSession>().InSingleAsync(id);
    }

    public async Task<IReadOnlyList<UserSession>> GetListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await db.Queryable<UserSession>().OrderBy(session => session.CreationTime, OrderByType.Desc).ToListAsync(cancellationToken);
    }

    public async Task<UserSession?> GetActiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        UserSession? session = await GetByIdAsync(id, cancellationToken);
        return session is not null && session.IsActive(DateTime.UtcNow) ? session : null;
    }

    public async Task<int> InsertAsync(UserSession entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        int rows = await db.Insertable(entity).ExecuteCommandAsync(cancellationToken);
        if (rows > 0)
        {
            domainEventSource.Track(entity);
        }

        return rows;
    }

    public async Task<int> UpdateAsync(UserSession entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        int rows = await db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
        if (rows > 0)
        {
            domainEventSource.Track(entity);
        }

        return rows;
    }

    public async Task<int> DeleteAsync(UserSession entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        int rows = await db.Deleteable(entity).ExecuteCommandAsync(cancellationToken);
        if (rows > 0)
        {
            domainEventSource.Track(entity);
        }

        return rows;
    }
}
