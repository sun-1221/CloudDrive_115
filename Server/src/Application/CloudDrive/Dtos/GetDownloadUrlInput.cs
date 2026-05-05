namespace Application.CloudDrive.Dtos;

/// <summary>
/// 获取下载地址输入。
/// </summary>
public class GetDownloadUrlInput
{
    /// <summary>
    /// 获取或设置文件提取码。
    /// </summary>
    public string PickCode { get; set; } = string.Empty;
}
