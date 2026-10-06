using Application.Abstractions.CloudDrive;

using Domain.Enums;

using Quartz;

namespace Infrastructure.Jobs;

/// <summary>
/// 基于 Quartz.NET 的 115 网盘后台任务调度器。
/// </summary>
internal sealed class QuartzCloudDriveJobScheduler(ISchedulerFactory schedulerFactory) : ICloudDriveJobScheduler
{
    private const string JobGroup = "cloud-drive";
    private const string JobIdDataKey = "JobId";
    private const string JobTypeDataKey = "JobType";

    /// <inheritdoc />
    public async Task ScheduleAsync(
        Guid jobId,
        CloudDriveJobType jobType,
        CancellationToken cancellationToken = default)
    {
        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        JobKey jobKey = GetJobKey(jobType);
        TriggerKey triggerKey = new($"{jobType}-{jobId:N}", JobGroup);

        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(jobKey)
            .UsingJobData(JobIdDataKey, jobId.ToString("D"))
            .UsingJobData(JobTypeDataKey, jobType.ToString())
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(trigger, default, cancellationToken);
    }

    private static JobKey GetJobKey(CloudDriveJobType jobType)
    {
        string jobName = jobType switch
        {
            CloudDriveJobType.Upload => "CloudDriveUploadJob",
            CloudDriveJobType.BatchCopy
                or CloudDriveJobType.BatchMove
                or CloudDriveJobType.BatchDelete
                or CloudDriveJobType.BatchRecycleBinRestore
                or CloudDriveJobType.BatchRecycleBinDelete => "CloudDriveBatchOperationJob",
            CloudDriveJobType.ClearRecycleBin => "CloudDriveClearRecycleBinJob",
            CloudDriveJobType.Transcode => "CloudDriveTranscodeJob",
            _ => throw new ArgumentOutOfRangeException(nameof(jobType), jobType, "Unsupported cloud drive job type.")
        };

        return new JobKey(jobName, JobGroup);
    }
}
