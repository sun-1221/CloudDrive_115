using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Video;

public sealed record GetVideoPlayUrlCommand(Guid UserId, GetVideoPlayInput Input)
    : ICommand<CloudDriveVideoPlayDto>;
