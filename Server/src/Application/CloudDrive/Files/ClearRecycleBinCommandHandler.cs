using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;

namespace Application.CloudDrive.Files;

internal sealed class ClearRecycleBinCommandHandler
    : ICommandHandler<ClearRecycleBinCommand, CloudDriveOperationResultDto>
{
    public Task<Result<CloudDriveOperationResultDto>> Handle(
        ClearRecycleBinCommand request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure<CloudDriveOperationResultDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("清空回收站")));
    }
}
