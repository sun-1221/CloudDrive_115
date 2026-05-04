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
    ICloudDriveOpenApiGateway openApiGateway,
    ICloudDriveMetadataCache metadataCache,
    ITransactionManager transactionManager,
    IPublisher publisher,
    ICloudDriveJobAuditLogger auditLogger,
    ILogger<CloudDriveBatchOperationJob> logger) : IJob
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
            logger.LogWarning("115 批量后台任务不存在 JobId={JobId}", jobId);
            return;
        }

        BatchOperationPlan? plan = CreatePlan(job);
        if (plan is null)
        {
            await MarkFailedAsync(job, "批量任务类型或 PayloadJson 无效。", context.CancellationToken);
            return;
        }

        try
        {
            await StartJobAsync(job, context.CancellationToken);
            await ExecuteOperationAsync(job, plan, context.CancellationToken);
            await UpdateCheckpointAsync(job, CreateCheckpoint(plan), 95, context.CancellationToken);
            await CompleteJobAsync(job, plan.AffectedDirectoryIds, context.CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            auditLogger.LogError(job, ex);
            logger.LogError(
                ex,
                "115 批量后台任务失败 JobId={JobId}, CloudAccountUid={CloudAccountUid}, JobType={JobType}",
                job.Id,
                job.CloudAccountUid,
                job.JobType);

            await MarkFailedAsync(job, CreateSafeErrorMessage(ex), context.CancellationToken);
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
                job.PayloadJson,
                "rb/revert",
                new Dictionary<string, string>
                {
                    ["format"] = "json",
                },
                affectedDirectoryIds: ["0"]),
            CloudDriveJobType.BatchRecycleBinDelete => CreateRecycleBinPlan(
                job.PayloadJson,
                "rb/delete",
                new Dictionary<string, string>
                {
                    ["format"] = "json",
                    ["ignore_warn"] = "1",
                },
                affectedDirectoryIds: ["0"]),
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

        return new BatchOperationPlan(
            "files/copy",
            new Dictionary<string, string>
            {
                ["format"] = "json",
                ["fileIds"] = Join(input.FileIds),
                ["targetDirectoryId"] = input.TargetDirectoryId,
            },
            input.FileIds,
            [input.TargetDirectoryId]);
    }

    private static BatchOperationPlan? CreateMovePlan(string payloadJson)
    {
        MoveFilesInput? input = JsonSerializer.Deserialize<MoveFilesInput>(payloadJson, PayloadJsonOptions);
        if (input is null)
        {
            return null;
        }

        return new BatchOperationPlan(
            "files/move",
            new Dictionary<string, string>
            {
                ["format"] = "json",
                ["fileIds"] = Join(input.FileIds),
                ["targetDirectoryId"] = input.TargetDirectoryId,
            },
            input.FileIds,
            [input.TargetDirectoryId]);
    }

    private static BatchOperationPlan? CreateDeletePlan(string payloadJson)
    {
        DeleteFilesInput? input = JsonSerializer.Deserialize<DeleteFilesInput>(payloadJson, PayloadJsonOptions);
        if (input is null)
        {
            return null;
        }

        return new BatchOperationPlan(
            "rb/delete",
            new Dictionary<string, string>
            {
                ["aid"] = "7",
                ["format"] = "json",
                ["fileIds"] = Join(input.FileIds),
            },
            input.FileIds,
            ["0"]);
    }

    private static BatchOperationPlan? CreateRecycleBinPlan(
        string payloadJson,
        string endpoint,
        Dictionary<string, string> formFields,
        IReadOnlyList<string> affectedDirectoryIds)
    {
        RecycleBinOperationInput? input = JsonSerializer.Deserialize<RecycleBinOperationInput>(
            payloadJson,
            PayloadJsonOptions);
        if (input is null)
        {
            return null;
        }

        formFields["tid"] = Join(input.RecycleBinItemIds);
        return new BatchOperationPlan(endpoint, formFields, input.RecycleBinItemIds, affectedDirectoryIds);
    }

    private async Task ExecuteOperationAsync(
        CloudDriveJob job,
        BatchOperationPlan plan,
        CancellationToken cancellationToken)
    {
        var context = new CloudDriveApiContext
        {
            Category = "batch",
            FormFields = plan.FormFields,
        };

        await openApiGateway.SendWebApiRequestAsync<JsonElement>(
            job.UserId,
            job.CloudAccountUid,
            HttpMethod.Post,
            plan.Endpoint,
            context,
            cancellationToken);
    }

    private async Task StartJobAsync(CloudDriveJob job, CancellationToken cancellationToken)
    {
        job.Start();
        job.UpdateProgress(1);
        await SaveAndPublishAsync(job, "started", cancellationToken);
    }

    private async Task UpdateCheckpointAsync(
        CloudDriveJob job,
        string checkpointData,
        int progress,
        CancellationToken cancellationToken)
    {
        job.UpdateCheckpoint(checkpointData);
        job.UpdateProgress(progress);
        await SaveAndPublishAsync(job, "checkpoint", cancellationToken);
    }

    private async Task CompleteJobAsync(
        CloudDriveJob job,
        IReadOnlyList<string> affectedDirectoryIds,
        CancellationToken cancellationToken)
    {
        job.Complete();
        await SaveAndPublishAsync(job, "completed", cancellationToken);

        foreach (string directoryId in affectedDirectoryIds.Distinct(StringComparer.Ordinal))
        {
            await metadataCache.InvalidateDirectoryAsync(job.UserId, job.CloudAccountUid, directoryId);
        }
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

    private static string CreateCheckpoint(BatchOperationPlan plan)
    {
        return JsonSerializer.Serialize(
            new BatchOperationCheckpoint(
                plan.Endpoint,
                plan.ItemIds.Count,
                [.. plan.ItemIds]),
            PayloadJsonOptions);
    }

    private static string CreateSafeErrorMessage(Exception exception)
    {
        return exception.Message.Length <= 500
            ? exception.Message
            : exception.Message[..500];
    }

    private static string Join(IEnumerable<string> values) => string.Join(',', values);

    private sealed record BatchOperationPlan(
        string Endpoint,
        Dictionary<string, string> FormFields,
        IReadOnlyList<string> ItemIds,
        IReadOnlyList<string> AffectedDirectoryIds);

    private sealed record BatchOperationCheckpoint(
        string Endpoint,
        int CompletedCount,
        IReadOnlyList<string> CompletedItemIds);
}
