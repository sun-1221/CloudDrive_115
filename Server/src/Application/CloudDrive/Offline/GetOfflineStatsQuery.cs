using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Offline;

/// <summary>
/// 查询 115 云下载任务统计。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
public sealed record GetOfflineStatsQuery(Guid UserId)
    : IQuery<CloudDriveOfflineStatsDto>, ICloudDriveAccountScopedRequest;
