using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Files;

/// <summary>
/// 查询指定文件的临时下载链接。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">下载链接查询输入。</param>
public sealed record GetDownloadUrlQuery(Guid UserId, GetDownloadUrlInput Input)
    : IQuery<CloudDriveDownloadDto>;
