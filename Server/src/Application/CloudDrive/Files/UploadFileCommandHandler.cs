using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;

namespace Application.CloudDrive.Files;

internal sealed class UploadFileCommandHandler
    : ICommandHandler<UploadFileCommand, CloudDriveUploadExecutionDto>
{
    public Task<Result<CloudDriveUploadExecutionDto>> Handle(
        UploadFileCommand request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure<CloudDriveUploadExecutionDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("文件上传")));
    }
}
