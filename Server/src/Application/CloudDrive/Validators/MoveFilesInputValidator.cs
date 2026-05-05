using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 移动文件输入验证器。
/// </summary>
public sealed class MoveFilesInputValidator : AbstractValidator<MoveFilesInput>
{
    /// <summary>
    /// 初始化 <see cref="MoveFilesInputValidator"/> 类的新实例。
    /// </summary>
    public MoveFilesInputValidator()
    {
        this.RequireNonEmptyItems(input => input.FileIds, "FileIds");
        RuleFor(input => input.TargetDirectoryId).NotEmptyText("目标目录 ID 不能为空。");
    }
}
