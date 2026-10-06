using System.Net;
using System.Text.Json;

using Application.Abstractions.CloudDrive;

using Domain.Exceptions;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

namespace WebApi.Middleware;

/// <summary>
/// 全局异常处理中间件。将管道中任意未处理异常映射为
/// <c>application/problem+json</c> 响应（R10.1–R10.5）。
/// </summary>
/// <remarks>
/// <para>
/// 映射规则（按优先级）：<br/>
/// 1. <see cref="ValidationException"/>（FluentValidation）→ 400，
///    <c>errors</c> 字段包含字段级错误详情；<br/>
/// 2. <see cref="DomainException"/> → <see cref="DomainException.StatusCode"/>；<br/>
/// 3. 其他异常 → 500，隐藏内部异常细节（非开发环境）。
/// </para>
/// <para>
/// 所有异常均会记录日志（请求路径 / 异常类型 / 异常消息）。
/// </para>
/// </remarks>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment,
    ICloudDriveSensitiveFieldsFilter? sensitiveFieldsFilter = null)
{
    private static readonly JsonSerializerOptions ProblemSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IHostEnvironment _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    private readonly ICloudDriveSensitiveFieldsFilter? _sensitiveFieldsFilter = sensitiveFieldsFilter;

    /// <summary>
    /// 中间件入口。捕获后续管道异常并写入 ProblemDetails 响应。
    /// </summary>
    /// <returns>表示异步中间件管道处理的 <see cref="Task"/>。</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _next(context);
        }
#pragma warning disable CA1031 // 全局异常处理中间件必须兜底捕获所有未处理异常
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogError(
                ex,
                "请求 {RequestPath} 处理失败：{ExceptionType} - {ExceptionMessage}",
                context.Request.Path.Value,
                ex.GetType().FullName,
                Redact(ex.Message));

            await WriteProblemResponseAsync(context, ex);
        }
    }

    private async Task WriteProblemResponseAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("响应已开始发送，无法写入 ProblemDetails 响应");
            return;
        }

        ProblemDetails problem = BuildProblemDetails(context, exception);

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await JsonSerializer.SerializeAsync(context.Response.Body, problem, problem.GetType(), ProblemSerializerOptions);
    }

    private ProblemDetails BuildProblemDetails(HttpContext context, Exception exception)
    {
        string instance = context.Request.Path.Value ?? string.Empty;

        switch (exception)
        {
            case ValidationException validation:
                return BuildValidationProblem(validation, instance);

            case UnauthorizedAccessException unauthorized:
                return new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Unauthorized,
                    Title = ResolveTitle((int)HttpStatusCode.Unauthorized),
                    Detail = Redact(unauthorized.Message),
                    Instance = instance,
                };

            case CloudDriveCookieInvalidatedException cloudDriveCookieInvalidated:
                return BuildDomainProblem(cloudDriveCookieInvalidated, instance, (int)HttpStatusCode.Unauthorized);

            case CloudDriveFileNotFoundException cloudDriveFileNotFound:
                return BuildDomainProblem(cloudDriveFileNotFound, instance, (int)HttpStatusCode.NotFound);

            case CloudDriveRateLimitException cloudDriveRateLimit:
                return BuildDomainProblem(cloudDriveRateLimit, instance, 429);

            case CloudDriveInvalidNameException cloudDriveInvalidName:
                return BuildDomainProblem(cloudDriveInvalidName, instance, (int)HttpStatusCode.BadRequest);

            case CloudDriveConfigurationException cloudDriveConfiguration:
                return BuildDomainProblem(cloudDriveConfiguration, instance, (int)HttpStatusCode.ServiceUnavailable);

            case CloudDriveApiNotCollectedException cloudDriveApiNotCollected:
                return BuildDomainProblem(cloudDriveApiNotCollected, instance, 501);

            case CloudDriveNetworkException cloudDriveNetwork:
                return BuildDomainProblem(cloudDriveNetwork, instance, (int)HttpStatusCode.BadGateway);

            case CloudDriveUploadException cloudDriveUpload:
                return BuildDomainProblem(cloudDriveUpload, instance, (int)HttpStatusCode.InternalServerError);

            case CloudDriveApiException cloudDriveApi:
                int cloudDriveApiStatusCode = string.Equals(
                    cloudDriveApi.ErrorCode,
                    "cloud_drive_api_not_collected",
                    StringComparison.Ordinal)
                    ? 501
                    : (int)HttpStatusCode.BadGateway;
                return BuildDomainProblem(
                    cloudDriveApi,
                    instance,
                    cloudDriveApiStatusCode);

            case DomainException domain:
                return BuildDomainProblem(domain, instance, domain.StatusCode);

            default:
                return new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "Internal Server Error",
                    Detail = _environment.IsDevelopment() ? Redact(exception.ToString()) : "服务器内部错误",
                    Instance = instance,
                };
        }
    }

    private ProblemDetails BuildDomainProblem(DomainException exception, string instance, int statusCode)
    {
        CloudDriveApiException? cloudDriveApi = exception as CloudDriveApiException;

        return new ProblemDetails
        {
            Status = statusCode,
            Title = ResolveTitle(statusCode),
            Detail = Redact(exception.Message),
            Instance = instance,
            Extensions =
            {
                ["errorCode"] = Redact(exception.ErrorCode),
                ["upstreamCode"] = Redact(cloudDriveApi?.UpstreamCode),
                ["upstreamMessage"] = Redact(cloudDriveApi?.UpstreamMessage),
            },
        };
    }

    private static ValidationProblemDetails BuildValidationProblem(ValidationException exception, string instance)
    {
        Dictionary<string, string[]> errors = exception.Errors
            .GroupBy(e => e.PropertyName ?? string.Empty, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

        return new ValidationProblemDetails(errors)
        {
            Status = (int)HttpStatusCode.BadRequest,
            Title = "Validation Failed",
            Detail = exception.Message,
            Instance = instance,
        };
    }

    private static string ResolveTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        429 => "Too Many Requests",
        501 => "Not Implemented",
        502 => "Bad Gateway",
        503 => "Service Unavailable",
        _ => "Error",
    };

    private string Redact(string? value)
    {
        return _sensitiveFieldsFilter?.Redact(value) ?? value ?? string.Empty;
    }
}
