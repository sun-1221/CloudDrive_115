using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.User;

/// <summary>
/// 查询 115 用户中心聚合资料。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">账号上下文输入。</param>
public sealed record GetUserProfileQuery(Guid UserId, CloudDriveAccountScopedInput Input)
    : IQuery<CloudDriveUserProfileDto>;
