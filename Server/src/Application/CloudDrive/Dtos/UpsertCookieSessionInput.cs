namespace Application.CloudDrive.Dtos;

/// <summary>
/// 注入或更新 115 Cookie 会话输入。
/// </summary>
public class UpsertCookieSessionInput
{
    /// <summary>
    /// 获取或设置 115 账号 UID。
    /// </summary>
    public string CloudAccountUid { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Cookie UID。
    /// </summary>
    public string Uid { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Cookie CID。
    /// </summary>
    public string Cid { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置 Cookie USSID。
    /// </summary>
    public string Ussid { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置额外 Cookie 字段。
    /// </summary>
    public IDictionary<string, string>? ExtraFields { get; set; }
}
