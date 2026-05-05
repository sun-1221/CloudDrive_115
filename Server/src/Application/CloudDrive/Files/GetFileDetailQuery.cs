using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Files;

/// <summary>
/// 查询指定文件或文件夹详情。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">文件详情查询输入。</param>
public sealed record GetFileDetailQuery(Guid UserId, GetFileInput Input)
    : IQuery<CloudDriveFileDetailDto>, ICloudDriveAccountScopedRequest;
