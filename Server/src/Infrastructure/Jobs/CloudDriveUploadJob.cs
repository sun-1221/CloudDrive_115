using System.Text.Json;

using Application.Abstractions;
using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Events;

using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using Domain.Exceptions;
using Domain.Repositories;

using Infrastructure.Http.Mappings;
using Infrastructure.Http.Responses;

using MediatR;

using Microsoft.Extensions.Logging;

using Quartz;

namespace Infrastructure.Jobs;

/// <summary>
/// 115 网盘异步上传 Quartz Job。
/// </summary>
internal sealed class CloudDriveUploadJob(
    ICloudDriveJobRepository jobRepository,
    ICloudDriveOpenApiGateway openApiGateway,
    ICloudDriveMetadataCache metadataCache,
    ITransactionManager transactionManager,
    IPublisher publisher,
    ICloudDriveJobAuditLogger auditLogger,
    ICloudDriveSensitiveFieldsFilter sensitiveFieldsFilter,
    ILogger<CloudDriveUploadJob> logger) : IJob
{
    private const string JobIdDataKey = "JobId";
    private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Guid jobId = ReadJobId(context);
        CloudDriveJob? job = await jobRepository.GetByIdAsync(jobId, context.CancellationToken);
        if (job is null)
        {
            logger.LogWarning("115 上传后台任务不存在 JobId={JobId}", jobId);
            return;
        }

        if (job.JobType != CloudDriveJobType.Upload)
        {
            await MarkFailedAsync(job, $"后台任务类型不匹配，期望 Upload，实际 {job.JobType}", context.CancellationToken);
            return;
        }

        UploadFileInput? input = DeserializePayload(job);
        if (input is null)
        {
            await MarkFailedAsync(job, "上传任务缺少有效 PayloadJson。", context.CancellationToken);
            return;
        }

        try
        {
            await StartJobAsync(job, context.CancellationToken);
            await ExecuteUploadAsync(job, input, context.CancellationToken);
            await CompleteJobAsync(job, input.DirectoryId, context.CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            auditLogger.LogError(job, ex);
            logger.LogError(ex, "115 上传后台任务失败 JobId={JobId}, CloudAccountUid={CloudAccountUid}", job.Id, sensitiveFieldsFilter.Redact(job.CloudAccountUid));
            await MarkFailedAsync(job, CreateSafeErrorMessage(ex), context.CancellationToken);
        }
    }

    private static Guid ReadJobId(IJobExecutionContext context)
    {
        string? value = context.MergedJobDataMap.GetString(JobIdDataKey);
        if (!Guid.TryParse(value, out Guid jobId))
        {
            throw new JobExecutionException("CloudDriveUploadJob requires a valid JobId in JobDataMap.");
        }

        return jobId;
    }

    private static UploadFileInput? DeserializePayload(CloudDriveJob job)
    {
        if (string.IsNullOrWhiteSpace(job.PayloadJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<UploadFileInput>(job.PayloadJson, PayloadJsonOptions);
    }

    private async Task ExecuteUploadAsync(
        CloudDriveJob job,
        UploadFileInput input,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.TempFilePath) || !File.Exists(input.TempFilePath))
        {
            throw new CloudDriveUploadException(
                $"上传临时文件不存在 JobId={job.Id}",
                "cloud_drive_upload_temp_file_missing");
        }

        CloudDriveUploadInitResponse initResponse = await openApiGateway.SendUploadInitRequestAsync<CloudDriveUploadInitResponse>(
            job.UserId,
            job.CloudAccountUid,
            BuildUploadInitContext(job.CloudAccountUid, input),
            cancellationToken);

        CloudDriveUploadSessionDto session = initResponse.ToDto();
        await UpdateCheckpointAsync(
            job,
            JsonSerializer.Serialize(
                new UploadCheckpoint(input.TempFilePath, session.PickCode ?? input.PickCode, session.ExpiresAt),
                PayloadJsonOptions),
            progress: initResponse.IsInstantUpload ? 100 : 30,
            cancellationToken);

        if (initResponse.IsInstantUpload)
        {
            return;
        }

        if (!initResponse.HasOssCredential || string.IsNullOrWhiteSpace(session.UploadEndpoint) || session.FormFields is null)
        {
            throw new CloudDriveUploadException(
                $"上传初始化未返回 OSS 直传凭证 JobId={job.Id}",
                "cloud_drive_upload_oss_credentials_missing");
        }

        await using FileStream fileStream = File.OpenRead(input.TempFilePath);
        await openApiGateway.UploadFileAsync<CloudDriveUploadCompleteResponse>(
            job.UserId,
            job.CloudAccountUid,
            session.UploadEndpoint,
            fileStream,
            input.FileName,
            session.FormFields,
            cancellationToken);

        await UpdateCheckpointAsync(job, job.CheckpointData, progress: 95, cancellationToken);
    }

    private static CloudDriveApiContext BuildUploadInitContext(string cloudAccountUid, UploadFileInput input)
    {
        return new CloudDriveApiContext
        {
            Category = "upload",
            FormFields = new Dictionary<string, string>
            {
                ["userid"] = cloudAccountUid,
                ["filename"] = input.FileName,
                ["filesize"] = input.FileSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["target"] = $"U_1_{input.DirectoryId}",
            },
        };
    }

    private async Task StartJobAsync(CloudDriveJob job, CancellationToken cancellationToken)
    {
        job.Start();
        job.UpdateProgress(1);
        await SaveAndPublishAsync(job, "started", cancellationToken);
    }

    private async Task UpdateCheckpointAsync(
        CloudDriveJob job,
        string? checkpointData,
        int progress,
        CancellationToken cancellationToken)
    {
        job.UpdateCheckpoint(checkpointData);
        job.UpdateProgress(progress);
        await SaveAndPublishAsync(job, "checkpoint", cancellationToken);
    }

    private async Task CompleteJobAsync(
        CloudDriveJob job,
        string directoryId,
        CancellationToken cancellationToken)
    {
        job.Complete();
        await SaveAndPublishAsync(job, "completed", cancellationToken);
        await metadataCache.InvalidateDirectoryAsync(job.UserId, job.CloudAccountUid, directoryId);
    }

    private async Task MarkFailedAsync(
        CloudDriveJob job,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        job.Fail(errorMessage);
        await SaveAndPublishAsync(job, "failed", cancellationToken);
    }

    private async Task SaveAndPublishAsync(CloudDriveJob job, string action, CancellationToken cancellationToken)
    {
        await transactionManager.BeginAsync(cancellationToken);
        try
        {
            await jobRepository.UpdateAsync(job, cancellationToken);
            await PublishDomainEventsAsync(job, cancellationToken);
            await transactionManager.CommitAsync(cancellationToken);
            auditLogger.LogState(job, action);
        }
        catch
        {
            await transactionManager.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task PublishDomainEventsAsync(CloudDriveJob job, CancellationToken cancellationToken)
    {
        DomainEvent[] events = [.. job.DomainEvents];
        foreach (DomainEvent domainEvent in events)
        {
            await publisher.Publish((object)DomainEventNotificationFactory.Create(domainEvent), cancellationToken);
        }

        job.ClearDomainEvents();
    }

    private static string CreateSafeErrorMessage(Exception exception)
    {
        return exception.Message.Length <= 500
            ? exception.Message
            : exception.Message[..500];
    }

    private sealed record UploadCheckpoint(string? TempFilePath, string? PickCode, DateTime? ExpiresAt);
}
