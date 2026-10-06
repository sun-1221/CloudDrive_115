using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.User;

public sealed record GetUserSpaceQuery(Guid UserId)
    : IQuery<CloudDriveSpaceInfoDto>, ICloudDriveAccountScopedRequest;
