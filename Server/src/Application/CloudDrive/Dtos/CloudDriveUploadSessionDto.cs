namespace Application.CloudDrive.Dtos;

/// <summary>
/// 115 上传初始化会话。
/// </summary>
public class CloudDriveUploadSessionDto
{
    /// <summary>
    /// 获取或设置官方上传任务标识或续传标识。
    /// </summary>
    public string? PickCode { get; set; }

    /// <summary>
    /// 获取或设置上传初始化状态，1 表示非秒传，2 表示秒传。
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 获取或设置上传目标。
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// 获取或设置 OSS bucket。
    /// </summary>
    public string? Bucket { get; set; }

    /// <summary>
    /// 获取或设置 OSS object key。
    /// </summary>
    public string? ObjectKey { get; set; }

    /// <summary>
    /// 获取或设置 OSS callback。
    /// </summary>
    public string? Callback { get; set; }

    /// <summary>
    /// 获取或设置 OSS callback 变量。
    /// </summary>
    public string? CallbackVar { get; set; }

    /// <summary>
    /// 获取或设置上传端点。
    /// </summary>
    public string? UploadEndpoint { get; set; }

    /// <summary>
    /// 获取或设置直传表单字段。
    /// </summary>
    public IDictionary<string, string>? FormFields { get; set; }

    /// <summary>
    /// 获取或设置秒传成功时返回的文件信息。
    /// </summary>
    public CloudDriveUploadedFileDto? File { get; set; }

    /// <summary>
    /// 获取或设置二次认证 sign_key。
    /// </summary>
    public string? SignKey { get; set; }

    /// <summary>
    /// 获取或设置二次认证 sign_check。
    /// </summary>
    public string? SignCheck { get; set; }

    /// <summary>
    /// 获取或设置凭证过期时间。
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
