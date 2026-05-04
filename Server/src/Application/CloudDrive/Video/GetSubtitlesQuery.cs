using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Video;

public sealed record GetSubtitlesQuery(Guid UserId, GetSubtitlesInput Input)
    : IQuery<CloudDriveSubtitleListDto>;
