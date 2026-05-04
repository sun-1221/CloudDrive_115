using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Preview;

public sealed record GetDocPreviewQuery(Guid UserId, GetPreviewInput Input)
    : IQuery<CloudDriveDocPreviewDto>;
