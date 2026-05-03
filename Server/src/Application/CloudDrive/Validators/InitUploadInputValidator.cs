using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 初始化上传输入验证器。
/// </summary>
public sealed class InitUploadInputValidator : AbstractValidator<InitUploadInput>
{
    /// <summary>
    /// 初始化 <see cref="InitUploadInputValidator"/> 类的新实例。
    /// </summary>
    public InitUploadInputValidator()
    {
        this.RequireCloudAccount();
        RuleFor(input => input.DirectoryId).NotEmptyText("上传目标目录 ID 不能为空。");
        RuleFor(input => input.FileName).NotEmptyText("文件名不能为空。");
        RuleFor(input => input.FileSize).GreaterThanOrEqualTo(0);
        RuleFor(input => input.FileSha1).NotEmptyText("文件 SHA1 不能为空。").ValidSha1();
        RuleFor(input => input.PreSha1).ValidOptionalSha1();
    }
}
