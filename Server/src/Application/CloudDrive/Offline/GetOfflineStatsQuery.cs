using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Offline;

/// <summary>
/// 查询 115 云下载任务统计。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">账号上下文输入。</param>
public sealed record GetOfflineStatsQuery(Guid UserId, CloudDriveAccountScopedInput Input)
    : IQuery<CloudDriveOfflineStatsDto>;
