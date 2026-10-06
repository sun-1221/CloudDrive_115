using Application.Abstractions;
using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Auth;

/// <summary>
/// 撤销指定 115 账号的 Cookie 会话。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">撤销输入。</param>
public sealed record RevokeCookieSessionCommand(Guid UserId, RevokeCookieSessionInput Input) : ICommand, IHasInput<RevokeCookieSessionInput>;
