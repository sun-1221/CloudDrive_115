using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class GetFileListQueryHandler(
    CloudDriveFileManager fileManager,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetFileListQuery, PagedResultDto<CloudDriveFileDto>>
{
    public async Task<Result<PagedResultDto<CloudDriveFileDto>>> Handle(
        GetFileListQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;
        string directoryId = CloudDriveFileHandlerSupport.NormalizeDirectoryId(request.Input.DirectoryId);
        CloudDriveApiContext context = CloudDriveFileHandlerSupport.CreateFileListContext(request.Input);

        PagedResultDto<CloudDriveFileDto> result = await fileManager.GetFileListAsync<PagedResultDto<CloudDriveFileDto>>(
            request.UserId,
            cloudAccountUid,
            directoryId,
            context,
            cancellationToken);

        return Result.Success(result);
    }
}
