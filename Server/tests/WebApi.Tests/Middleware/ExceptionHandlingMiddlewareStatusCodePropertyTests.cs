using System.Net;
using System.Text.Json;

using FluentValidation;
using FluentValidation.Results;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

using Microsoft.AspNetCore.Http;

using WebApi.Extensions;
using WebApi.Middleware;
using WebApi.Tests.Support;

using DomainError = Domain.Results.Error;
using DomainResult = Domain.Results.Result;

namespace WebApi.Tests.Middleware;

/// <summary>
/// Property 10：异常中间件 / WebApi 适配层状态码映射。
/// </summary>
/// <remarks>
/// 对应 Property 10（design.md §Property 10）与需求 R10.1–R10.4、R10.6。
/// </remarks>
public sealed class ExceptionHandlingMiddlewareStatusCodePropertyTests
{
    /// <summary>
    /// 任何被中间件处理的异常都应产生 <c>application/problem+json</c> 响应。
    /// </summary>
    [Property(Arbitrary = [typeof(NonEmptyStringArb)])]
    public async Task Any_exception_produces_problem_json_response(NonEmptyString message)
    {
        // Arrange（准备）

        // Act（执行）
        DefaultHttpContext context = await InvokeMiddlewareAsync(new InvalidOperationException(message.Get));

        // Assert（断言）
        Assert.StartsWith("application/problem+json", context.Response.ContentType, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// FluentValidation 抛出的 <see cref="ValidationException"/> 必定映射为 400，且响应中包含 errors 字段。
    /// </summary>
    [Property(Arbitrary = [typeof(NonEmptyStringArb)])]
    public async Task ValidationException_maps_to_400_with_errors(NonEmptyString property, NonEmptyString message)
    {
        // Arrange（准备）
        var failure = new ValidationFailure(property.Get, message.Get);
        var exception = new ValidationException([failure]);

        // Act（执行）
        DefaultHttpContext context = await InvokeMiddlewareAsync(exception);

        // Assert（断言）
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);

        using JsonDocument document = await ReadResponseBodyAsync(context);
        Assert.True(document.RootElement.TryGetProperty("errors", out JsonElement errors));
        Assert.True(errors.TryGetProperty(property.Get, out JsonElement fieldErrors));
        Assert.Contains(message.Get, fieldErrors.EnumerateArray().Select(e => e.GetString()));
    }

    /// <summary>
    /// <see cref="Domain.Exceptions.DomainException"/> 派生类必定映射为其 <c>StatusCode</c>，限定为非 2xx 典型业务状态码。
    /// </summary>
    [Property(Arbitrary = [typeof(DomainStatusCodeArb), typeof(NonEmptyStringArb)])]
    public async Task DomainException_maps_to_its_status_code(DomainStatusCode code, NonEmptyString message)
    {
        // Arrange（准备）
        var exception = new TestDomainException(code.Value, message.Get);

        // Act（执行）
        DefaultHttpContext context = await InvokeMiddlewareAsync(exception);

        // Assert（断言）
        Assert.Equal(code.Value, context.Response.StatusCode);
    }

    /// <summary>
    /// 未知异常（非 <see cref="ValidationException"/>、非 <see cref="Domain.Exceptions.DomainException"/>）
    /// 均映射为 500，并隐藏内部细节（非开发环境下 detail 不暴露异常类型完整名）。
    /// </summary>
    [Property(Arbitrary = [typeof(NonEmptyStringArb)])]
    public async Task Unknown_exception_maps_to_500_and_hides_internals(NonEmptyString secret)
    {
        // Arrange（准备）
        var exception = new InvalidOperationException(secret.Get);

        // Act（执行）
        DefaultHttpContext context = await InvokeMiddlewareAsync(exception, isDevelopment: false);

        // Assert（断言）
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);

        using JsonDocument document = await ReadResponseBodyAsync(context);
        Assert.True(document.RootElement.TryGetProperty("detail", out JsonElement detail));
        string? detailText = detail.GetString();

        // 非开发环境下 detail 应为固定提示，隐藏内部细节（如异常类型、堆栈、消息中的敏感数据）
        Assert.Equal("服务器内部错误", detailText);
    }

    /// <summary>
    /// 需求 R10.6：<c>Result.Failure</c> 经 WebApi 适配层映射为非 2xx <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/>。
    /// </summary>
    [Property(Arbitrary = [typeof(NonSuccessStatusCodeArb), typeof(NonEmptyStringArb)])]
    public void Result_Failure_never_maps_to_2xx(NonSuccessStatusCode statusCode, NonEmptyString code, NonEmptyString description)
    {
        // Arrange（准备）
        DomainResult failure = DomainResult.Failure(new DomainError(code.Get, description.Get));

        // Act（执行）
        IResult httpResult = failure.ToHttpResult(statusCode.Value);

        // Assert（断言）
        var problem = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>(httpResult);
        int actualStatus = problem.StatusCode;
        Assert.False(actualStatus is >= 200 and < 300, $"失败响应状态码不应为 2xx，实际 = {actualStatus}");
    }

    /// <summary>
    /// 适配扩展在传入 2xx 状态码时必须抛异常，防止以 200 返回失败响应。
    /// </summary>
    [Property(Arbitrary = [typeof(SuccessStatusCodeArb), typeof(NonEmptyStringArb)])]
    public void ToHttpResult_rejects_2xx_failure_status_code(SuccessStatusCode statusCode, NonEmptyString code, NonEmptyString description)
    {
        // Arrange（准备）
        DomainResult failure = DomainResult.Failure(new DomainError(code.Get, description.Get));

        // Act（执行）
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = failure.ToHttpResult(statusCode.Value);
        });

        // Assert（断言）
    }

    private static async Task<DefaultHttpContext> InvokeMiddlewareAsync(Exception exception, bool isDevelopment = false)
    {
        var logger = new FakeLogger<ExceptionHandlingMiddleware>();
        IHostEnvironmentStub env = new(isDevelopment);
        RequestDelegate next = _ => throw exception;

        var middleware = new ExceptionHandlingMiddleware(next, logger, env.Environment);

        var context = new DefaultHttpContext();
        context.Request.Path = "/test/path";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        return context;
    }

    private static async Task<JsonDocument> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }

    private readonly struct IHostEnvironmentStub(bool isDevelopment)
    {
        public FakeHostEnvironment Environment { get; } = new(isDevelopment
            ? Microsoft.Extensions.Hosting.Environments.Development
            : Microsoft.Extensions.Hosting.Environments.Production);
    }
}

/// <summary>
/// FsCheck：非空字符串生成器。
/// </summary>
public static class NonEmptyStringArb
{
    /// <summary>非空字符串。</summary>
    public static Arbitrary<NonEmptyString> NonEmpty()
    {
        Gen<NonEmptyString> gen = ArbMap.Default.ArbFor<NonEmptyString>().Generator;
        return Arb.From(gen);
    }
}

/// <summary>
/// FsCheck：业务状态码（401/403/404/409/429/502/503）。
/// </summary>
public sealed record DomainStatusCode(int Value);

/// <summary>FsCheck 仲裁器容器。</summary>
public static class DomainStatusCodeArb
{
    /// <summary>生成典型业务状态码。</summary>
    public static Arbitrary<DomainStatusCode> Codes() =>
        Arb.From(Gen.Elements(400, 401, 403, 404, 409, 429, 502, 503).Select(c => new DomainStatusCode(c)));
}

/// <summary>
/// FsCheck：非 2xx HTTP 状态码。
/// </summary>
public sealed record NonSuccessStatusCode(int Value);

/// <summary>FsCheck 仲裁器容器。</summary>
public static class NonSuccessStatusCodeArb
{
    /// <summary>生成非 2xx 状态码。</summary>
    public static Arbitrary<NonSuccessStatusCode> Codes() =>
        Arb.From(Gen.Elements(400, 401, 403, 404, 409, 429, 500, 502, 503).Select(c => new NonSuccessStatusCode(c)));
}

/// <summary>
/// FsCheck：2xx HTTP 状态码（用于反例场景）。
/// </summary>
public sealed record SuccessStatusCode(int Value);

/// <summary>FsCheck 仲裁器容器。</summary>
public static class SuccessStatusCodeArb
{
    /// <summary>生成 2xx 状态码。</summary>
    public static Arbitrary<SuccessStatusCode> Codes() =>
        Arb.From(Gen.Elements(200, 201, 202, 204).Select(c => new SuccessStatusCode(c)));
}
