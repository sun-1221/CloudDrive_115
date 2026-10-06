using Domain.Entities;

namespace Domain.Repositories;

/// <summary>本地用户仓储。</summary>
public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
}
