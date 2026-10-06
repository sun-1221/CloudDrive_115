using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;

namespace Application.CloudDrive.Files;

internal sealed class MoveFilesCommandHandler
    : ICommandHandler<MoveFilesCommand, CloudDriveOperationResultDto>
{
    public Task<Result<CloudDriveOperationResultDto>> Handle(
        MoveFilesCommand request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure<CloudDriveOperationResultDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("文件移动")));
    }
}
