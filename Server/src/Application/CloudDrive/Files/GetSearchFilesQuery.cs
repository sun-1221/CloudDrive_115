using Application.Abstractions;
using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Files;

/// <summary>
/// 在指定 115 账号中搜索文件。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">文件搜索输入。</param>
public sealed record GetSearchFilesQuery(Guid UserId, SearchFileInput Input)
    : IQuery<CloudDriveSearchListDto>, IHasInput<SearchFileInput>;
