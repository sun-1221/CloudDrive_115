using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Offline;

public sealed record GetOfflineQuotaQuery(Guid UserId, CloudDriveAccountScopedInput Input)
    : IQuery<CloudDriveOfflineQuotaDto>;
