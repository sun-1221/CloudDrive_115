using Application.Abstractions.CloudDrive;

using Domain.Entities;

using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs;

/// <summary>
/// 115 网盘后台任务审计日志记录器。只记录脱敏摘要，不输出 Cookie、签名或 PayloadJson。
/// </summary>
internal sealed class CloudDriveJobAuditLogger(
    ICloudDriveSensitiveFieldsFilter sensitiveFieldsFilter,
    ILogger<CloudDriveJobAuditLogger> logger) : ICloudDriveJobAuditLogger
{
    /// <inheritdoc />
    public void LogCreated(CloudDriveJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        logger.LogInformation(
            "115 后台任务已创建 UserId={UserId}, CloudAccountUid={CloudAccountUid}, JobId={JobId}, JobType={JobType}, ResourceId={ResourceId}",
            job.UserId,
            Redact(job.CloudAccountUid),
            job.Id,
            job.JobType,
            Redact(job.ResourceId));
    }

    /// <inheritdoc />
    public void LogState(CloudDriveJob job, string action)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        logger.LogInformation(
            "115 后台任务状态变更 Action={Action}, UserId={UserId}, CloudAccountUid={CloudAccountUid}, JobId={JobId}, JobType={JobType}, Status={Status}, Progress={Progress}, CheckpointSummary={CheckpointSummary}, ErrorSummary={ErrorSummary}",
            Redact(action),
            job.UserId,
            Redact(job.CloudAccountUid),
            job.Id,
            job.JobType,
            job.Status,
            job.Progress,
            CreateCheckpointSummary(job.CheckpointData),
            Redact(job.ErrorMessage));
    }

    /// <inheritdoc />
    public void LogError(CloudDriveJob job, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(exception);

        logger.LogError(
            exception,
            "115 后台任务错误 UserId={UserId}, CloudAccountUid={CloudAccountUid}, JobId={JobId}, JobType={JobType}, Status={Status}, ErrorSummary={ErrorSummary}",
            job.UserId,
            Redact(job.CloudAccountUid),
            job.Id,
            job.JobType,
            job.Status,
            Redact(exception.Message));
    }

    private string Redact(string? value) => sensitiveFieldsFilter.Redact(value);

    private string CreateCheckpointSummary(string? checkpointData)
    {
        if (string.IsNullOrWhiteSpace(checkpointData))
        {
            return string.Empty;
        }

        string redacted = Redact(checkpointData);
        return redacted.Length <= 160 ? redacted : redacted[..160];
    }
}
