using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 获取视频播放进度输入验证器。
/// </summary>
public sealed class GetVideoPlayProgressInputValidator : AbstractValidator<GetVideoPlayProgressInput>
{
    /// <summary>
    /// 初始化 <see cref="GetVideoPlayProgressInputValidator"/> 类的新实例。
    /// </summary>
    public GetVideoPlayProgressInputValidator()
    {
        RuleFor(input => input)
            .Must(input => !string.IsNullOrWhiteSpace(input.FileId) || !string.IsNullOrWhiteSpace(input.PickCode))
            .WithMessage("FileId 与 PickCode 必须至少提供一项。");
    }
}
