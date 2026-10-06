using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.User;

/// <summary>
/// 查询 115 用户中心聚合资料。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
public sealed record GetUserProfileQuery(Guid UserId)
    : IQuery<CloudDriveUserProfileDto>, ICloudDriveAccountScopedRequest;
