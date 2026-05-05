using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Commands;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Video;

internal sealed class SubmitTranscodeCommandHandler(
    CloudDriveFileManager fileManager,
    ICloudDriveJobOrchestrator jobOrchestrator,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<SubmitTranscodeCommand, CloudDriveVideoTranscodeDto>
{
    public async Task<Result<CloudDriveVideoTranscodeDto>> Handle(
        SubmitTranscodeCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        Result<SubmitTranscodeInput> inputResult = await NormalizeInputAsync(
            request.UserId,
            request.Input,
            cancellationToken);

        if (inputResult.IsFailure)
        {
            return Result.Failure<CloudDriveVideoTranscodeDto>(inputResult.Error);
        }

        Guid jobId = await jobOrchestrator.EnqueueVideoTranscodeAsync(
            request.UserId,
            accountContextAccessor.CloudAccountUid,
            inputResult.Value,
            cancellationToken);

        return Result.Success(new CloudDriveVideoTranscodeDto
        {
            FileId = inputResult.Value.FileId ?? inputResult.Value.PickCode ?? string.Empty,
            Status = "Pending",
            JobId = jobId,
        });
    }

    private async Task<Result<SubmitTranscodeInput>> NormalizeInputAsync(
        Guid userId,
        SubmitTranscodeInput input,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(input.FileId))
        {
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
                return Result.Failure<SubmitTranscodeInput>(CloudDriveFileHandlerSupport.FileNotFound(input.FileId));
            }

            if (!CloudDriveVideoSupport.IsVideo(file))
            {
                return Result.Failure<SubmitTranscodeInput>(CloudDriveVideoSupport.NotVideoFile);
            }

            if (string.IsNullOrWhiteSpace(file.PickCode))
            {
                return Result.Failure<SubmitTranscodeInput>(CloudDriveVideoSupport.PickCodeRequired);
            }

            input.PickCode = file.PickCode;
        }

        return string.IsNullOrWhiteSpace(input.PickCode)
            ? Result.Failure<SubmitTranscodeInput>(CloudDriveVideoSupport.PickCodeRequired)
            : Result.Success(input);
    }
}
