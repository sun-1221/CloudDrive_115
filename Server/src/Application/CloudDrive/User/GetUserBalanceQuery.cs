using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.User;

/// <summary>
/// 查询 115 账户余额。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
public sealed record GetUserBalanceQuery(Guid UserId)
    : IQuery<CloudDriveBalanceDto>, ICloudDriveAccountScopedRequest;
