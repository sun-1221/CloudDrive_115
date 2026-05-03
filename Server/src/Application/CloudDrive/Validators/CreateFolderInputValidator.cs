using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 创建文件夹输入验证器。
/// </summary>
public sealed class CreateFolderInputValidator : AbstractValidator<CreateFolderInput>
{
    /// <summary>
    /// 初始化 <see cref="CreateFolderInputValidator"/> 类的新实例。
    /// </summary>
    public CreateFolderInputValidator()
    {
        this.RequireCloudAccount();
        RuleFor(input => input.ParentId).NotEmptyText("父目录 ID 不能为空。");
        RuleFor(input => input.FolderName)
            .Must(CloudDriveValidationRules.IsValidFolderName)
            .WithMessage("文件夹名称不能为空、不能包含非法字符且不能超过 255 个字符。");
    }
}
