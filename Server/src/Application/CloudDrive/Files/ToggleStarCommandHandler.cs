using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class ToggleStarCommandHandler(
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<ToggleStarCommand, CloudDriveOperationResultDto>
{
    public Task<Result<CloudDriveOperationResultDto>> Handle(
        ToggleStarCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        return Task.FromResult(Result.Failure<CloudDriveOperationResultDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("星标切换")));
    }
}
