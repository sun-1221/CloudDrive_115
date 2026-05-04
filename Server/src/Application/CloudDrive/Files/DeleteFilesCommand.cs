using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Files;

/// <summary>
/// 删除指定 115 账号中的文件或文件夹。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">删除文件输入。</param>
public sealed record DeleteFilesCommand(Guid UserId, DeleteFilesInput Input)
    : ICommand<CloudDriveOperationResultDto>;
