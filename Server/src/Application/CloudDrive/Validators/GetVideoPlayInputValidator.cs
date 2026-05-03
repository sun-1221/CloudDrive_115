using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 获取视频播放输入验证器。
/// </summary>
public sealed class GetVideoPlayInputValidator : AbstractValidator<GetVideoPlayInput>
{
    /// <summary>
    /// 初始化 <see cref="GetVideoPlayInputValidator"/> 类的新实例。
    /// </summary>
    public GetVideoPlayInputValidator()
    {
        this.RequireCloudAccount();
        RuleFor(input => input)
            .Must(input => !string.IsNullOrWhiteSpace(input.FileId) || !string.IsNullOrWhiteSpace(input.PickCode))
            .WithMessage("FileId 与 PickCode 必须至少提供一项。");
        RuleFor(input => input.Definition)
            .Must(CloudDriveValidationRules.IsValidVideoDefinition)
            .WithMessage("Definition 必须为 1、2、3、4、5 或 100。");
    }
}
