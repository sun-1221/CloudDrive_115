using Application.Behaviors;

using FluentValidation;

using FsCheck.Xunit;

using MediatR;

namespace Application.Tests.Behaviors;

/// <summary>
/// 设计文档 Property 1 与 Property 2：ValidationBehavior 行为属性测试。
/// </summary>
public sealed class ValidationBehaviorPropertyTests
{
    /// <summary>
    /// Property 1：对任意请求，若存在已注册的 FluentValidation 验证器且输入不满足验证规则，
    /// 则 ValidationBehavior 应抛出 <see cref="ValidationException"/>，异常中应包含
    /// 所有验证失败的字段级错误信息，Handler 不应被执行。
    /// </summary>
    [Property]
    public async Task<bool> Handle_WithFailingValidators_ThrowsValidationExceptionAndSkipsHandler(
        int payload1,
        int payload2)
    {
        // Arrange
        string errorMessage1 = $"error-{payload1}";
        string errorMessage2 = $"error-{payload2}";
        bool handlerWasCalled = false;

        IValidator<TestRequest>[] validators =
        [
            new AlwaysFailValidator(errorMessage1),
            new AlwaysFailValidator(errorMessage2),
        ];
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(validators);
        var request = new TestRequest("x");

        // Act
        ValidationException ex = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                request,
                _ =>
                {
                    handlerWasCalled = true;
                    return Task.FromResult(new TestResponse());
                },
                CancellationToken.None));

        // Assert
        bool containsFirstError = ex.Errors.Any(e => e.ErrorMessage == errorMessage1);
        bool containsSecondError = ex.Errors.Any(e => e.ErrorMessage == errorMessage2);

        return !handlerWasCalled && containsFirstError && containsSecondError;
    }

    /// <summary>
    /// Property 2：对任意请求，若没有对应的 FluentValidation 验证器，
    /// ValidationBehavior 应直接将请求传递给下一个管道，不抛出异常。
    /// </summary>
    [Property]
    public async Task<bool> Handle_WithoutValidators_PassesRequestToNext(int payload)
    {
        // Arrange
        bool handlerWasCalled = false;
        var behavior = new ValidationBehavior<TestRequest, TestResponse>([]);
        var request = new TestRequest($"req-{payload}");
        var expected = new TestResponse();

        // Act
        TestResponse actual = await behavior.Handle(
            request,
            _ =>
            {
                handlerWasCalled = true;
                return Task.FromResult(expected);
            },
            CancellationToken.None);

        return handlerWasCalled && ReferenceEquals(actual, expected);
    }

    [Fact]
    public async Task Handle_WithAllPassingValidators_InvokesNextAndReturnsResponse()
    {
        // Arrange
        bool handlerWasCalled = false;
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(
            [new AlwaysPassValidator()]);
        var request = new TestRequest("ok");
        var expected = new TestResponse();

        // Act
        TestResponse actual = await behavior.Handle(
            request,
            _ =>
            {
                handlerWasCalled = true;
                return Task.FromResult(expected);
            },
            CancellationToken.None);

        // Assert
        Assert.True(handlerWasCalled);
        Assert.Same(expected, actual);
    }

    public sealed record TestRequest(string Value) : IRequest<TestResponse>;

    public sealed record TestResponse;

    private sealed class AlwaysFailValidator : AbstractValidator<TestRequest>
    {
        public AlwaysFailValidator(string errorMessage)
        {
            RuleFor(r => r.Value).Must(_ => false).WithMessage(errorMessage);
        }
    }

    private sealed class AlwaysPassValidator : AbstractValidator<TestRequest>
    {
        public AlwaysPassValidator()
        {
            RuleFor(r => r.Value).Must(_ => true);
        }
    }
}
