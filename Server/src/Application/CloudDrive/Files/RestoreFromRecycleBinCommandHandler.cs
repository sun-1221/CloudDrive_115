using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;

namespace Application.CloudDrive.Files;

internal sealed class RestoreFromRecycleBinCommandHandler
    : ICommandHandler<RestoreFromRecycleBinCommand, CloudDriveOperationResultDto>
{
    public Task<Result<CloudDriveOperationResultDto>> Handle(
        RestoreFromRecycleBinCommand request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure<CloudDriveOperationResultDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("回收站还原")));
    }
}
