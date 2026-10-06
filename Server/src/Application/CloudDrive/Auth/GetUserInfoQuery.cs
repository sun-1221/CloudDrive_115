using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Auth;

/// <summary>
/// 聚合查询指定 115 账号的用户信息。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
public sealed record GetUserInfoQuery(Guid UserId)
    : IQuery<CloudDriveUserInfoDto>, ICloudDriveAccountScopedRequest;
