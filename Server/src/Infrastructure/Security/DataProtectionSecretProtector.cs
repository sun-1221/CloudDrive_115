using Application.Abstractions;

using Microsoft.AspNetCore.DataProtection;

namespace Infrastructure.Security;

/// <summary>
/// 基于 ASP.NET Core Data Protection 的 <see cref="ISecretProtector"/> 实现。
/// </summary>
/// <remarks>
/// 使用专用 purpose 字符串 <see cref="ProtectorPurpose"/> 隔离应用级密钥。
/// 密钥持久化位置由 <see cref="SecretsSetup.AddSecretsProtection"/> 配置。
/// </remarks>
internal sealed class DataProtectionSecretProtector : ISecretProtector
{
    /// <summary>
    /// Data Protection Provider 的 purpose 字符串，用于密钥隔离。
    /// </summary>
    internal const string ProtectorPurpose = "CloudDrive.Secrets.v1";

    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _protector = provider.CreateProtector(ProtectorPurpose);
    }

    /// <inheritdoc />
    public ValueTask<string> ProtectAsync(string plaintext, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(plaintext);
        cancellationToken.ThrowIfCancellationRequested();

        string result = _protector.Protect(plaintext);
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public ValueTask<string> UnprotectAsync(string protectedText, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(protectedText);
        cancellationToken.ThrowIfCancellationRequested();

        string result = _protector.Unprotect(protectedText);
        return ValueTask.FromResult(result);
    }
}
