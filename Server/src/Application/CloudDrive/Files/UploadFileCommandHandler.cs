using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Commands;

using Domain.Exceptions;
using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class UploadFileCommandHandler(
    ICloudDriveOpenApiGateway openApiGateway,
    ICloudDriveJobOrchestrator jobOrchestrator,
    ICloudDriveMetadataCache metadataCache,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<UploadFileCommand, CloudDriveUploadExecutionDto>
{
    public async Task<Result<CloudDriveUploadExecutionDto>> Handle(
        UploadFileCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.CloudAccountUid))
        {
            return Result.Failure<CloudDriveUploadExecutionDto>(
                CloudDriveFileHandlerSupport.CloudAccountUidRequired);
        }

        if (ShouldRunAsync(request.Input, configOptions.Value))
        {
            if (string.IsNullOrWhiteSpace(request.Input.TempFilePath))
            {
                return Result.Failure<CloudDriveUploadExecutionDto>(
                    CloudDriveFileHandlerSupport.UploadStreamRequired);
            }

            Guid jobId = await jobOrchestrator.EnqueueLargeUploadAsync(
                request.UserId,
                request.Input,
                cancellationToken);

            return Result.Success(CloudDriveFileHandlerSupport.CreateAcceptedUploadResult(jobId));
        }

        if (request.FileStream is null)
        {
            return Result.Failure<CloudDriveUploadExecutionDto>(
                CloudDriveFileHandlerSupport.UploadStreamRequired);
        }

        CloudDriveUploadSessionDto session =
            await openApiGateway.SendUploadInitRequestAsync<CloudDriveUploadSessionDto>(
                request.UserId,
                request.Input.CloudAccountUid,
                CloudDriveFileHandlerSupport.CreateUploadInitContext(request.Input),
                cancellationToken);

        CloudDriveUploadedFileDto uploadedFile;
        if (session.Status == 2)
        {
            uploadedFile = CloudDriveFileHandlerSupport.CreateUploadedFileFromSession(session, request.Input);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(session.UploadEndpoint) || session.FormFields is null)
            {
                throw new CloudDriveUploadException(
                    "上传初始化未返回 OSS 直传凭证。",
                    "cloud_drive_upload_oss_credentials_missing");
            }

            uploadedFile = await openApiGateway.UploadFileAsync<CloudDriveUploadedFileDto>(
                request.UserId,
                request.Input.CloudAccountUid,
                session.UploadEndpoint,
                request.FileStream,
                request.Input.FileName,
                session.FormFields,
                cancellationToken);
        }

        await metadataCache.InvalidateDirectoryAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            request.Input.DirectoryId);

        return Result.Success(new CloudDriveUploadExecutionDto
        {
            Accepted = false,
            Message = "上传完成。",
            File = uploadedFile,
        });
    }

    private static bool ShouldRunAsync(UploadFileInput input, CloudDriveAppConfig config)
    {
        return input.ForceAsync || input.FileSize > config.LargeFileAsyncThresholdBytes;
    }
}
