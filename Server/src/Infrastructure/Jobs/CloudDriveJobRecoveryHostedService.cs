using System.Text.Json;

using Application.Abstractions;
using Application.Abstractions.CloudDrive;

using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs;

/// <summary>
/// 应用启动时协调 115 网盘未完成后台任务。
/// </summary>
internal sealed class CloudDriveJobRecoveryHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<CloudDriveJobRecoveryHostedService> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        IServiceProvider services = scope.ServiceProvider;
        ICloudDriveJobRepository jobRepository = services.GetRequiredService<ICloudDriveJobRepository>();
        ICloudDriveJobScheduler jobScheduler = services.GetRequiredService<ICloudDriveJobScheduler>();
        ITransactionManager transactionManager = services.GetRequiredService<ITransactionManager>();
        ICloudDriveJobAuditLogger auditLogger = services.GetRequiredService<ICloudDriveJobAuditLogger>();

        IReadOnlyList<CloudDriveJob> jobs = await jobRepository.GetByStatusesAsync(
            [CloudDriveJobStatus.Pending, CloudDriveJobStatus.Running],
            cancellationToken);

        foreach (CloudDriveJob job in jobs)
        {
            if (IsRecoverable(job, out string failureReason))
            {
                await jobScheduler.ScheduleAsync(job.Id, job.JobType, cancellationToken);
                auditLogger.LogState(job, "recovery_scheduled");
                continue;
            }

            await MarkFailedAsync(
                jobRepository,
                transactionManager,
                auditLogger,
                job,
                failureReason,
                cancellationToken);
        }

        logger.LogInformation("115 后台任务启动恢复协调完成 Count={Count}", jobs.Count);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static bool IsRecoverable(CloudDriveJob job, out string failureReason)
    {
        if (string.IsNullOrWhiteSpace(job.PayloadJson))
        {
            failureReason = "后台任务缺少 PayloadJson，无法恢复。";
            return false;
        }

        if (job.JobType != CloudDriveJobType.Upload)
        {
            failureReason = string.Empty;
            return true;
        }

        string? tempFilePath = ReadTempFilePath(job.CheckpointData) ?? ReadTempFilePath(job.PayloadJson);
        if (string.IsNullOrWhiteSpace(tempFilePath) || !File.Exists(tempFilePath))
        {
            failureReason = "上传任务本地临时文件不可用，无法恢复。";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    private static string? ReadTempFilePath(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (root.TryGetProperty("tempFilePath", out JsonElement camel)
                || root.TryGetProperty("TempFilePath", out camel))
            {
                return camel.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static async Task MarkFailedAsync(
        ICloudDriveJobRepository jobRepository,
        ITransactionManager transactionManager,
        ICloudDriveJobAuditLogger auditLogger,
        CloudDriveJob job,
        string failureReason,
        CancellationToken cancellationToken)
    {
        await transactionManager.BeginAsync(cancellationToken);
        try
        {
            job.Fail(failureReason);
            await jobRepository.UpdateAsync(job, cancellationToken);
            await transactionManager.CommitAsync(cancellationToken);
            auditLogger.LogState(job, "recovery_failed");
        }
        catch
        {
            await transactionManager.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
