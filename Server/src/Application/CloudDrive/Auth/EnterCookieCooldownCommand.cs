using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Auth;

/// <summary>
/// 标记 Cookie 会话进入冷却期。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">冷却输入。</param>
public sealed record EnterCookieCooldownCommand(Guid UserId, EnterCookieCooldownInput Input) : ICommand;
