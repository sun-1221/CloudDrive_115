using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveUserInfoResponse
{
    [JsonPropertyName("user_id")]
    public string? UserId { get; init; }

    [JsonPropertyName("user_name")]
    public string? UserName { get; init; }

    [JsonPropertyName("display_uid")]
    public string? DisplayUid { get; init; }

    [JsonPropertyName("user_face")]
    public string? UserFace { get; init; }

    [JsonPropertyName("user_face_s")]
    public string? UserFaceSmall { get; init; }

    [JsonPropertyName("user_face_m")]
    public string? UserFaceMedium { get; init; }

    [JsonPropertyName("user_face_l")]
    public string? UserFaceLarge { get; init; }

    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("is_vip")]
    public int? IsVip { get; init; }

    [JsonPropertyName("pub_mobile_mask")]
    public string? MaskedPhone { get; init; }

    [JsonPropertyName("pub_email_mask")]
    public string? MaskedEmail { get; init; }
}
