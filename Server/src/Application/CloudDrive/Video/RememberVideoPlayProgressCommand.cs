using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Video;

public sealed record RememberVideoPlayProgressCommand(Guid UserId, RememberVideoPlayProgressInput Input)
    : ICommand, ICloudDriveAccountScopedRequest;
