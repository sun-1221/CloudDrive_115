using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Files;

/// <summary>
/// 初始化指定 115 账号的文件上传会话。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">上传初始化输入。</param>
public sealed record InitUploadCommand(Guid UserId, InitUploadInput Input)
    : ICommand<CloudDriveUploadSessionDto>, ICloudDriveAccountScopedRequest;
