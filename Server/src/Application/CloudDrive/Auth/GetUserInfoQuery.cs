using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Auth;

/// <summary>
/// 聚合查询指定 115 账号的用户信息。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">账号上下文输入。</param>
public sealed record GetUserInfoQuery(Guid UserId, CloudDriveAccountScopedInput Input)
    : IQuery<CloudDriveUserInfoDto>;
