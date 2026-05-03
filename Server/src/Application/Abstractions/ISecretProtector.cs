namespace Application.Abstractions;

/// <summary>
/// 机密保护抽象。由 Infrastructure 层基于 ASP.NET Core Data Protection 实现，
/// 用于加密 / 解密 115 app_secret、第三方 API Key 等敏感字段（R11.5、R11.6）。
/// </summary>
public interface ISecretProtector
{
    /// <summary>
    /// 对明文进行加密，返回 Base64 格式的受保护字符串。
    /// </summary>
    /// <param name="plaintext">要加密的明文，不可为空。</param>
    ValueTask<string> ProtectAsync(string plaintext, CancellationToken cancellationToken = default);

    /// <summary>
    /// 对受保护字符串进行解密，还原明文。
    /// </summary>
    /// <param name="protectedText">先前由 <see cref="ProtectAsync"/> 产生的受保护字符串。</param>
    ValueTask<string> UnprotectAsync(string protectedText, CancellationToken cancellationToken = default);
}
