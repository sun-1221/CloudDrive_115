using Domain.Entities;
using Domain.Enums;
using Domain.Events;

namespace Domain.Tests.Entities;

/// <summary>
/// 115 网盘后台任务聚合根测试。
/// </summary>
public sealed class CloudDriveJobTests
{
    private static readonly Guid TestUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string TestCloudAccountUid = "115-user-001";

    /// <summary>
    /// 验证待处理任务启动后状态变为运行中并追加状态变更事件。
    /// </summary>
    [Fact]
    public void Start_WhenJobIsPending_ChangesStatusToRunningAndAppendsEvent()
    {
        // Arrange
        CloudDriveJob job = CreateUploadJob();

        // Act
        job.Start();

        // Assert
        Assert.Equal(CloudDriveJobStatus.Running, job.Status);
        Assert.NotNull(job.StartTime);
        CloudDriveJobStatusChanged domainEvent = Assert.IsType<CloudDriveJobStatusChanged>(Assert.Single(job.DomainEvents));
        Assert.Equal(CloudDriveJobStatus.Pending, domainEvent.PreviousStatus);
        Assert.Equal(CloudDriveJobStatus.Running, domainEvent.NewStatus);
    }

    /// <summary>
    /// 验证运行中任务完成后状态变为已完成。
    /// </summary>
    [Fact]
    public void Complete_WhenJobIsRunning_ChangesStatusToCompleted()
    {
        // Arrange
        CloudDriveJob job = CreateUploadJob();
        job.Start();
        job.ClearDomainEvents();

        // Act
        job.Complete();

        // Assert
        Assert.Equal(CloudDriveJobStatus.Completed, job.Status);
        Assert.Equal(100, job.Progress);
        Assert.Null(job.ErrorMessage);
        Assert.NotNull(job.EndTime);
        CloudDriveJobStatusChanged domainEvent = Assert.IsType<CloudDriveJobStatusChanged>(Assert.Single(job.DomainEvents));
        Assert.Equal(CloudDriveJobStatus.Running, domainEvent.PreviousStatus);
        Assert.Equal(CloudDriveJobStatus.Completed, domainEvent.NewStatus);
    }

    /// <summary>
    /// 验证运行中任务失败后状态变为失败并记录错误。
    /// </summary>
    [Fact]
    public void Fail_WhenJobIsRunning_ChangesStatusToFailedAndRecordsError()
    {
        // Arrange
        CloudDriveJob job = CreateUploadJob();
        job.Start();
        job.ClearDomainEvents();
        const string errorMessage = "upload failed";

        // Act
        job.Fail(errorMessage);

        // Assert
        Assert.Equal(CloudDriveJobStatus.Failed, job.Status);
        Assert.Equal(errorMessage, job.ErrorMessage);
        Assert.NotNull(job.EndTime);
        CloudDriveJobStatusChanged domainEvent = Assert.IsType<CloudDriveJobStatusChanged>(Assert.Single(job.DomainEvents));
        Assert.Equal(CloudDriveJobStatus.Running, domainEvent.PreviousStatus);
        Assert.Equal(CloudDriveJobStatus.Failed, domainEvent.NewStatus);
    }

    /// <summary>
    /// 验证运行中任务取消后状态变为已取消。
    /// </summary>
    [Fact]
    public void Cancel_WhenJobIsRunning_ChangesStatusToCancelled()
    {
        // Arrange
        CloudDriveJob job = CreateUploadJob();
        job.Start();
        job.ClearDomainEvents();

        // Act
        job.Cancel();

        // Assert
        Assert.Equal(CloudDriveJobStatus.Cancelled, job.Status);
        Assert.NotNull(job.EndTime);
        CloudDriveJobStatusChanged domainEvent = Assert.IsType<CloudDriveJobStatusChanged>(Assert.Single(job.DomainEvents));
        Assert.Equal(CloudDriveJobStatus.Running, domainEvent.PreviousStatus);
        Assert.Equal(CloudDriveJobStatus.Cancelled, domainEvent.NewStatus);
    }

    private static CloudDriveJob CreateUploadJob()
    {
        return CloudDriveJob.Create(
            TestUserId,
            TestCloudAccountUid,
            CloudDriveJobType.Upload,
            resourceId: "file-001",
            payloadJson: "{}");
    }
}
