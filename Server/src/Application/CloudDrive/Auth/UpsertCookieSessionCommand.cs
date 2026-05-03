using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Auth;

/// <summary>
/// 注入或更新指定 115 账号的 Cookie 会话。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">Cookie 会话输入。</param>
public sealed record UpsertCookieSessionCommand(Guid UserId, UpsertCookieSessionInput Input)
    : ICommand<CloudDriveCookieSessionDto>;
