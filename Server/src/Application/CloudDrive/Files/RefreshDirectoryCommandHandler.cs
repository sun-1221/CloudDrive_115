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
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<RefreshDirectoryCommand>
{
    public async Task<Result> Handle(
        RefreshDirectoryCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;
        string directoryId = CloudDriveFileHandlerSupport.NormalizeDirectoryId(request.Input.DirectoryId);
        await metadataCache.InvalidateDirectoryAsync(
            request.UserId,
            cloudAccountUid,
            directoryId,
            cancellationToken);

        var listInput = new GetFileListInput
        {
            DirectoryId = directoryId,
            Offset = 0,
            Limit = 1,
        };
        CloudDriveApiContext context = CloudDriveFileHandlerSupport.CreateFileListContext(listInput);

        using IDisposable permit = await rateLimiter.AcquireAsync(
            request.UserId,
            cloudAccountUid,
            context.Category,
            cancellationToken);

        await openApiGateway.SendWebApiRequestAsync<PagedResultDto<CloudDriveFileDto>>(
            request.UserId,
            cloudAccountUid,
            HttpMethod.Get,
            "files",
            context,
            cancellationToken);

        return Result.Success();
    }
}
