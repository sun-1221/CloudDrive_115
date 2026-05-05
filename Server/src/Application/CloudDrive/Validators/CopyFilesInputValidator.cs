using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 复制文件输入验证器。
/// </summary>
public sealed class CopyFilesInputValidator : AbstractValidator<CopyFilesInput>
{
    /// <summary>
    /// 初始化 <see cref="CopyFilesInputValidator"/> 类的新实例。
    /// </summary>
    public CopyFilesInputValidator()
    {
        this.RequireNonEmptyItems(input => input.FileIds, "FileIds");
        RuleFor(input => input.TargetDirectoryId).NotEmptyText("目标目录 ID 不能为空。");
    }
}
