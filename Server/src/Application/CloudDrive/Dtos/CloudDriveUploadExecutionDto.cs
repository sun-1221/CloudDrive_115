namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 上传执行结果。
/// </summary>
public class CloudDriveUploadExecutionDto : CloudDriveOperationResultDto
{
    /// <summary>
    /// 获取或设置同步上传成功后的文件信息。
    /// </summary>
    public CloudDriveUploadedFileDto? File { get; set; }
}
