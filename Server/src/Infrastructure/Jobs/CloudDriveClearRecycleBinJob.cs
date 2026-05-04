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
/// 115 网盘清空回收站 Quartz Job。
/// </summary>
internal sealed class CloudDriveClearRecycleBinJob(
    ICloudDriveJobRepository jobRepository,
    ICloudDriveOpenApiGateway openApiGateway,
    ICloudDriveMetadataCache metadataCache,
    ITransactionManager transactionManager,
    IPublisher publisher,
    ICloudDriveJobAuditLogger auditLogger,
    ILogger<CloudDriveClearRecycleBinJob> logger) : IJob
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
            logger.LogWarning("115 清空回收站后台任务不存在 JobId={JobId}", jobId);
            return;
        }

        if (job.JobType != CloudDriveJobType.ClearRecycleBin)
        {
            await MarkFailedAsync(job, $"后台任务类型不匹配，期望 ClearRecycleBin，实际 {job.JobType}", context.CancellationToken);
            return;
        }

        ClearRecycleBinInput? input = DeserializePayload(job);
        if (input is null)
        {
            await MarkFailedAsync(job, "清空回收站任务缺少有效 PayloadJson。", context.CancellationToken);
            return;
        }

        try
        {
            await StartJobAsync(job, context.CancellationToken);
            await ExecuteClearAsync(job, input, context.CancellationToken);
            await UpdateCheckpointAsync(job, CreateCheckpoint(), 95, context.CancellationToken);
            await CompleteJobAsync(job, context.CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            auditLogger.LogError(job, ex);
            logger.LogError(
                ex,
                "115 清空回收站后台任务失败 JobId={JobId}, CloudAccountUid={CloudAccountUid}",
                job.Id,
                job.CloudAccountUid);

            await MarkFailedAsync(job, CreateSafeErrorMessage(ex), context.CancellationToken);
        }
    }

    private static Guid ReadJobId(IJobExecutionContext context)
    {
        string? value = context.MergedJobDataMap.GetString(JobIdDataKey);
        if (!Guid.TryParse(value, out Guid jobId))
        {
            throw new JobExecutionException("CloudDriveClearRecycleBinJob requires a valid JobId in JobDataMap.");
        }

        return jobId;
    }

    private static ClearRecycleBinInput? DeserializePayload(CloudDriveJob job)
    {
        if (string.IsNullOrWhiteSpace(job.PayloadJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<ClearRecycleBinInput>(job.PayloadJson, PayloadJsonOptions);
    }

    private async Task ExecuteClearAsync(
        CloudDriveJob job,
        ClearRecycleBinInput input,
        CancellationToken cancellationToken)
    {
        string cloudAccountUid = string.IsNullOrWhiteSpace(input.CloudAccountUid)
            ? job.CloudAccountUid
            : input.CloudAccountUid;

        var context = new CloudDriveApiContext
        {
            Category = "recycle-bin",
            FormFields = new Dictionary<string, string>
            {
                ["format"] = "json",
            },
        };

        await openApiGateway.SendWebApiRequestAsync<JsonElement>(
            job.UserId,
            cloudAccountUid,
            HttpMethod.Post,
            "rb/clean",
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

    private async Task CompleteJobAsync(CloudDriveJob job, CancellationToken cancellationToken)
    {
        job.Complete();
        await SaveAndPublishAsync(job, "completed", cancellationToken);
        await metadataCache.InvalidateDirectoryAsync(job.UserId, job.CloudAccountUid, "0");
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

    private static string CreateCheckpoint()
    {
        return JsonSerializer.Serialize(
            new ClearRecycleBinCheckpoint("rb/clean", DateTime.UtcNow),
            PayloadJsonOptions);
    }

    private static string CreateSafeErrorMessage(Exception exception)
    {
        return exception.Message.Length <= 500
            ? exception.Message
            : exception.Message[..500];
    }

    private sealed record ClearRecycleBinCheckpoint(string Endpoint, DateTime CompletedAtUtc);
}
