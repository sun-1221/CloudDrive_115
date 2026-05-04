using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class GetFileDetailQueryHandler(
    CloudDriveFileManager fileManager,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetFileDetailQuery, CloudDriveFileDetailDto>
{
    public async Task<Result<CloudDriveFileDetailDto>> Handle(
        GetFileDetailQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveFileDetailDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        string fileId = request.Input.FileId ?? request.Input.Path ?? string.Empty;
        CloudDriveApiContext context = CloudDriveFileHandlerSupport.CreateFileDetailContext(request.Input);

        CloudDriveFileDetailDto? result = await fileManager.GetFileDetailAsync<CloudDriveFileDetailDto?>(
            request.UserId,
            request.Input.CloudAccountUid,
            fileId,
            context,
            cancellationToken);

        if (result is null)
        {
            return Result.Failure<CloudDriveFileDetailDto>(
                CloudDriveFileHandlerSupport.FileNotFound(fileId));
        }

        return Result.Success(result);
    }
}
