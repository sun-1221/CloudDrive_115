using Application.Abstractions;
using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Video;

public sealed record GetVideoPlayProgressQuery(Guid UserId, GetVideoPlayProgressInput Input)
    : IQuery<CloudDriveVideoPlayProgressDto>, ICloudDriveAccountScopedRequest, IHasInput<GetVideoPlayProgressInput>;
