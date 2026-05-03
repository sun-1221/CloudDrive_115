namespace Application.CloudDrive.Dtos;

/// <summary>
/// 获取文件预览输入。
/// </summary>
public class GetPreviewInput : CloudDriveAccountScopedInput
{
    /// <summary>
    /// 获取或设置文件 ID。
    /// </summary>
    public string FileId { get; set; } = string.Empty;
}
