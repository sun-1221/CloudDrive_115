using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 获取文件列表输入验证器。
/// </summary>
public sealed class GetFileListInputValidator : AbstractValidator<GetFileListInput>
{
    /// <summary>
    /// 初始化 <see cref="GetFileListInputValidator"/> 类的新实例。
    /// </summary>
    public GetFileListInputValidator()
    {
        this.RequireCloudAccount();
        RuleFor(input => input.Offset).GreaterThanOrEqualTo(0);
        RuleFor(input => input.Limit).InclusiveBetween(1, CloudDriveValidationRules.MaxFileListLimit);
        RuleFor(input => input.Type).Must(CloudDriveValidationRules.IsValidFileListType).WithMessage("Type 必须在 1-7 之间。");
        RuleFor(input => input.Asc).Must(CloudDriveValidationRules.IsValidBinaryFlag).WithMessage("Asc 必须为 0 或 1。");
        RuleFor(input => input.Star).Must(CloudDriveValidationRules.IsValidBinaryFlag).WithMessage("Star 必须为 0 或 1。");
        RuleFor(input => input.ShowDir).Must(CloudDriveValidationRules.IsValidBinaryFlag).WithMessage("ShowDir 必须为 0 或 1。");
    }
}
