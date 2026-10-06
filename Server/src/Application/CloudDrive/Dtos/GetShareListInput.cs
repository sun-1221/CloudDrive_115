namespace Application.CloudDrive.Dtos;

/// <summary>
/// 获取分享列表输入。
/// </summary>
public class GetShareListInput
{
    /// <summary>
    /// 获取或设置跳过数量。
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// 获取或设置最大返回数量。
    /// </summary>
    public int Limit { get; set; } = 30;
}
