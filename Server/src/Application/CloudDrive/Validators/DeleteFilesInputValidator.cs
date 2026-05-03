using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 删除文件输入验证器。
/// </summary>
public sealed class DeleteFilesInputValidator : AbstractValidator<DeleteFilesInput>
{
    /// <summary>
    /// 初始化 <see cref="DeleteFilesInputValidator"/> 类的新实例。
    /// </summary>
    public DeleteFilesInputValidator()
    {
        this.RequireCloudAccount();
        this.RequireNonEmptyItems(input => input.FileIds, "FileIds");
    }
}
