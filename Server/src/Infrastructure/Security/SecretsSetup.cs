using Application.Abstractions;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Security;

/// <summary>
/// 机密保护基础设施注册入口。
/// </summary>
/// <remarks>
/// 按设计 §8. WebApi 启动与认证的机密保护约定实现：<br/>
/// - 注册 <see cref="IDataProtectionProvider"/>；<br/>
/// - 设置统一的 <c>ApplicationName</c>，保证同一应用的多个实例可共享密钥；<br/>
/// - 支持通过 <c>keyStoragePath</c> 将密钥持久化到文件系统（R12.7）；<br/>
/// - 注册 <see cref="ISecretProtector"/> 的单例实现（R12.7）。
/// </remarks>
public static class SecretsSetup
{
    /// <summary>
    /// 默认应用名。用于 Data Protection 的 <c>ApplicationName</c> 与密钥隔离。
    /// </summary>
    public const string DefaultApplicationName = "CloudDrive";

    /// <summary>
    /// 注册机密保护相关服务。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="keyStoragePath">密钥持久化目录。传入 <see langword="null"/> 或空字符串时使用默认内存/临时存储。</param>
    /// <param name="applicationName">Data Protection 应用名。<see langword="null"/> 时使用 <see cref="DefaultApplicationName"/>。</param>
    /// <returns>原始服务集合。</returns>
    public static IServiceCollection AddSecretsProtection(
        this IServiceCollection services,
        string? keyStoragePath = null,
        string? applicationName = null)
    {
        IDataProtectionBuilder builder = services.AddDataProtection()
            .SetApplicationName(applicationName ?? DefaultApplicationName);

        if (!string.IsNullOrWhiteSpace(keyStoragePath))
        {
            // 多实例或重启后要能解密历史密文，配置路径时必须持久化 Data Protection key ring。
            Directory.CreateDirectory(keyStoragePath);
            builder.PersistKeysToFileSystem(new DirectoryInfo(keyStoragePath));
        }

        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();

        return services;
    }
}
