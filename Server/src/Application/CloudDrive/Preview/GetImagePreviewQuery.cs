using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Preview;

public sealed record GetImagePreviewQuery(Guid UserId, GetPreviewInput Input)
    : IQuery<CloudDriveImagePreviewDto>, ICloudDriveAccountScopedRequest;
