using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Files;

/// <summary>
/// 在指定 115 账号目录下创建文件夹。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">创建文件夹输入。</param>
public sealed record CreateFolderCommand(Guid UserId, CreateFolderInput Input)
    : ICommand<CloudDriveFolderDto>, ICloudDriveAccountScopedRequest;
