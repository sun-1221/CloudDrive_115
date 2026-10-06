using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Offline;

public sealed record GetOfflineQuotaQuery(Guid UserId)
    : IQuery<CloudDriveOfflineQuotaDto>, ICloudDriveAccountScopedRequest;
