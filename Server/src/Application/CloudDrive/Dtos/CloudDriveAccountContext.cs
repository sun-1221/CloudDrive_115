namespace Application.CloudDrive.Dtos;

/// <summary>
/// 当前请求解析出的 115 账号上下文。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="CloudAccountUid">115 账号 UID。</param>
/// <param name="CookieUid">Cookie UID。</param>
public sealed record CloudDriveAccountContext(Guid UserId, string CloudAccountUid, string CookieUid);
