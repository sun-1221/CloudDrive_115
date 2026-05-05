using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 切换星标输入验证器。
/// </summary>
public sealed class ToggleStarInputValidator : AbstractValidator<ToggleStarInput>
{
    /// <summary>
    /// 初始化 <see cref="ToggleStarInputValidator"/> 类的新实例。
    /// </summary>
    public ToggleStarInputValidator()
    {
        RuleFor(input => input.FileId).NotEmptyText("FileId 不能为空。");
    }
}
