using Application.Abstractions;
using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Files;

/// <summary>
/// 从指定 115 账号回收站还原条目。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">回收站操作输入。</param>
public sealed record RestoreFromRecycleBinCommand(Guid UserId, RecycleBinOperationInput Input)
    : ICommand<CloudDriveOperationResultDto>, IHasInput<RecycleBinOperationInput>;
