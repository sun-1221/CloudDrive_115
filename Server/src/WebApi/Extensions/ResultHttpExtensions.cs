using System.Net;

using Domain.Results;

using Microsoft.AspNetCore.Http;
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
            : BuildFailureProblem(result.Error, failureStatusCode);
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
            : BuildFailureProblem(result.Error, failureStatusCode);
    }

    private static IResult BuildFailureProblem(Error error, int statusCode) => Results.Problem(
        detail: error.Description,
        statusCode: statusCode,
        title: error.Code,
        type: null,
        instance: null,
        extensions: new Dictionary<string, object?>
        {
            ["errorCode"] = error.Code,
        });

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
