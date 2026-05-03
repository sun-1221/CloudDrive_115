namespace Application.Abstractions.CloudDrive;

/// <summary>
/// 115 网盘 Cookie 负载加密 / 签名密钥保护抽象。
/// </summary>
/// <remarks>
/// 定义在 Application 层，基础设施层默认使用 ASP.NET Core Data Protection 实现。
/// 同一部署拓扑下必须持久化并共享 key ring，以保证服务重启后仍可解密既有 Cookie 与签名密钥。
/// </remarks>
public interface ICloudDriveSecretProtector
{
    /// <summary>
    /// 对明文进行加密，返回受保护字符串。
    /// </summary>
    /// <param name="keyName">key ring 名称，用于对不同用途维护独立密钥环。</param>
    /// <param name="plaintext">要加密的明文，不可为空。</param>
    /// <returns>加密后的受保护字符串。</returns>
    string Protect(string keyName, string plaintext);

    /// <summary>
    /// 对受保护字符串进行解密，还原明文。
    /// </summary>
    /// <param name="keyName">key ring 名称，须与加密时使用的名称一致。</param>
    /// <param name="ciphertext">先前由 <see cref="Protect"/> 产生的受保护字符串。</param>
    /// <param name="keyVersion">可选密钥版本号，用于 key ring 轮换时的向后兼容解密。</param>
    /// <returns>解密后的明文。</returns>
    string Unprotect(string keyName, string ciphertext, int? keyVersion = null);
}
