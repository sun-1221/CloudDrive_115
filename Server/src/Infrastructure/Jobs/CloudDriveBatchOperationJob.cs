using System.Text.Json;

using Application.Abstractions;
using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Events;

using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using Domain.Repositories;

using MediatR;

using Microsoft.Extensions.Logging;

using Quartz;

namespace Infrastructure.Jobs;

/// <summary>
/// 115 网盘批量文件与回收站操作 Quartz Job。
/// </summary>
internal sealed class CloudDriveBatchOperationJob(
    ICloudDriveJobRepository jobRepository,
    ITransactionManager transactionManager,
    IPublisher publisher,
    ICloudDriveJobAuditLogger auditLogger,
    ICloudDriveSensitiveFieldsFilter sensitiveFieldsFilter,
    ILogger<CloudDriveBatchOperationJob> logger) : IJob
{
    private const string JobIdDataKey = "JobId";
    private const string ApiNotCollectedCode = "cloud_drive_api_not_collected";
    private const string ApiNotCollectedMessage = "批量写操作 115 网页端接口尚未采集，待补采集后再接入。";
    private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        Guid jobId = ReadJobId(context);
        CloudDriveJob? job = await jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            logger.LogWarning("115 批量后台任务不存在 JobId={JobId}", jobId);
            return;
        }

        BatchOperationPlan? plan = CreatePlan(job);
        if (plan is null)
        {
            await MarkFailedAsync(job, "批量任务类型或 PayloadJson 无效。", cancellationToken);
            return;
        }

        try
        {
            await StartJobAsync(job, cancellationToken);
            await MarkFailedAsync(job, $"{ApiNotCollectedCode}: {ApiNotCollectedMessage}", cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            auditLogger.LogError(job, ex);
            logger.LogError(
                ex,
                "115 批量后台任务失败 JobId={JobId}, CloudAccountUid={CloudAccountUid}, JobType={JobType}",
                job.Id,
                sensitiveFieldsFilter.Redact(job.CloudAccountUid),
                job.JobType);

            await MarkFailedAsync(job, CreateSafeErrorMessage(ex), cancellationToken);
        }
    }

    private static Guid ReadJobId(IJobExecutionContext context)
    {
        string? value = context.MergedJobDataMap.GetString(JobIdDataKey);
        if (!Guid.TryParse(value, out Guid jobId))
        {
            throw new JobExecutionException("CloudDriveBatchOperationJob requires a valid JobId in JobDataMap.");
        }

        return jobId;
    }

    private static BatchOperationPlan? CreatePlan(CloudDriveJob job)
    {
        if (string.IsNullOrWhiteSpace(job.PayloadJson))
        {
            return null;
        }

        return job.JobType switch
        {
            CloudDriveJobType.BatchCopy => CreateCopyPlan(job.PayloadJson),
            CloudDriveJobType.BatchMove => CreateMovePlan(job.PayloadJson),
            CloudDriveJobType.BatchDelete => CreateDeletePlan(job.PayloadJson),
            CloudDriveJobType.BatchRecycleBinRestore => CreateRecycleBinPlan(
                job.PayloadJson),
            CloudDriveJobType.BatchRecycleBinDelete => CreateRecycleBinPlan(
                job.PayloadJson),
            _ => null
        };
    }

    private static BatchOperationPlan? CreateCopyPlan(string payloadJson)
    {
        CopyFilesInput? input = JsonSerializer.Deserialize<CopyFilesInput>(payloadJson, PayloadJsonOptions);
        if (input is null)
        {
            return null;
        }

        return new BatchOperationPlan(input.FileIds);
    }

    private static BatchOperationPlan? CreateMovePlan(string payloadJson)
    {
        MoveFilesInput? input = JsonSerializer.Deserialize<MoveFilesInput>(payloadJson, PayloadJsonOptions);
        if (input is null)
        {
            return null;
        }

        return new BatchOperationPlan(input.FileIds);
    }

    private static BatchOperationPlan? CreateDeletePlan(string payloadJson)
    {
        DeleteFilesInput? input = JsonSerializer.Deserialize<DeleteFilesInput>(payloadJson, PayloadJsonOptions);
        if (input is null)
        {
            return null;
        }

        return new BatchOperationPlan(input.FileIds);
    }

    private static BatchOperationPlan? CreateRecycleBinPlan(string payloadJson)
    {
        RecycleBinOperationInput? input = JsonSerializer.Deserialize<RecycleBinOperationInput>(
            payloadJson,
            PayloadJsonOptions);
        if (input is null)
        {
            return null;
        }

        return new BatchOperationPlan(input.RecycleBinItemIds);
    }

    private async Task StartJobAsync(CloudDriveJob job, CancellationToken cancellationToken)
    {
        job.Start();
        job.UpdateProgress(1);
        await SaveAndPublishAsync(job, "started", cancellationToken);
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

    private sealed record BatchOperationPlan(IReadOnlyList<string> ItemIds);
}
