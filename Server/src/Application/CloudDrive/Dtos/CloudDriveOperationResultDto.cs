namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 网盘操作结果。
/// </summary>
public class CloudDriveOperationResultDto
{
    /// <summary>
    /// 获取或设置操作是否已受理为后台任务。
    /// </summary>
    public bool Accepted { get; set; }

    /// <summary>
    /// 获取或设置后台任务标识。
    /// </summary>
    public Guid? JobId { get; set; }

    /// <summary>
    /// 获取或设置结果或受理说明。
    /// </summary>
    public string? Message { get; set; }
}
