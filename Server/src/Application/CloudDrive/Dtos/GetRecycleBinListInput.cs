namespace Application.CloudDrive.Dtos;

/// <summary>
/// 获取回收站列表输入。
/// </summary>
public class GetRecycleBinListInput
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
