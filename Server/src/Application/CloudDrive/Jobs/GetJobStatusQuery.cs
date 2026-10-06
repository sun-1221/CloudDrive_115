using Application.Abstractions;
using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Jobs;

/// <summary>
/// 查询单个后台任务状态。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">查询输入。</param>
public sealed record GetJobStatusQuery(Guid UserId, GetJobStatusInput Input) : IQuery<CloudDriveJobDto>, ICloudDriveAccountScopedRequest, IHasInput<GetJobStatusInput>;
