using System.Net;
using System.Text;
using System.Text.Json;

using Application.Abstractions;
using Domain.Repositories;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
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
                options.MapInboundClaims = false;
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

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        return WriteProblemDetailsAsync(
                            context.HttpContext,
                            HttpStatusCode.Unauthorized,
                            "Unauthorized");
                    },
                    OnForbidden = context => WriteProblemDetailsAsync(
                        context.HttpContext,
                        HttpStatusCode.Forbidden,
                        "Forbidden"),
                    OnTokenValidated = async context =>
                    {
                        string? sid = context.Principal?.FindFirst("sid")?.Value;
                        if (!Guid.TryParse(sid, out Guid sessionId))
                        {
                            context.Fail("JWT 缺少有效的会话 ID。");
                            return;
                        }

                        IUserSessionRepository repository = context.HttpContext.RequestServices.GetRequiredService<IUserSessionRepository>();
                        if (await repository.GetActiveAsync(sessionId, context.HttpContext.RequestAborted) is null)
                        {
                            context.Fail("登录会话已撤销或过期。");
                        }
                        else
                        {
                            context.HttpContext.RequestServices.GetRequiredService<IUserSessionContextAccessor>().SetSession(sessionId);
                        }
                    },
                };
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// 将认证失败和授权失败响应统一写入 ProblemDetails，避免受保护端点绕过全局异常中间件时输出空响应。
    /// </summary>
    /// <param name="httpContext">当前 HTTP 请求上下文。</param>
    /// <param name="statusCode">需要写入响应的 HTTP 状态码。</param>
    /// <param name="title">ProblemDetails 的标题。</param>
    /// <returns>表示异步写入响应体的任务。</returns>
    private static async Task WriteProblemDetailsAsync(
        HttpContext httpContext,
        HttpStatusCode statusCode,
        string title)
    {
        if (httpContext.Response.HasStarted)
        {
            return;
        }

        int status = (int)statusCode;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Instance = httpContext.Request.Path.Value,
        };

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";

        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            problem,
            problem.GetType(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            httpContext.RequestAborted);
    }
}
