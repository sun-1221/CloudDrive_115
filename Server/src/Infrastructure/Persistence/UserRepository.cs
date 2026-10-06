using Domain.Entities;
using Domain.Repositories;

using SqlSugar;

namespace Infrastructure.Persistence;

internal sealed class UserRepository(ISqlSugarClient db, Application.Abstractions.IDomainEventSource domainEventSource)
    : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await db.Queryable<User>().InSingleAsync(id);
    }

    public async Task<IReadOnlyList<User>> GetListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await db.Queryable<User>().OrderBy(user => user.CreationTime, OrderByType.Desc).ToListAsync(cancellationToken);
    }

    public async Task<int> InsertAsync(User entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        int rows = await db.Insertable(entity).ExecuteCommandAsync(cancellationToken);
        if (rows > 0)
        {
            domainEventSource.Track(entity);
        }

        return rows;
    }

    public async Task<int> UpdateAsync(User entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        int rows = await db.Updateable(entity).ExecuteCommandAsync(cancellationToken);
        if (rows > 0)
        {
            domainEventSource.Track(entity);
        }

        return rows;
    }

    public async Task<int> DeleteAsync(User entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        int rows = await db.Deleteable(entity).ExecuteCommandAsync(cancellationToken);
        if (rows > 0)
        {
            domainEventSource.Track(entity);
        }

        return rows;
    }

    public async Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        return await db.Queryable<User>().Where(user => user.UserName == userName).FirstAsync(cancellationToken);
    }
}
