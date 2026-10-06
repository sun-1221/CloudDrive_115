using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Auth;

/// <summary>
/// 查询当前用户绑定的 115 账号摘要列表。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
public sealed record GetBoundAccountsQuery(Guid UserId) : IQuery<List<CloudDriveAccountSummaryDto>>;
