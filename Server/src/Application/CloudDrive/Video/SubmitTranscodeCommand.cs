using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Video;

public sealed record SubmitTranscodeCommand(Guid UserId, SubmitTranscodeInput Input)
    : ICommand<CloudDriveVideoTranscodeDto>, ICloudDriveAccountScopedRequest;
