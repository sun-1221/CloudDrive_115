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
    ICloudDriveOpenApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<RememberVideoPlayProgressCommand>
{
    public async Task<Result> Handle(
        RememberVideoPlayProgressCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure(CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        if (request.Input.Time < 0)
        {
            return Result.Failure(CloudDriveVideoSupport.TimeInvalid);
        }

        Result<string> pickCodeResult = await ResolvePickCodeAsync(
            request.UserId,
            request.Input,
            cancellationToken);

        if (pickCodeResult.IsFailure)
        {
            return Result.Failure(pickCodeResult.Error);
        }

        await openApiGateway.SendWebApiRequestAsync<JsonElement>(
            request.UserId,
            request.Input.CloudAccountUid,
            HttpMethod.Post,
            "files/history",
            CloudDriveVideoSupport.CreateProgressWriteContext(request.Input, pickCodeResult.Value),
            cancellationToken);

        return Result.Success();
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

        CloudDriveFileDetailDto? file = await fileManager.GetFileDetailAsync<CloudDriveFileDetailDto?>(
            userId,
            input.CloudAccountUid,
            input.FileId,
            CloudDriveFileHandlerSupport.CreateFileDetailContext(new GetFileInput
            {
                CloudAccountUid = input.CloudAccountUid,
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
