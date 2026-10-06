using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;

namespace Application.CloudDrive.Files;

internal sealed class DeleteFilesCommandHandler
    : ICommandHandler<DeleteFilesCommand, CloudDriveOperationResultDto>
{
    public Task<Result<CloudDriveOperationResultDto>> Handle(
        DeleteFilesCommand request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure<CloudDriveOperationResultDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("文件删除")));
    }
}
