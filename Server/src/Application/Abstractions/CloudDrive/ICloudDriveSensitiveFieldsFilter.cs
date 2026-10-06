namespace Application.Abstractions.CloudDrive;

/// <summary>
/// 115 网盘敏感字段脱敏过滤器抽象。用于日志、审计和 ProblemDetails 输出前的统一过滤。
/// </summary>
public interface ICloudDriveSensitiveFieldsFilter
{
    /// <summary>
    /// 对文本中的 Cookie、UID、CID、USSID、签名和密钥等敏感内容执行脱敏。
    /// </summary>
    /// <returns>返回当前操作的处理结果。</returns>
    string Redact(string? value);

    /// <summary>
    /// 对结构化字段字典执行脱敏。
    /// </summary>
    /// <returns>返回当前操作的处理结果。</returns>
    IReadOnlyDictionary<string, string?> RedactFields(IReadOnlyDictionary<string, string?> fields);
}
