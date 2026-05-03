using Application.CloudDrive.Dtos;

using Domain.Entities;

namespace Application.CloudDrive.Mappings;

/// <summary>
/// 后台任务相关手动映射。
/// </summary>
public static class JobMapping
{
    /// <summary>
    /// 将后台任务聚合根映射为 DTO。
    /// </summary>
    public static CloudDriveJobDto ToDto(this CloudDriveJob job)
    {
        return new CloudDriveJobDto
        {
            Id = job.Id,
            JobType = job.JobType,
            Status = job.Status,
            ResourceId = job.ResourceId,
            Progress = job.Progress,
            ErrorMessage = job.ErrorMessage,
            StartTime = job.StartTime,
            EndTime = job.EndTime,
        };
    }
}
