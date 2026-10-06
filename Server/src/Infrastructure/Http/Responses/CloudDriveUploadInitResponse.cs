using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 上传初始化响应模型。
/// </summary>
internal sealed class CloudDriveUploadInitResponse
{
    /// <summary>
    /// 获取或设置文件标识。
    /// </summary>
    [JsonPropertyName("file_id")]
    public string? FileId { get; init; }

    /// <summary>
    /// 获取或设置文件名称。
    /// </summary>
    [JsonPropertyName("file_name")]
    public string? FileName { get; init; }

    /// <summary>
    /// 获取或设置 115 文件提取码。
    /// </summary>
    [JsonPropertyName("pick_code")]
    public string? PickCode { get; init; }

    /// <summary>
    /// 获取或设置文件 SHA1。
    /// </summary>
    [JsonPropertyName("sha1")]
    public string? Sha1 { get; init; }

    /// <summary>
    /// 获取或设置目录标识。
    /// </summary>
    [JsonPropertyName("cid")]
    public string? DirectoryId { get; init; }

    /// <summary>
    /// 获取或设置文件大小。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("file_size")]
    public long? FileSize { get; init; }

    /// <summary>
    /// 获取或设置 OSS 对象键。
    /// </summary>
    [JsonPropertyName("object")]
    public string? Object { get; init; }

    /// <summary>
    /// 获取或设置 OSS AccessId。
    /// </summary>
    [JsonPropertyName("accessid")]
    public string? AccessId { get; init; }

    /// <summary>
    /// 获取或设置 OSS 上传主机地址。
    /// </summary>
    [JsonPropertyName("host")]
    public string? Host { get; init; }

    /// <summary>
    /// 获取或设置 OSS 上传策略。
    /// </summary>
    [JsonPropertyName("policy")]
    public string? Policy { get; init; }

    /// <summary>
    /// 获取或设置 OSS 上传签名。
    /// </summary>
    [JsonPropertyName("signature")]
    public string? Signature { get; init; }

    /// <summary>
    /// 获取或设置凭证过期时间戳。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("expire")]
    public long? Expire { get; init; }

    /// <summary>
    /// 获取或设置 OSS 回调参数。
    /// </summary>
    [JsonPropertyName("callback")]
    public string? Callback { get; init; }

    /// <summary>
    /// 获取是否为秒传结果。
    /// </summary>
    [JsonIgnore]
    public bool IsInstantUpload => !string.IsNullOrWhiteSpace(FileId)
        && string.IsNullOrWhiteSpace(Object)
        && string.IsNullOrWhiteSpace(AccessId)
        && string.IsNullOrWhiteSpace(Host);

    /// <summary>
    /// 获取是否包含 OSS 直传凭证。
    /// </summary>
    [JsonIgnore]
    public bool HasOssCredential => !string.IsNullOrWhiteSpace(Object)
        && !string.IsNullOrWhiteSpace(AccessId)
        && !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(Policy)
        && !string.IsNullOrWhiteSpace(Signature)
        && !string.IsNullOrWhiteSpace(Callback);
}
