using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.User;

public sealed record GetUserVipQuery(Guid UserId, CloudDriveAccountScopedInput Input)
    : IQuery<CloudDriveVipInfoDto>;
