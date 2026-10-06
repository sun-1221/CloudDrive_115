using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Video;

internal sealed class GetVideoPlayUrlCommandHandler(
    CloudDriveFileManager fileManager,
    ICloudDriveWebApiGateway openApiGateway,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<GetVideoPlayUrlCommand, CloudDriveVideoPlayDto>
{
    public async Task<Result<CloudDriveVideoPlayDto>> Handle(
        GetVideoPlayUrlCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        Result<string> pickCodeResult = await ResolvePickCodeAsync(
            request.UserId,
            request.Input,
            cancellationToken);

        if (pickCodeResult.IsFailure)
        {
            return Result.Failure<CloudDriveVideoPlayDto>(pickCodeResult.Error);
        }

        JsonElement response = await openApiGateway.SendWebApiRequestAsync<JsonElement>(
            request.UserId,
            cloudAccountUid,
            HttpMethod.Get,
            "files/video",
            CloudDriveVideoSupport.CreateVideoPlayContext(pickCodeResult.Value, request.Input.Definition),
            cancellationToken);

        CloudDriveVideoPlayDto dto = CloudDriveVideoSupport.MapVideoPlay(response, request.Input.Definition);
        return Result.Success(dto);
    }

    private async Task<Result<string>> ResolvePickCodeAsync(
        Guid userId,
        GetVideoPlayInput input,
        CancellationToken cancellationToken)
    {
        string cloudAccountUid = accountContextAccessor.CloudAccountUid;
        if (!string.IsNullOrWhiteSpace(input.FileId))
        {
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

            if (!CloudDriveVideoSupport.IsVideo(file))
            {
                return Result.Failure<string>(CloudDriveVideoSupport.NotVideoFile);
            }

            return string.IsNullOrWhiteSpace(file.PickCode)
                ? Result.Failure<string>(CloudDriveVideoSupport.PickCodeRequired)
                : Result.Success(file.PickCode);
        }

        return string.IsNullOrWhiteSpace(input.PickCode)
            ? Result.Failure<string>(CloudDriveVideoSupport.PickCodeRequired)
            : Result.Success(input.PickCode);
    }
}
