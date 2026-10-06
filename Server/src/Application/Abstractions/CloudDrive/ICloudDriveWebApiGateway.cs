namespace Application.Abstractions.CloudDrive;

/// <summary>
/// 115 API 统一网关抽象。覆盖 webapi.115.com、my.115.com、vip.115.com、pay.115.com、
/// clouddownload.115.com/web、uplb.115.com 与 OSS 直传等多域名分组。
/// </summary>
/// <remarks>
/// 定义在 Application 层，基础设施层由 <c>CloudDriveApiClient</c> 实现。
/// 网关内部从 <c>CookieSessionManager</c> 获取当前 Cookie 并注入请求头。
/// </remarks>
public interface ICloudDriveWebApiGateway
{
    /// <summary>
    /// 发送网页端业务 API 请求（默认 webapi.115.com）。
    /// </summary>
    /// <returns>返回反序列化后的网页端 API 响应。</returns>
    Task<T> SendWebApiRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        HttpMethod method,
        string endpoint,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送用户中心类 API 请求（my.115.com / vip.115.com / pay.115.com）。
    /// <paramref name="context"/> 的 <see cref="CloudDriveApiContext.TargetDomain"/> 指向具体域名。
    /// </summary>
    /// <returns>返回反序列化后的用户中心 API 响应。</returns>
    Task<T> SendUserCenterRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        HttpMethod method,
        string endpoint,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送云下载 API 请求（clouddownload.115.com/web/）。
    /// 自动从 <see cref="ICookieSignatureStrategy"/> 生成 sign + time + uid 签名并合并到请求体。
    /// </summary>
    /// <returns>返回反序列化后的云下载 API 响应。</returns>
    Task<T> SendOfflineApiRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        HttpMethod method,
        string endpoint,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传初始化（POST uplb.115.com/3.0/sampleinitupload.php）。
    /// 返回秒传结果或 OSS 直传凭证。
    /// </summary>
    /// <returns>返回反序列化后的上传初始化响应。</returns>
    Task<T> SendUploadInitRequestAsync<T>(
        Guid userId,
        string cloudAccountUid,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// OSS 直传（PostObject 方式，不需 Cookie 注入）。
    /// </summary>
    /// <returns>返回反序列化后的 OSS 直传响应。</returns>
    Task<T> UploadFileAsync<T>(
        Guid userId,
        string cloudAccountUid,
        string uploadUrl,
        Stream fileStream,
        string fileName,
        IDictionary<string, string>? formFields = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 统一请求上下文：按 115 网页端的 query string / urlencoded form / JSON / multipart 四类构造方式组合。
/// </summary>
public sealed class CloudDriveApiContext
{
    /// <summary>
    /// 多域名聚合时显式指定目标域名。
    /// </summary>
    public string? TargetDomain { get; init; }

    /// <summary>
    /// URL 查询参数。
    /// </summary>
    public IDictionary<string, string>? QueryParameters { get; init; }

    /// <summary>
    /// application/x-www-form-urlencoded 表单字段。
    /// </summary>
    public IDictionary<string, string>? FormFields { get; init; }

    /// <summary>
    /// application/json 请求体。
    /// </summary>
    public object? JsonBody { get; init; }

    /// <summary>
    /// 限流桶场景分类。
    /// </summary>
    public string? Category { get; init; }
}
