using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// Common response envelope for 115 web APIs. Error fields intentionally keep all observed aliases;
/// <c>IErrorFieldAdapter</c> chooses the authoritative field per URL path.
/// </summary>
internal sealed class CloudDrive115WebResponse<T>
{
    [JsonConverter(typeof(BoolOrNumberJsonConverter))]
    [JsonPropertyName("state")]
    public bool State { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("Message")]
    public string? UpperMessage { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("err")]
    public string? Err { get; set; }

    [JsonConverter(typeof(StringOrNumberJsonConverter))]
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonConverter(typeof(StringOrNumberJsonConverter))]
    [JsonPropertyName("errNo")]
    public string? ErrNo { get; set; }

    [JsonConverter(typeof(StringOrNumberJsonConverter))]
    [JsonPropertyName("errno")]
    public string? Errno { get; set; }

    [JsonConverter(typeof(StringOrNumberJsonConverter))]
    [JsonPropertyName("errCode")]
    public string? ErrCode { get; set; }

    [JsonConverter(typeof(StringOrNumberJsonConverter))]
    [JsonPropertyName("ErrNo")]
    public string? UpperErrNo { get; set; }

    [JsonPropertyName("request")]
    public string? Request { get; set; }
}
