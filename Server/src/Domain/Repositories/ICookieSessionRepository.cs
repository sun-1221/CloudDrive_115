using Domain.Entities;

namespace Domain.Repositories;

/// <summary>
/// Cookie 会话仓储接口。
/// </summary>
public interface ICookieSessionRepository : IRepository<CookieSession, Guid>
{
    /// <summary>
    /// 根据系统用户与 115 账号查询 Cookie 会话。
    /// </summary>
    /// <param name="userId">系统用户 ID。</param>
    /// <param name="cloudAccountUid">115 账号唯一标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>存在时返回 Cookie 会话；否则返回 <see langword="null"/>。</returns>
    Task<CookieSession?> GetByAccountAsync(
        Guid userId,
        string cloudAccountUid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询指定系统用户绑定的全部 Cookie 会话。
    /// </summary>
    /// <param name="userId">系统用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Cookie 会话只读列表；无数据时返回空列表。</returns>
    Task<IReadOnlyList<CookieSession>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询全部 Active 或冷却中的 Cookie 会话。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Active 或 Invalidated 会话只读列表；无数据时返回空列表。</returns>
    Task<IReadOnlyList<CookieSession>> GetAvailableSessionsAsync(
        CancellationToken cancellationToken = default);
}
