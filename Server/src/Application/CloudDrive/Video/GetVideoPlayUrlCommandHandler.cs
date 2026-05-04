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
    ICloudDriveOpenApiGateway openApiGateway,
    ICloudDriveJobOrchestrator jobOrchestrator,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<GetVideoPlayUrlCommand, CloudDriveVideoPlayDto>
{
    public async Task<Result<CloudDriveVideoPlayDto>> Handle(
        GetVideoPlayUrlCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveVideoPlayDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

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
            request.Input.CloudAccountUid,
            HttpMethod.Get,
            "files/video",
            CloudDriveVideoSupport.CreateVideoPlayContext(pickCodeResult.Value, request.Input.Definition),
            cancellationToken);

        CloudDriveVideoPlayDto dto = CloudDriveVideoSupport.MapVideoPlay(response, request.Input.Definition);
        if (dto.Status == VideoPlayStatus.Transcoding)
        {
            Guid jobId = await jobOrchestrator.EnqueueVideoTranscodeAsync(
                request.UserId,
                new SubmitTranscodeInput
                {
                    CloudAccountUid = request.Input.CloudAccountUid,
                    FileId = request.Input.FileId,
                    PickCode = pickCodeResult.Value,
                    Op = "vip_push",
                },
                cancellationToken);

            dto.JobId = jobId;
        }

        return Result.Success(dto);
    }

    private async Task<Result<string>> ResolvePickCodeAsync(
        Guid userId,
        GetVideoPlayInput input,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(input.FileId))
        {
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
