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
    /// 获取或设置目标目录 ID。
    /// </summary>
    public string? DirectoryId { get; set; }

    /// <summary>
    /// 获取或设置日期筛选。
    /// </summary>
    public string? Date { get; set; }

    /// <summary>
    /// 获取或设置文件类型筛选。
    /// </summary>
    public int? Type { get; set; }

    /// <summary>
    /// 获取或设置提取码筛选。
    /// </summary>
    public string? PickCode { get; set; }

    /// <summary>
    /// 获取或设置是否统计文件夹。
    /// </summary>
    public int? CountFolders { get; set; } = 1;

    /// <summary>
    /// 获取或设置来源筛选。
    /// </summary>
    public string? Source { get; set; }
}
