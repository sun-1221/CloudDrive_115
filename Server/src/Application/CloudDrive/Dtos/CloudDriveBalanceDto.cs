namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 账户余额信息。
/// </summary>
public class CloudDriveBalanceDto
{
    /// <summary>
    /// 获取或设置余额数值。
    /// </summary>
    public decimal? Balance { get; set; }

    /// <summary>
    /// 获取或设置余额展示文本。
    /// </summary>
    public string? DisplayText { get; set; }
}
