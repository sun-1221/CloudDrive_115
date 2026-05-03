namespace Application.CloudDrive.Dtos;

/// <summary>
/// 搜索文件输入。
/// </summary>
public class SearchFileInput : CloudDriveAccountScopedInput
{
    /// <summary>
    /// 获取或设置搜索关键字。
    /// </summary>
    public string Keyword { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置跳过数量。
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// 获取或设置最大返回数量。
    /// </summary>
    public int Limit { get; set; } = 50;

    /// <summary>
    /// 获取或设置文件标签筛选。
    /// </summary>
    public string? FileLabel { get; set; }

    /// <summary>
    /// 获取或设置目标目录 ID。
    /// </summary>
    public string? DirectoryId { get; set; }

    /// <summary>
    /// 获取或设置开始日期。
    /// </summary>
    public string? GteDay { get; set; }

    /// <summary>
    /// 获取或设置结束日期。
    /// </summary>
    public string? LteDay { get; set; }

    /// <summary>
    /// 获取或设置文件或文件夹筛选。
    /// </summary>
    public int? Fc { get; set; }

    /// <summary>
    /// 获取或设置文件类型筛选。
    /// </summary>
    public int? Type { get; set; }

    /// <summary>
    /// 获取或设置文件后缀筛选。
    /// </summary>
    public string? Suffix { get; set; }
}
