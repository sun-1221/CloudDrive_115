using System.Text.Json;

using Application.Abstractions;
using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Events;

using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using Domain.Repositories;

using Infrastructure.Http.Responses;

using MediatR;

using Microsoft.Extensions.Logging;

using Quartz;

namespace Infrastructure.Jobs;

/// <summary>
/// 115 网盘视频转码 Quartz Job。
/// </summary>
internal sealed class CloudDriveTranscodeJob(
    ICloudDriveJobRepository jobRepository,
    ICloudDriveOpenApiGateway openApiGateway,
    ITransactionManager transactionManager,
    IPublisher publisher,
    ICloudDriveJobAuditLogger auditLogger,
    ILogger<CloudDriveTranscodeJob> logger) : IJob
{
    private const string JobIdDataKey = "JobId";
    private const int ProbeMaxAttempts = 3;
    private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Guid jobId = ReadJobId(context);
        CloudDriveJob? job = await jobRepository.GetByIdAsync(jobId, context.CancellationToken);
        if (job is null)
        {
            logger.LogWarning("115 视频转码后台任务不存在 JobId={JobId}", jobId);
            return;
        }

        if (job.JobType != CloudDriveJobType.Transcode)
        {
            await MarkFailedAsync(job, $"后台任务类型不匹配，期望 Transcode，实际 {job.JobType}", context.CancellationToken);
            return;
        }

        SubmitTranscodeInput? input = DeserializePayload(job);
        if (input is null || string.IsNullOrWhiteSpace(input.PickCode))
        {
            await MarkFailedAsync(job, "视频转码任务缺少有效 PickCode。", context.CancellationToken);
            return;
        }

        try
        {
            await StartJobAsync(job, context.CancellationToken);
            await SubmitTranscodeAsync(job, input, context.CancellationToken);

            bool isReady = await ProbeUntilReadyAsync(job, input, context.CancellationToken);
            if (!isReady)
            {
                await MarkFailedAsync(job, "视频转码尚未在本轮后台任务探测内完成。", context.CancellationToken);
                return;
            }

            await CompleteJobAsync(job, context.CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            auditLogger.LogError(job, ex);
            logger.LogError(
                ex,
                "115 视频转码后台任务失败 JobId={JobId}, CloudAccountUid={CloudAccountUid}",
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
            throw new JobExecutionException("CloudDriveTranscodeJob requires a valid JobId in JobDataMap.");
        }

        return jobId;
    }

    private static SubmitTranscodeInput? DeserializePayload(CloudDriveJob job)
    {
        if (string.IsNullOrWhiteSpace(job.PayloadJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<SubmitTranscodeInput>(job.PayloadJson, PayloadJsonOptions);
    }

    private async Task SubmitTranscodeAsync(
        CloudDriveJob job,
        SubmitTranscodeInput input,
        CancellationToken cancellationToken)
    {
        var context = new CloudDriveApiContext
        {
            Category = "video",
            QueryParameters = new Dictionary<string, string>
            {
                ["pickcode"] = input.PickCode!,
                ["op"] = input.Op,
                ["format"] = "json",
            },
        };

        await openApiGateway.SendWebApiRequestAsync<CloudDriveVideoPushResponse>(
            job.UserId,
            job.CloudAccountUid,
            HttpMethod.Get,
            "files/video_push",
            context,
            cancellationToken);

        await UpdateCheckpointAsync(job, CreateCheckpoint(input.PickCode!, "submitted", 0), 30, cancellationToken);
    }

    private async Task<bool> ProbeUntilReadyAsync(
        CloudDriveJob job,
        SubmitTranscodeInput input,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; attempt <= ProbeMaxAttempts; attempt++)
        {
            CloudDriveVideoPlayResponse response = await ProbePlayAsync(job, input.PickCode!, cancellationToken);
            bool isReady = response.VideoUrls.Count > 0
                || !string.Equals(response.Status, "transcoding", StringComparison.OrdinalIgnoreCase);

            await UpdateCheckpointAsync(
                job,
                CreateCheckpoint(input.PickCode!, isReady ? "ready" : "transcoding", attempt),
                isReady ? 95 : Math.Min(30 + attempt * 20, 90),
                cancellationToken);

            if (isReady)
            {
                return true;
            }
        }

        return false;
    }

    private Task<CloudDriveVideoPlayResponse> ProbePlayAsync(
        CloudDriveJob job,
        string pickCode,
        CancellationToken cancellationToken)
    {
        var context = new CloudDriveApiContext
        {
            Category = "video",
            QueryParameters = new Dictionary<string, string>
            {
                ["pickcode"] = pickCode,
                ["share_id"] = "0",
                ["local"] = "1",
                ["format"] = "json",
            },
        };

        return openApiGateway.SendWebApiRequestAsync<CloudDriveVideoPlayResponse>(
            job.UserId,
            job.CloudAccountUid,
            HttpMethod.Get,
            "files/video",
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

    private static string CreateCheckpoint(string pickCode, string status, int probeAttempt)
    {
        return JsonSerializer.Serialize(
            new TranscodeCheckpoint(pickCode, status, probeAttempt),
            PayloadJsonOptions);
    }

    private static string CreateSafeErrorMessage(Exception exception)
    {
        return exception.Message.Length <= 500
            ? exception.Message
            : exception.Message[..500];
    }

    private sealed record TranscodeCheckpoint(string PickCode, string Status, int ProbeAttempt);
}
