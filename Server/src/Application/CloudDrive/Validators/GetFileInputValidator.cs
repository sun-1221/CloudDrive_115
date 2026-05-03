using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 获取文件输入验证器。
/// </summary>
public sealed class GetFileInputValidator : AbstractValidator<GetFileInput>
{
    /// <summary>
    /// 初始化 <see cref="GetFileInputValidator"/> 类的新实例。
    /// </summary>
    public GetFileInputValidator()
    {
        this.RequireCloudAccount();
        RuleFor(input => input)
            .Must(input => !string.IsNullOrWhiteSpace(input.FileId) || !string.IsNullOrWhiteSpace(input.Path))
            .WithMessage("FileId 与 Path 必须至少提供一项。");
    }
}
