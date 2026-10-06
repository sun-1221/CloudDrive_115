using Application.Abstractions;
using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Files;

/// <summary>
/// 刷新指定目录的元数据缓存。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">目录刷新输入。</param>
public sealed record RefreshDirectoryCommand(Guid UserId, RefreshDirectoryInput Input)
    : ICommand, ICloudDriveAccountScopedRequest, IHasInput<RefreshDirectoryInput>;
