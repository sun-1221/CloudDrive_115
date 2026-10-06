using Application.Abstractions;

using FluentValidation;

using Infrastructure.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.DependencyInjection;

/// <summary>
/// FluentValidation 注册扩展测试。
/// </summary>
public sealed class ValidationSetupTests
{
    /// <summary>
    /// 验证实现 IHasInput 的请求会自动获得输入 DTO 验证器适配。
    /// </summary>
    [Fact]
    public async Task AddValidators_WhenRequestImplementsIHasInput_RegistersRequestValidatorAdapter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddValidators(typeof(ValidationSetupTests).Assembly);
        using ServiceProvider provider = services.BuildServiceProvider();

        IValidator<TestRequest> validator = Assert.Single(
            provider.GetServices<IValidator<TestRequest>>());

        // Act
        FluentValidation.Results.ValidationResult result =
            await validator.ValidateAsync(new TestRequest(new TestInput(string.Empty)));

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == "Input.Name");
    }

    public sealed record TestRequest(TestInput Input) : IHasInput<TestInput>;

    public sealed record TestInput(string Name);

    public sealed class TestInputValidator : AbstractValidator<TestInput>
    {
        public TestInputValidator()
        {
            RuleFor(input => input.Name).NotEmpty();
        }
    }
}
