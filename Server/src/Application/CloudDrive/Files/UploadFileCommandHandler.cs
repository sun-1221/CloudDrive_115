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
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<UploadFileCommand, CloudDriveUploadExecutionDto>
{
    public async Task<Result<CloudDriveUploadExecutionDto>> Handle(
        UploadFileCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        if (ShouldRunAsync(request.Input, configOptions.Value))
        {
            if (string.IsNullOrWhiteSpace(request.Input.TempFilePath))
            {
                if (request.FileStream is null)
                {
                    return Result.Failure<CloudDriveUploadExecutionDto>(
                        CloudDriveFileHandlerSupport.UploadStreamRequired);
                }

                // 将文件流暂存到临时文件，供后台任务恢复使用
                string tempFilePath = Path.Combine(Path.GetTempPath(), $"115upload_{Guid.NewGuid():N}");
                await using (var fs = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    await request.FileStream.CopyToAsync(fs, cancellationToken);
                }

                request.Input.TempFilePath = tempFilePath;
            }

            Guid jobId = await jobOrchestrator.EnqueueLargeUploadAsync(
                request.UserId,
                cloudAccountUid,
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
                cloudAccountUid,
                CloudDriveFileHandlerSupport.CreateUploadInitContext(cloudAccountUid, request.Input),
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
                cloudAccountUid,
                session.UploadEndpoint,
                request.FileStream,
                request.Input.FileName,
                session.FormFields,
                cancellationToken);
        }

        await metadataCache.InvalidateDirectoryAsync(
            request.UserId,
            cloudAccountUid,
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
