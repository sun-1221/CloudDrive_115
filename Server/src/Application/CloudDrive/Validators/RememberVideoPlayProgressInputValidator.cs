using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 记忆视频播放进度输入验证器。
/// </summary>
public sealed class RememberVideoPlayProgressInputValidator : AbstractValidator<RememberVideoPlayProgressInput>
{
    /// <summary>
    /// 初始化 <see cref="RememberVideoPlayProgressInputValidator"/> 类的新实例。
    /// </summary>
    public RememberVideoPlayProgressInputValidator()
    {
        RuleFor(input => input)
            .Must(input => !string.IsNullOrWhiteSpace(input.FileId) || !string.IsNullOrWhiteSpace(input.PickCode))
            .WithMessage("FileId 与 PickCode 必须至少提供一项。");
        RuleFor(input => input.Time).GreaterThanOrEqualTo(0);
    }
}
