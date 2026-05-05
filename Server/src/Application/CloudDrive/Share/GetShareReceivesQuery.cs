using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Share;

public sealed record GetShareReceivesQuery(Guid UserId, GetShareListInput Input)
    : IQuery<CloudDriveShareListDto>, ICloudDriveAccountScopedRequest;
