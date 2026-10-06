using Application.Abstractions;
using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Files;

/// <summary>
/// 清空指定 115 账号回收站。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">清空回收站输入。</param>
public sealed record ClearRecycleBinCommand(Guid UserId, ClearRecycleBinInput Input)
    : ICommand<CloudDriveOperationResultDto>, IHasInput<ClearRecycleBinInput>;
