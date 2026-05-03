namespace Application.Abstractions.CloudDrive;

/// <summary>
/// 云下载签名策略抽象。为 115 网页端云下载接口（115.com/web/lixian/）
/// 生成 sign + time + uid 签名三元组。
/// </summary>
/// <remarks>
/// 签名密钥由运维通过受保护配置注入，经 <see cref="ICloudDriveSecretProtector"/> 加密存储。
/// 默认注册 <c>NotImplementedCookieSignatureStrategy</c>，调用时返回明确的
/// <c>offline_signature_not_available</c> 错误。
/// </remarks>
public interface ICookieSignatureStrategy
{
    /// <summary>
    /// 生成云下载请求的签名三元组。
    /// </summary>
    /// <param name="userId">系统用户 ID。</param>
    /// <param name="cloudAccountUid">115 账号唯一标识。</param>
    /// <param name="requestBody">请求体内容。</param>
    CookieSignature Generate(Guid userId, string cloudAccountUid, string requestBody);
}

/// <summary>
/// 云下载签名结果（值对象）。
/// </summary>
/// <param name="Sign">签名字符串。</param>
/// <param name="Time">签名时间戳。</param>
/// <param name="Uid">115 账号 UID。</param>
public readonly record struct CookieSignature(string Sign, long Time, string Uid);
