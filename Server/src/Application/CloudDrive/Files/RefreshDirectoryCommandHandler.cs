using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class RefreshDirectoryCommandHandler(
    ICloudDriveOpenApiGateway openApiGateway,
    ICloudDriveMetadataCache metadataCache,
    ICloudDriveRateLimiter rateLimiter,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<RefreshDirectoryCommand>
{
    public async Task<Result> Handle(
        RefreshDirectoryCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure(CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        string directoryId = CloudDriveFileHandlerSupport.NormalizeDirectoryId(request.Input.DirectoryId);
        await metadataCache.InvalidateDirectoryAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            directoryId);

        var listInput = new GetFileListInput
        {
            CloudAccountUid = request.Input.CloudAccountUid,
            DirectoryId = directoryId,
            Offset = 0,
            Limit = 1,
        };
        CloudDriveApiContext context = CloudDriveFileHandlerSupport.CreateFileListContext(listInput);

        using IDisposable permit = await rateLimiter.AcquireAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            context.Category,
            cancellationToken);

        await openApiGateway.SendWebApiRequestAsync<PagedResultDto<CloudDriveFileDto>>(
            request.UserId,
            request.Input.CloudAccountUid,
            HttpMethod.Get,
            "files",
            context,
            cancellationToken);

        return Result.Success();
    }
}
