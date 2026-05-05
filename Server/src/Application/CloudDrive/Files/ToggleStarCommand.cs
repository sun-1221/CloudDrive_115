using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Files;

/// <summary>
/// 更新指定文件或文件夹的星标状态。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">星标操作输入。</param>
public sealed record ToggleStarCommand(Guid UserId, ToggleStarInput Input)
    : ICommand<CloudDriveOperationResultDto>, ICloudDriveAccountScopedRequest;
