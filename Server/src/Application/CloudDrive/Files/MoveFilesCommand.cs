using Application.Abstractions;
using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Files;

/// <summary>
/// 移动指定 115 账号中的文件或文件夹。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">移动文件输入。</param>
public sealed record MoveFilesCommand(Guid UserId, MoveFilesInput Input)
    : ICommand<CloudDriveOperationResultDto>, IHasInput<MoveFilesInput>;
