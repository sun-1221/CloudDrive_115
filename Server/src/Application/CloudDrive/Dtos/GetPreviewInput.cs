namespace Application.CloudDrive.Dtos;

/// <summary>
/// 获取文件预览输入。
/// </summary>
public class GetPreviewInput
{
    /// <summary>
    /// 获取或设置文件提取码（pickCode）。
    /// </summary>
    public string PickCode { get; set; } = string.Empty;
}
