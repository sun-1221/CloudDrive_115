using Domain.Enums;

namespace Application.CloudDrive.Dtos;

/// <summary>
/// 获取后台任务列表输入。
/// </summary>
public class GetJobListInput
{
    /// <summary>
    /// 获取或设置可选 115 账号 UID。
    /// </summary>
    public string? CloudAccountUid { get; set; }

    /// <summary>
    /// 获取或设置任务状态筛选。
    /// </summary>
    public CloudDriveJobStatus? Status { get; set; }

    /// <summary>
    /// 获取或设置跳过数量。
    /// </summary>
    public int SkipCount { get; set; }

    /// <summary>
    /// 获取或设置最大返回数量。
    /// </summary>
    public int MaxResultCount { get; set; } = 20;
}
