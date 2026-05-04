using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class InitUploadCommandHandler(
    ICloudDriveOpenApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<InitUploadCommand, CloudDriveUploadSessionDto>
{
    public async Task<Result<CloudDriveUploadSessionDto>> Handle(
        InitUploadCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveUploadSessionDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        CloudDriveUploadSessionDto result =
            await openApiGateway.SendUploadInitRequestAsync<CloudDriveUploadSessionDto>(
                request.UserId,
                request.Input.CloudAccountUid,
                CloudDriveFileHandlerSupport.CreateUploadInitContext(request.Input),
                cancellationToken);

        return Result.Success(result);
    }
}
