using System.Text.Json;

using Application.Abstractions;
using Application.Abstractions.CloudDrive;

using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

namespace Infrastructure.Jobs;

/// <summary>
/// 协调 115 网盘启动恢复流程，集中持有 scoped 仓储、事务与调度依赖。
/// </summary>
/// <param name="jobRepository">后台任务仓储。</param>
/// <param name="jobScheduler">后台任务调度器。</param>
/// <param name="transactionManager">事务管理器。</param>
/// <param name="auditLogger">后台任务审计日志记录器。</param>
internal sealed class CloudDriveJobRecoveryCoordinator(
    ICloudDriveJobRepository jobRepository,
    ICloudDriveJobScheduler jobScheduler,
    ITransactionManager transactionManager,
    ICloudDriveJobAuditLogger auditLogger)
{
    /// <summary>
    /// 恢复可继续执行的任务，并将不可恢复任务标记为失败。
    /// </summary>
    /// <param name="cancellationToken">用于取消启动恢复流程的令牌。</param>
    /// <returns>本次参与恢复协调的任务数量。</returns>
    public async Task<int> RecoverAsync(CancellationToken cancellationToken)
    {
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

            await MarkFailedAsync(job, failureReason, cancellationToken);
        }

        return jobs.Count;
    }

    /// <summary>
    /// 判断任务是否具备启动恢复所需的最小持久化上下文。
    /// </summary>
    /// <param name="job">待检查的后台任务。</param>
    /// <param name="failureReason">不可恢复时返回的失败原因。</param>
    /// <returns>具备恢复条件时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    private static bool IsRecoverable(CloudDriveJob job, out string failureReason)
    {
        if (!IsCapabilityEnabled(job.JobType))
        {
            failureReason = "cloud_drive_api_not_collected: 任务对应的 115 网页端接口尚未采集。";
            return false;
        }

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

    private static bool IsCapabilityEnabled(CloudDriveJobType jobType) => false;

    /// <summary>
    /// 从任务载荷或检查点 JSON 中读取上传临时文件路径。
    /// </summary>
    /// <param name="json">任务载荷或检查点 JSON。</param>
    /// <returns>存在时返回本地临时文件路径；否则返回 <see langword="null"/>。</returns>
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

    /// <summary>
    /// 在独立事务中将不可恢复任务标记为失败。
    /// </summary>
    /// <param name="job">需要标记失败的后台任务。</param>
    /// <param name="failureReason">失败原因。</param>
    /// <param name="cancellationToken">用于取消持久化操作的令牌。</param>
    /// <returns>表示异步事务提交的任务。</returns>
    private async Task MarkFailedAsync(
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
