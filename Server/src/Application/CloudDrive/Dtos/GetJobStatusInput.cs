using Domain.Enums;

namespace Application.CloudDrive.Dtos;

/// <summary>
/// 获取后台任务状态输入。
/// </summary>
public class GetJobStatusInput
{
    /// <summary>
    /// 获取或设置任务 ID。
    /// </summary>
    public Guid JobId { get; set; }
}
