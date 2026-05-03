using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace WebApi.Extensions;

/// <summary>
/// JWT Bearer 认证注册扩展（R12.6）。业务模块可在 <c>Program.cs</c> 中按需调用，
/// 框架本身不强制启用认证。
/// </summary>
/// <remarks>
/// <para>
/// 配置节约定（默认 <c>"Jwt"</c>）：<br/>
/// - <c>Issuer</c>：签发方。<br/>
/// - <c>Audience</c>：受众。<br/>
/// - <c>SigningKey</c>：对称签名密钥（HS256）。<br/>
/// 任一字段为空时，<see cref="AddJwtAuthentication"/> 视为配置缺失并抛出
/// <see cref="InvalidOperationException"/>，避免启用了不安全的默认配置。
/// </para>
/// <para>
/// 本扩展仅注册认证服务；请求管道启用通过 <c>app.UseAuthentication().UseAuthorization()</c> 完成。
/// </para>
/// </remarks>
public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// 默认 JWT 配置节名称。
    /// </summary>
    public const string DefaultConfigSectionName = "Jwt";

    /// <summary>
    /// 注册 JWT Bearer 认证。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">应用配置。</param>
    /// <param name="sectionName">配置节名。</param>
    /// <returns>原始服务集合。</returns>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = DefaultConfigSectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        IConfigurationSection section = configuration.GetSection(sectionName);
        if (!section.Exists())
        {
            throw new InvalidOperationException($"未找到配置节 '{sectionName}'，无法启用 JWT 认证。");
        }

        string? issuer = section["Issuer"];
        string? audience = section["Audience"];
        string? signingKey = section["SigningKey"];

        if (string.IsNullOrWhiteSpace(issuer)
            || string.IsNullOrWhiteSpace(audience)
            || string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                $"配置节 '{sectionName}' 必须包含非空的 Issuer / Audience / SigningKey 字段。");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        services.AddAuthorization();

        return services;
    }
}
