using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class InitUploadCommandHandler(
    ICloudDriveOpenApiGateway openApiGateway,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<InitUploadCommand, CloudDriveUploadSessionDto>
{
    public async Task<Result<CloudDriveUploadSessionDto>> Handle(
        InitUploadCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        CloudDriveUploadSessionDto result =
            await openApiGateway.SendUploadInitRequestAsync<CloudDriveUploadSessionDto>(
                request.UserId,
                cloudAccountUid,
                CloudDriveFileHandlerSupport.CreateUploadInitContext(cloudAccountUid, request.Input),
                cancellationToken);

        return Result.Success(result);
    }
}
