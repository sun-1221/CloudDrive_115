namespace Infrastructure.Http;

/// <summary>
/// 115 网页端各域名分组使用的命名 HTTP 客户端名称。
/// </summary>
internal static class CloudDriveHttpClientNames
{
    /// <summary>
    /// webapi.115.com 对应的命名 HTTP 客户端名称。
    /// </summary>
    public const string Web = "Cloud115Web";

    /// <summary>
    /// my.115.com 对应的命名 HTTP 客户端名称。
    /// </summary>
    public const string My = "Cloud115My";

    /// <summary>
    /// vip.115.com 对应的命名 HTTP 客户端名称。
    /// </summary>
    public const string Vip = "Cloud115Vip";

    /// <summary>
    /// pay.115.com 对应的命名 HTTP 客户端名称。
    /// </summary>
    public const string Pay = "Cloud115Pay";

    /// <summary>
    /// clouddownload.115.com/web 对应的命名 HTTP 客户端名称。
    /// </summary>
    public const string Offline = "Cloud115Offline";

    /// <summary>
    /// uplb.115.com 上传初始化接口对应的命名 HTTP 客户端名称。
    /// </summary>
    public const string Upload = "Cloud115Upload";

    /// <summary>
    /// OSS 直传接口对应的命名 HTTP 客户端名称。
    /// </summary>
    public const string UploadDirect = "Cloud115UploadDirect";
}
