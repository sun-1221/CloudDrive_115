using System.Net;

using Domain.Results;

using Microsoft.AspNetCore.Mvc;

namespace WebApi.Extensions;

/// <summary>
/// <see cref="Result"/> / <see cref="Result{T}"/> → <see cref="IResult"/> 的适配扩展。
/// 统一由 WebApi 适配层保证 <see cref="Result.IsFailure"/> 不会以 HTTP 200 响应（R10.6）。
/// </summary>
public static class ResultHttpExtensions
{
    /// <summary>
    /// 将 <see cref="Result"/> 映射为 <see cref="IResult"/>。
    /// 成功 → <c>204 NoContent</c>；失败 → <paramref name="failureStatusCode"/>（默认 400）的 <see cref="ProblemDetails"/>。
    /// </summary>
    /// <param name="result">操作结果。</param>
    /// <param name="failureStatusCode">失败时的 HTTP 状态码；必须为非 2xx。</param>
    /// <returns>可写入响应的 <see cref="IResult"/>。</returns>
    public static IResult ToHttpResult(this Result result, int failureStatusCode = (int)HttpStatusCode.BadRequest)
    {
        ArgumentNullException.ThrowIfNull(result);
        EnsureNonSuccessStatusCode(failureStatusCode);

        return result.IsSuccess
            ? Results.NoContent()
            : BuildFailureProblem(result.Error, ResolveFailureStatusCode(result.Error, failureStatusCode));
    }

    /// <summary>
    /// 将 <see cref="Result{T}"/> 映射为 <see cref="IResult"/>。
    /// 成功 → <c>200 OK</c> + 值；失败 → <paramref name="failureStatusCode"/>（默认 400）的 <see cref="ProblemDetails"/>。
    /// </summary>
    /// <typeparam name="T">成功值类型。</typeparam>
    /// <param name="result">带返回值的操作结果。</param>
    /// <param name="failureStatusCode">失败时的 HTTP 状态码；必须为非 2xx。</param>
    /// <returns>可写入响应的 <see cref="IResult"/>。</returns>
    public static IResult ToHttpResult<T>(this Result<T> result, int failureStatusCode = (int)HttpStatusCode.BadRequest)
    {
        ArgumentNullException.ThrowIfNull(result);
        EnsureNonSuccessStatusCode(failureStatusCode);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : BuildFailureProblem(result.Error, ResolveFailureStatusCode(result.Error, failureStatusCode));
    }

    private static IResult BuildFailureProblem(Error error, int statusCode) => Results.Problem(
        detail: error.Description,
        statusCode: statusCode,
        title: error.Code,
        type: $"https://httpstatuses.io/{statusCode}",
        extensions: new Dictionary<string, object?>
        {
            ["code"] = error.Code,
            ["upstreamCode"] = null,
            ["upstreamMessage"] = null,
        });

    /// <summary>
    /// 将 <see cref="Result{T}"/> 映射为 201 Created。
    /// </summary>
    /// <returns>返回当前操作的处理结果。</returns>
    public static IResult ToCreatedHttpResult<T>(this Result<T> result, string? location = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? Results.Created(location ?? string.Empty, result.Value)
            : BuildFailureProblem(result.Error, ResolveFailureStatusCode(result.Error));
    }

    /// <summary>
    /// 将可能代表后台受理的 <see cref="Result{T}"/> 映射为 200/202。
    /// </summary>
    /// <returns>返回当前操作的处理结果。</returns>
    public static IResult ToOperationHttpResult<T>(this Result<T> result, Func<T, bool> acceptedSelector)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(acceptedSelector);

        return result.IsFailure
            ? BuildFailureProblem(result.Error, ResolveFailureStatusCode(result.Error))
            : acceptedSelector(result.Value)
                ? Results.Accepted(value: result.Value)
                : Results.Ok(result.Value);
    }

    private static int ResolveFailureStatusCode(
        Error error,
        int fallbackStatusCode = (int)HttpStatusCode.BadRequest)
    {
        EnsureNonSuccessStatusCode(fallbackStatusCode);

        return error.Code switch
        {
            "invalid_credentials" or
            "session_unavailable" => (int)HttpStatusCode.Unauthorized,
            "user_name_taken" => (int)HttpStatusCode.Conflict,
            "cloud_drive_account_unavailable" => (int)HttpStatusCode.NotFound,
            "cloud_drive_cookie_invalidated" or
            "cookie_not_active" => (int)HttpStatusCode.Unauthorized,
            "cloud_drive_account_context_unavailable" or
            "cloud_drive_account_context_ambiguous" or
            "cloud_drive_configuration_unavailable" or
            "offline_signature_not_available" or
            "share_sample_unavailable" or
            "cloud_drive_api_not_collected" => 501,
            "cloud_drive_file_not_found" or
            "cloud_drive_job_not_found" => (int)HttpStatusCode.NotFound,
            "cloud_drive_rate_limited" => 429,
            "cloud_drive_upload_failed" or
            "cloud_drive_upload_oss_credentials_missing" => (int)HttpStatusCode.InternalServerError,
            "cloud_drive_api_error" or
            "unknown_error_shape" => (int)HttpStatusCode.BadGateway,
            _ => fallbackStatusCode,
        };
    }

    private static void EnsureNonSuccessStatusCode(int statusCode)
    {
        if (statusCode is >= 200 and < 300)
        {
            throw new ArgumentOutOfRangeException(
                nameof(statusCode),
                statusCode,
                "失败响应的状态码必须为非 2xx，禁止以 HTTP 200 表示失败。");
        }
    }
}
