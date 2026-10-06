using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;

using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;

using Microsoft.Extensions.Logging;

namespace Application.CloudDrive.Jobs;

/// <summary>
/// 115 网盘后台任务编排器实现。<see cref="CloudDriveJob"/> 是状态与恢复信息的唯一事实来源。
/// </summary>
public sealed class CloudDriveJobOrchestrator(
    ICloudDriveJobRepository jobRepository,
    ICloudDriveJobScheduler jobScheduler,
    ICloudDriveJobAuditLogger auditLogger,
    ICloudDriveSensitiveFieldsFilter sensitiveFieldsFilter,
    ILogger<CloudDriveJobOrchestrator> logger) : ICloudDriveJobOrchestrator
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<Guid> EnqueueLargeUploadAsync(
        Guid userId,
        string cloudAccountUid,
        UploadFileInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        EnsureCapabilityEnabled(CloudDriveJobType.Upload);
        EnsureCapabilityEnabled(CloudDriveJobType.ClearRecycleBin);
        EnsureValidUserId(userId);
        EnsureValidCloudAccountUid(cloudAccountUid);

        return await EnqueueAsync(
            userId,
            cloudAccountUid,
            CloudDriveJobType.Upload,
            input,
            input.DirectoryId,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid> EnqueueBatchOperationAsync(
        Guid userId,
        string cloudAccountUid,
        CloudDriveJobType jobType,
        object payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        EnsureCapabilityEnabled(jobType);
        EnsureValidUserId(userId);
        EnsureValidCloudAccountUid(cloudAccountUid);
        EnsureBatchJobType(jobType);

        return await EnqueueAsync(
            userId,
            cloudAccountUid,
            jobType,
            payload,
            resourceId: null,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid> EnqueueClearRecycleBinAsync(
        Guid userId,
        string cloudAccountUid,
        CancellationToken cancellationToken = default)
    {
        EnsureCapabilityEnabled(CloudDriveJobType.ClearRecycleBin);
        EnsureValidUserId(userId);
        EnsureValidCloudAccountUid(cloudAccountUid);

        var payload = new ClearRecycleBinInput();

        return await EnqueueAsync(
            userId,
            cloudAccountUid,
            CloudDriveJobType.ClearRecycleBin,
            payload,
            resourceId: "recycle-bin",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Guid> EnqueueVideoTranscodeAsync(
        Guid userId,
        string cloudAccountUid,
        SubmitTranscodeInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        EnsureCapabilityEnabled(CloudDriveJobType.Transcode);
        EnsureValidUserId(userId);
        EnsureValidCloudAccountUid(cloudAccountUid);

        string? resourceId = string.IsNullOrWhiteSpace(input.FileId)
            ? input.PickCode
            : input.FileId;

        return await EnqueueAsync(
            userId,
            cloudAccountUid,
            CloudDriveJobType.Transcode,
            input,
            resourceId,
            cancellationToken);
    }

    private async Task<Guid> EnqueueAsync(
        Guid userId,
        string cloudAccountUid,
        CloudDriveJobType jobType,
        object payload,
        string? resourceId,
        CancellationToken cancellationToken)
    {
        string payloadJson = JsonSerializer.Serialize(payload, PayloadJsonOptions);
        CloudDriveJob job = CloudDriveJob.Create(
            userId,
            cloudAccountUid,
            jobType,
            resourceId,
            payloadJson);

        await jobRepository.InsertAsync(job, cancellationToken);
        await jobScheduler.ScheduleAsync(job.Id, job.JobType, cancellationToken);
        auditLogger.LogCreated(job);

        logger.LogInformation(
            "已创建 115 网盘后台任务 UserId={UserId}, CloudAccountUid={CloudAccountUid}, JobId={JobId}, JobType={JobType}",
            userId,
            sensitiveFieldsFilter.Redact(cloudAccountUid),
            job.Id,
            job.JobType);

        return job.Id;
    }

    private static void EnsureValidUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }
    }

    private static void EnsureValidCloudAccountUid(string cloudAccountUid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cloudAccountUid);
    }

    private static void EnsureBatchJobType(CloudDriveJobType jobType)
    {
        if (jobType is not (CloudDriveJobType.BatchCopy
            or CloudDriveJobType.BatchMove
            or CloudDriveJobType.BatchDelete
            or CloudDriveJobType.BatchRecycleBinRestore
            or CloudDriveJobType.BatchRecycleBinDelete))
        {
            throw new ArgumentOutOfRangeException(
                nameof(jobType),
                jobType,
                "Job type must be a batch operation type.");
        }
    }

    private static void EnsureCapabilityEnabled(CloudDriveJobType jobType)
    {
        throw new CloudDriveApiNotCollectedException(
            $"后台任务类型 {jobType} 对应的 115 网页端接口尚未采集。");
    }
}
