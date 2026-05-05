using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Files;

/// <summary>
/// 查询指定目录下的文件列表。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">文件列表查询输入。</param>
public sealed record GetFileListQuery(Guid UserId, GetFileListInput Input)
    : IQuery<PagedResultDto<CloudDriveFileDto>>, ICloudDriveAccountScopedRequest;
