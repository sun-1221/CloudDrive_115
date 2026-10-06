using Domain.Entities;
using Domain.Repositories;

namespace Application.Tests.Support;

/// <summary>
/// 测试用 Cookie 会话仓储，基于内存字典。
/// </summary>
internal sealed class FakeCookieSessionRepository : ICookieSessionRepository
{
    private readonly Dictionary<Guid, CookieSession> _store = [];

    public Task<CookieSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out CookieSession? session);
        return Task.FromResult(session);
    }

    public Task<IReadOnlyList<CookieSession>> GetListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CookieSession>>(_store.Values.ToList().AsReadOnly());

    public Task<int> InsertAsync(CookieSession entity, CancellationToken cancellationToken = default)
    {
        _store[entity.Id] = entity;
        return Task.FromResult(1);
    }

    public Task<int> UpdateAsync(CookieSession entity, CancellationToken cancellationToken = default)
    {
        _store[entity.Id] = entity;
        return Task.FromResult(1);
    }

    public Task<int> DeleteAsync(CookieSession entity, CancellationToken cancellationToken = default)
    {
        _store.Remove(entity.Id);
        return Task.FromResult(1);
    }

    public Task<CookieSession?> GetByAccountAsync(Guid userId, string cloudAccountUid, CancellationToken cancellationToken = default)
    {
        CookieSession? found = _store.Values.FirstOrDefault(
            s => s.UserId == userId && s.CloudAccountUid == cloudAccountUid);
        return Task.FromResult(found);
    }

    public Task<IReadOnlyList<CookieSession>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CookieSession> result = _store.Values
            .Where(s => s.UserId == userId)
            .ToList().AsReadOnly();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<CookieSession>> GetAvailableSessionsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CookieSession> result = _store.Values
            .Where(s => s.Status is Domain.Enums.CookieSessionStatus.Active
                or Domain.Enums.CookieSessionStatus.Invalidated)
            .ToList()
            .AsReadOnly();

        return Task.FromResult(result);
    }
}
