using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Video;

internal sealed class RememberVideoPlayProgressCommandHandler(
    CloudDriveFileManager fileManager,
    ICloudDriveWebApiGateway openApiGateway,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<RememberVideoPlayProgressCommand>
{
    public Task<Result> Handle(
        RememberVideoPlayProgressCommand request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure(
            CloudDriveFileHandlerSupport.ApiNotCollected("播放进度写入")));
    }

    private async Task<Result<string>> ResolvePickCodeAsync(
        Guid userId,
        RememberVideoPlayProgressInput input,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.FileId))
        {
            return string.IsNullOrWhiteSpace(input.PickCode)
                ? Result.Failure<string>(CloudDriveVideoSupport.PickCodeRequired)
                : Result.Success(input.PickCode);
        }

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;
        CloudDriveFileDetailDto? file = await fileManager.GetFileDetailAsync<CloudDriveFileDetailDto?>(
            userId,
            cloudAccountUid,
            input.FileId,
            CloudDriveFileHandlerSupport.CreateFileDetailContext(new GetFileInput
            {
                FileId = input.FileId,
            }),
            cancellationToken);

        if (file is null)
        {
            return Result.Failure<string>(CloudDriveFileHandlerSupport.FileNotFound(input.FileId));
        }

        return string.IsNullOrWhiteSpace(file.PickCode)
            ? Result.Failure<string>(CloudDriveVideoSupport.PickCodeRequired)
            : Result.Success(file.PickCode);
    }
}
