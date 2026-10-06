using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 重命名文件输入验证器。
/// </summary>
public sealed class RenameFileInputValidator : AbstractValidator<RenameFileInput>
{
    /// <summary>
    /// 初始化 <see cref="RenameFileInputValidator"/> 类的新实例。
    /// </summary>
    public RenameFileInputValidator()
    {
        RuleFor(input => input.FileId).NotEmptyText("FileId 不能为空。");
        RuleFor(input => input.NewFileName)
            .Must(CloudDriveValidationRules.IsValidRenameName)
            .WithMessage("文件名不能为空、不能包含非法字符且 UTF-8 编码后不能超过 255 字节。");
    }
}
