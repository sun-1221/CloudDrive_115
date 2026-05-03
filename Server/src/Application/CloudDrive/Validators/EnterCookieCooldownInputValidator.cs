using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// Cookie 冷却输入验证器。
/// </summary>
public sealed class EnterCookieCooldownInputValidator : AbstractValidator<EnterCookieCooldownInput>
{
    /// <summary>
    /// 初始化 <see cref="EnterCookieCooldownInputValidator"/> 类的新实例。
    /// </summary>
    public EnterCookieCooldownInputValidator()
    {
        this.RequireCloudAccount();
        RuleFor(input => input.Reason).NotEmptyText("冷却原因不能为空。").MaximumLength(256);
        RuleFor(input => input.CooldownSeconds).GreaterThan(0).When(input => input.CooldownSeconds.HasValue);
    }
}
