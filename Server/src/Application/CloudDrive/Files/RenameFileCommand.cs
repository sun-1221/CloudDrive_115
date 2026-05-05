using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Files;

/// <summary>
/// 重命名指定 115 账号中的文件或文件夹。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">重命名文件输入。</param>
public sealed record RenameFileCommand(Guid UserId, RenameFileInput Input)
    : ICommand<CloudDriveFileDetailDto>, ICloudDriveAccountScopedRequest;
