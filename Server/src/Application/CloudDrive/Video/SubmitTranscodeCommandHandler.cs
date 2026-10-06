using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Video;

internal sealed class SubmitTranscodeCommandHandler(
    CloudDriveFileManager fileManager,
    ICloudDriveJobOrchestrator jobOrchestrator,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<SubmitTranscodeCommand, CloudDriveVideoTranscodeDto>
{
    public Task<Result<CloudDriveVideoTranscodeDto>> Handle(
        SubmitTranscodeCommand request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure<CloudDriveVideoTranscodeDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("主动转码")));
    }
}
