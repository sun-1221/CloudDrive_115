namespace Application.CloudDrive.Dtos;

/// <summary>
/// 获取文件列表输入。
/// </summary>
public class GetFileListInput
{
    /// <summary>
    /// 获取或设置目录 ID，未指定时表示根目录。
    /// </summary>
    public string? DirectoryId { get; set; }

    /// <summary>
    /// 获取或设置跳过数量。
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// 获取或设置最大返回数量。
    /// </summary>
    public int Limit { get; set; } = 50;

    /// <summary>
    /// 获取或设置文件类型筛选。
    /// </summary>
    public int? Type { get; set; }

    /// <summary>
    /// 获取或设置文件后缀筛选。
    /// </summary>
    public string? Suffix { get; set; }

    /// <summary>
    /// 获取或设置排序方向。
    /// </summary>
    public int? Asc { get; set; }

    /// <summary>
    /// 获取或设置排序字段。
    /// </summary>
    public string? OrderBy { get; set; }

    /// <summary>
    /// 获取或设置官方 custom_order。
    /// </summary>
    public int? CustomOrder { get; set; }

    /// <summary>
    /// 获取或设置官方 stdir。
    /// </summary>
    public int? Stdir { get; set; }

    /// <summary>
    /// 获取或设置星标筛选。
    /// </summary>
    public int? Star { get; set; }

    /// <summary>
    /// 获取或设置官方 cur。
    /// </summary>
    public int? Cur { get; set; }

    /// <summary>
    /// 获取或设置是否显示目录。
    /// </summary>
    public int? ShowDir { get; set; }

    /// <summary>
    /// 获取或设置自然排序。
    /// </summary>
    public int? NatSort { get; set; }

    /// <summary>
    /// 获取或设置 snap 参数。
    /// </summary>
    public int? Snap { get; set; }

    /// <summary>
    /// 获取或设置是否统计文件夹数。
    /// </summary>
    public int? CountFolders { get; set; }

    /// <summary>
    /// 获取或设置是否记录打开时间。
    /// </summary>
    public int? RecordOpenTime { get; set; }

    /// <summary>
    /// 获取或设置 code 参数（待采集含义）。
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 获取或设置 scid 参数（待采集含义）。
    /// </summary>
    public string? Scid { get; set; }

    /// <summary>
    /// 获取或设置 source 参数。
    /// </summary>
    public string? Source { get; set; }
}
