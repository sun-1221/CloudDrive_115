using Domain.Entities;

namespace Domain.Repositories;

/// <summary>本地登录会话仓储。</summary>
public interface IUserSessionRepository : IRepository<UserSession, Guid>
{
    Task<UserSession?> GetActiveAsync(Guid id, CancellationToken cancellationToken = default);
}
