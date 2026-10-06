using Application.Abstractions;
using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Files;

/// <summary>
/// 复制指定 115 账号中的文件或文件夹。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">复制文件输入。</param>
public sealed record CopyFilesCommand(Guid UserId, CopyFilesInput Input)
    : ICommand<CloudDriveOperationResultDto>, Application.Abstractions.CloudDrive.ICloudDriveAccountScopedRequest, IHasInput<CopyFilesInput>;
