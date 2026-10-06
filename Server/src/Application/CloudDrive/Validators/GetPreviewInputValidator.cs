using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 获取预览输入验证器。
/// </summary>
public sealed class GetPreviewInputValidator : AbstractValidator<GetPreviewInput>
{
    /// <summary>
    /// 初始化 <see cref="GetPreviewInputValidator"/> 类的新实例。
    /// </summary>
    public GetPreviewInputValidator()
    {
        RuleFor(input => input.PickCode).NotEmptyText("PickCode 不能为空。");
    }
}
