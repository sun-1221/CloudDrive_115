using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 获取任务列表输入验证器。
/// </summary>
public sealed class GetJobListInputValidator : AbstractValidator<GetJobListInput>
{
    /// <summary>
    /// 初始化 <see cref="GetJobListInputValidator"/> 类的新实例。
    /// </summary>
    public GetJobListInputValidator()
    {
        RuleFor(input => input.SkipCount).GreaterThanOrEqualTo(0);
        RuleFor(input => input.MaxResultCount).InclusiveBetween(1, CloudDriveValidationRules.MaxDefaultListLimit);
    }
}
