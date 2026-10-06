using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 获取分享列表输入验证器。
/// </summary>
public sealed class GetShareListInputValidator : AbstractValidator<GetShareListInput>
{
    /// <summary>
    /// 初始化 <see cref="GetShareListInputValidator"/> 类的新实例。
    /// </summary>
    public GetShareListInputValidator()
    {
        RuleFor(input => input.Offset).GreaterThanOrEqualTo(0);
        RuleFor(input => input.Limit).InclusiveBetween(1, CloudDriveValidationRules.MaxDefaultListLimit);
    }
}
