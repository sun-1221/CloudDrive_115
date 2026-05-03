using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 搜索文件输入验证器。
/// </summary>
public sealed class SearchFileInputValidator : AbstractValidator<SearchFileInput>
{
    /// <summary>
    /// 初始化 <see cref="SearchFileInputValidator"/> 类的新实例。
    /// </summary>
    public SearchFileInputValidator()
    {
        this.RequireCloudAccount();
        RuleFor(input => input.Keyword).NotEmptyText("搜索关键字不能为空。");
        RuleFor(input => input.Offset).GreaterThanOrEqualTo(0);
        RuleFor(input => input.Limit).GreaterThan(0);
        RuleFor(input => input)
            .Must(input => input.Offset + input.Limit <= CloudDriveValidationRules.MaxSearchWindow)
            .WithMessage("Offset 与 Limit 之和不能超过 10000。");
        RuleFor(input => input.GteDay).Must(CloudDriveValidationRules.IsValidDate).WithMessage("GteDay 必须为 yyyy-MM-dd。");
        RuleFor(input => input.LteDay).Must(CloudDriveValidationRules.IsValidDate).WithMessage("LteDay 必须为 yyyy-MM-dd。");
        RuleFor(input => input.Fc).Must(CloudDriveValidationRules.IsValidSearchFc).WithMessage("Fc 必须为 1 或 2。");
        RuleFor(input => input.Type).Must(CloudDriveValidationRules.IsValidSearchFileType).WithMessage("Type 必须在 1-6 之间。");
    }
}
