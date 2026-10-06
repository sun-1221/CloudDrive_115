using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;

/// <summary>
/// 115 网盘后台任务仓储接口。
/// </summary>
public interface ICloudDriveJobRepository : IRepository<CloudDriveJob, Guid>
{
    /// <summary>
    /// 查询指定用户的后台任务列表。
    /// </summary>
    /// <param name="userId">系统用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>任务只读列表；无数据时返回空列表。</returns>
    Task<IReadOnlyList<CloudDriveJob>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据任务 ID 与用户 ID 查询后台任务。
    /// </summary>
    /// <param name="userId">系统用户 ID。</param>
    /// <param name="id">任务 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>存在且归属于该用户时返回任务；否则返回 <see langword="null"/>。</returns>
    Task<CloudDriveJob?> GetByIdForUserAsync(
        Guid userId,
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 按状态扫描后台任务，供应用启动恢复协调使用。
    /// </summary>
    /// <param name="statuses">要扫描的任务状态集合。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>匹配状态的任务只读列表；无数据时返回空列表。</returns>
    Task<IReadOnlyList<CloudDriveJob>> GetByStatusesAsync(
        IEnumerable<CloudDriveJobStatus> statuses,
        CancellationToken cancellationToken = default);
}
