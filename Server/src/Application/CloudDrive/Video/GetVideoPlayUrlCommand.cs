using Application.Abstractions;
using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Video;

public sealed record GetVideoPlayUrlCommand(Guid UserId, GetVideoPlayInput Input)
    : ICommand<CloudDriveVideoPlayDto>, ICloudDriveAccountScopedRequest, IHasInput<GetVideoPlayInput>;
