using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 获取回收站列表输入验证器。
/// </summary>
public sealed class GetRecycleBinListInputValidator : AbstractValidator<GetRecycleBinListInput>
{
    /// <summary>
    /// 初始化 <see cref="GetRecycleBinListInputValidator"/> 类的新实例。
    /// </summary>
    public GetRecycleBinListInputValidator()
    {
        this.RequireCloudAccount();
        RuleFor(input => input.Offset).GreaterThanOrEqualTo(0);
        RuleFor(input => input.Limit).InclusiveBetween(1, CloudDriveValidationRules.MaxRecycleBinLimit);
    }
}
