using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 115 用户信息响应模型。
/// </summary>
internal sealed class CloudDriveUserInfoResponse
{
    /// <summary>
    /// 获取或设置用户标识。
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; init; }

    /// <summary>
    /// 获取或设置用户名。
    /// </summary>
    [JsonPropertyName("user_name")]
    public string? UserName { get; init; }

    /// <summary>
    /// 获取或设置展示 UID。
    /// </summary>
    [JsonPropertyName("display_uid")]
    public string? DisplayUid { get; init; }

    /// <summary>
    /// 获取或设置用户头像地址。
    /// </summary>
    [JsonPropertyName("user_face")]
    public string? UserFace { get; init; }

    /// <summary>
    /// 获取或设置小尺寸用户头像地址。
    /// </summary>
    [JsonPropertyName("user_face_s")]
    public string? UserFaceSmall { get; init; }

    /// <summary>
    /// 获取或设置中尺寸用户头像地址。
    /// </summary>
    [JsonPropertyName("user_face_m")]
    public string? UserFaceMedium { get; init; }

    /// <summary>
    /// 获取或设置大尺寸用户头像地址。
    /// </summary>
    [JsonPropertyName("user_face_l")]
    public string? UserFaceLarge { get; init; }

    /// <summary>
    /// 获取或设置 VIP 状态。
    /// </summary>
    [JsonConverter(typeof(NullableStringOrNumberToIntJsonConverter))]
    [JsonPropertyName("is_vip")]
    public int? IsVip { get; init; }

    /// <summary>
    /// 获取或设置脱敏手机号。
    /// </summary>
    [JsonPropertyName("pub_mobile_mask")]
    public string? MaskedPhone { get; init; }

    /// <summary>
    /// 获取或设置脱敏邮箱。
    /// </summary>
    [JsonPropertyName("pub_email_mask")]
    public string? MaskedEmail { get; init; }
}
