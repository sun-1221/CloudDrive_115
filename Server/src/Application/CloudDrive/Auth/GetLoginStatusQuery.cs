using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Auth;

/// <summary>
/// 探测指定 115 账号 Cookie 登录态。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">账号上下文输入。</param>
public sealed record GetLoginStatusQuery(Guid UserId, CloudDriveAccountScopedInput Input)
    : IQuery<CloudDriveLoginStatusDto>;
