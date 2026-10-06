using Application.Abstractions;
using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Files;

/// <summary>
/// 查询指定 115 账号的回收站列表。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">回收站列表查询输入。</param>
public sealed record GetRecycleBinListQuery(Guid UserId, GetRecycleBinListInput Input)
    : IQuery<CloudDriveRecycleListDto>, ICloudDriveAccountScopedRequest, IHasInput<GetRecycleBinListInput>;
