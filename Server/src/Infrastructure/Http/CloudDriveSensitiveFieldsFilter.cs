using System.Text.RegularExpressions;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive;

using Microsoft.Extensions.Options;

namespace Infrastructure.Http;

/// <summary>
/// 115 网盘日志敏感字段脱敏过滤器。
/// </summary>
internal sealed partial class CloudDriveSensitiveFieldsFilter(
    IOptions<CloudDriveAppConfig> configOptions) : ICloudDriveSensitiveFieldsFilter
{
    /// <summary>
    /// 获取需要整体脱敏的字段名称集合。
    /// </summary>
    private static readonly HashSet<string> SensitiveFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "cookie",
        "uid",
        "cid",
        "ussid",
        "seid",
        "sign",
        "signkey",
        "signature",
        "password",
        "encryptedcookiepayload",
        "extrafields",
        "pub_mobile",
        "pub_email",
        "alipay",
        "wechat",
        "weibo",
        "address",
        "homepage",
        "location",
        "location_show",
        "location_birth",
        "location_link",
        "birthday",
        "star",
        "calendar_type",
        "gender",
        "blood_type",
        "is_marry",
        "height",
        "weight",
        "education",
        "job",
        "salary",
        "like_celeb",
        "like_music",
        "like_animal",
        "like_book",
        "like_video",
        "interest",
    };

    /// <summary>
    /// 获取键值形式敏感字段的正则表达式文本。
    /// </summary>
    private const string KeyValueSensitiveRegex =
        @"(?i)([""']?\b(uid|cid|ussid|seid|sign|signkey|signature|password|encryptedcookiepayload|extrafields"
        + @"|pub_mobile|pub_email|alipay|wechat|weibo|address|homepage|location|location_show|location_birth"
        + @"|location_link|birthday|star|calendar_type|gender|blood_type|is_marry|height|weight|education"
        + @"|job|salary|like_celeb|like_music|like_animal|like_book|like_video|interest)\b[""']?\s*[:=]\s*[""']?)([^&;\s,}""']+)";

    /// <summary>
    /// 对单个字符串值执行敏感信息脱敏。
    /// </summary>
    /// <param name="value">可能包含敏感信息的原始值。</param>
    /// <returns>脱敏后的字符串。</returns>
    public string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // 先处理 Cookie 头，再处理通用 key=value，覆盖请求头、日志消息和异常文本三种形态。
        string redacted = CookiePattern().Replace(value, "$1=[REDACTED]");
        redacted = KeyValueSensitivePattern().Replace(redacted, "$1[REDACTED]");

        CloudDriveAppConfig config = configOptions.Value;

        // 密钥字段名允许配置，脱敏规则必须跟随配置，避免轮换命名后日志泄露真实值。
        redacted = RedactConfiguredKeyName(redacted, config.CookieEncryptionKeyName);
        redacted = RedactConfiguredKeyName(redacted, config.SignatureKeyName);

        return redacted;
    }

    /// <summary>
    /// 对字段集合执行敏感信息脱敏。
    /// </summary>
    /// <param name="fields">需要脱敏的字段集合。</param>
    /// <returns>脱敏后的字段集合。</returns>
    public IReadOnlyDictionary<string, string?> RedactFields(IReadOnlyDictionary<string, string?> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        var redacted = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        CloudDriveAppConfig config = configOptions.Value;

        foreach (KeyValuePair<string, string?> field in fields)
        {
            // 字段名命中敏感项时整值替换，非敏感字段仍扫描字段值里的嵌套 Cookie/sign。
            bool isSensitive = IsSensitiveFieldName(field.Key)
                || field.Key.Equals(config.CookieEncryptionKeyName, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(config.SignatureKeyName)
                    && field.Key.Equals(config.SignatureKeyName, StringComparison.OrdinalIgnoreCase));

            redacted[field.Key] = isSensitive ? "[REDACTED]" : Redact(field.Value);
        }

        return redacted;
    }

    /// <summary>
    /// 判断字段名称是否属于敏感字段。
    /// </summary>
    private static bool IsSensitiveFieldName(string fieldName)
    {
        return SensitiveFieldNames.Contains(fieldName)
            || fieldName.Contains("cookie", StringComparison.OrdinalIgnoreCase)
            || fieldName.Contains("ussid", StringComparison.OrdinalIgnoreCase)
            || fieldName.Contains("extraFields", StringComparison.OrdinalIgnoreCase)
            || fieldName.Contains("signkey", StringComparison.OrdinalIgnoreCase)
            || fieldName.Contains("encryptedcookiepayload", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 对配置中声明的密钥名称执行脱敏。
    /// </summary>
    private static string RedactConfiguredKeyName(string value, string? keyName)
    {
        if (string.IsNullOrWhiteSpace(keyName))
        {
            return value;
        }

        string escaped = Regex.Escape(keyName);

        // 配置项来自部署环境，必须转义后进入正则，避免特殊字符改变匹配语义。
        return Regex.Replace(
            value,
            $"({escaped}\\s*[:=]\\s*)([^&;\\s,}}]+)",
            "$1[REDACTED]",
            RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(100));
    }

    /// <summary>
    /// 匹配 Cookie 头中的 115 登录态字段。
    /// </summary>
    [GeneratedRegex(@"(?i)\b(UID|CID|USSID|SEID)\s*=\s*([^;\s]+)", RegexOptions.CultureInvariant)]
    private static partial Regex CookiePattern();

    /// <summary>
    /// 匹配键值形式的敏感字段。
    /// </summary>
    [GeneratedRegex(KeyValueSensitiveRegex, RegexOptions.CultureInvariant)]
    private static partial Regex KeyValueSensitivePattern();
}
