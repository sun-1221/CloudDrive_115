using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveUploadInitResponse
{
    [JsonPropertyName("file_id")]
    public string? FileId { get; init; }

    [JsonPropertyName("file_name")]
    public string? FileName { get; init; }

    [JsonPropertyName("pick_code")]
    public string? PickCode { get; init; }

    [JsonPropertyName("sha1")]
    public string? Sha1 { get; init; }

    [JsonPropertyName("cid")]
    public string? DirectoryId { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("file_size")]
    public long? FileSize { get; init; }

    [JsonPropertyName("object")]
    public string? Object { get; init; }

    [JsonPropertyName("accessid")]
    public string? AccessId { get; init; }

    [JsonPropertyName("host")]
    public string? Host { get; init; }

    [JsonPropertyName("policy")]
    public string? Policy { get; init; }

    [JsonPropertyName("signature")]
    public string? Signature { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToLongJsonConverter))]
    [JsonPropertyName("expire")]
    public long? Expire { get; init; }

    [JsonPropertyName("callback")]
    public string? Callback { get; init; }

    [JsonIgnore]
    public bool IsInstantUpload => !string.IsNullOrWhiteSpace(FileId)
        && string.IsNullOrWhiteSpace(Object)
        && string.IsNullOrWhiteSpace(AccessId)
        && string.IsNullOrWhiteSpace(Host);

    [JsonIgnore]
    public bool HasOssCredential => !string.IsNullOrWhiteSpace(Object)
        && !string.IsNullOrWhiteSpace(AccessId)
        && !string.IsNullOrWhiteSpace(Host)
        && !string.IsNullOrWhiteSpace(Policy)
        && !string.IsNullOrWhiteSpace(Signature)
        && !string.IsNullOrWhiteSpace(Callback);
}
