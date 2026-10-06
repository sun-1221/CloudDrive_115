using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class InitUploadCommandHandler(
    ICloudDriveWebApiGateway openApiGateway,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<InitUploadCommand, CloudDriveUploadSessionDto>
{
    public Task<Result<CloudDriveUploadSessionDto>> Handle(
        InitUploadCommand request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure<CloudDriveUploadSessionDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("文件上传")));
    }
}
