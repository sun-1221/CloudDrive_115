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

    /// <summary>获取或设置可提现金额文本。</summary>
    public string? CanWithdraw { get; set; }

    /// <summary>获取或设置不可提现金额文本。</summary>
    public string? CantWithdraw { get; set; }

    /// <summary>获取或设置明细金额。</summary>
    public double? Detail { get; set; }

    /// <summary>获取或设置活动金额。</summary>
    public double? FeedbackActMoney { get; set; }

    /// <summary>获取或设置余额文本。</summary>
    public string? Money { get; set; }
}
