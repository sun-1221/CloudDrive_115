using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 获取字幕输入验证器。
/// </summary>
public sealed class GetSubtitlesInputValidator : AbstractValidator<GetSubtitlesInput>
{
    /// <summary>
    /// 初始化 <see cref="GetSubtitlesInputValidator"/> 类的新实例。
    /// </summary>
    public GetSubtitlesInputValidator()
    {
        RuleFor(input => input)
            .Must(input => !string.IsNullOrWhiteSpace(input.FileId) || !string.IsNullOrWhiteSpace(input.PickCode))
            .WithMessage("FileId 与 PickCode 必须至少提供一项。");
    }
}
