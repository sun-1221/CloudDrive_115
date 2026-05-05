using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Jobs;

/// <summary>
/// 查询后台任务列表。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">查询输入。</param>
public sealed record GetJobListQuery(Guid UserId, GetJobListInput Input) : IQuery<PagedResultDto<CloudDriveJobDto>>, ICloudDriveAccountScopedRequest;
