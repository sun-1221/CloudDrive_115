using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Auth;

/// <summary>
/// 探测当前唯一 115 Cookie 登录态。
/// </summary>
/// <param name="UserId">系统用户 ID；匿名探测时为 <see langword="null"/>。</param>
/// <param name="SessionId">当前 JWT 会话 ID；用于读取会话选择的 115 账号。</param>
public sealed record GetLoginStatusQuery(Guid? UserId, Guid? SessionId = null)
    : IQuery<CloudDriveAuthCheckDto>;
