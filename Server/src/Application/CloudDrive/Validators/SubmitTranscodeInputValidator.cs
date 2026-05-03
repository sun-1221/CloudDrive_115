using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 提交视频转码输入验证器。
/// </summary>
public sealed class SubmitTranscodeInputValidator : AbstractValidator<SubmitTranscodeInput>
{
    /// <summary>
    /// 初始化 <see cref="SubmitTranscodeInputValidator"/> 类的新实例。
    /// </summary>
    public SubmitTranscodeInputValidator()
    {
        this.RequireCloudAccount();
        RuleFor(input => input)
            .Must(input => !string.IsNullOrWhiteSpace(input.FileId) || !string.IsNullOrWhiteSpace(input.PickCode))
            .WithMessage("FileId 与 PickCode 必须至少提供一项。");
        RuleFor(input => input.Op)
            .Must(value => value is "vip_push" or "pay_push")
            .WithMessage("Op 必须为 vip_push 或 pay_push。");
    }
}
