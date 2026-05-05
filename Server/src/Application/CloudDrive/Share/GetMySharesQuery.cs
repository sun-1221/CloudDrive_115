using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Share;

public sealed record GetMySharesQuery(Guid UserId, GetShareListInput Input)
    : IQuery<CloudDriveShareListDto>, ICloudDriveAccountScopedRequest;
