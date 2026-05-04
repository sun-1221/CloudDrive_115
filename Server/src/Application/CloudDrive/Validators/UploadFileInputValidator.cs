using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 上传文件输入验证器。
/// </summary>
public sealed class UploadFileInputValidator : AbstractValidator<UploadFileInput>
{
    /// <summary>
    /// 初始化 <see cref="UploadFileInputValidator"/> 类的新实例。
    /// </summary>
    public UploadFileInputValidator()
    {
        this.RequireCloudAccount();
        RuleFor(input => input.DirectoryId).NotEmptyText("上传目标目录 ID 不能为空。");
        RuleFor(input => input.FileName).NotEmptyText("文件名不能为空。");
        RuleFor(input => input.FileSize).GreaterThanOrEqualTo(0);
        RuleFor(input => input.FileSha1).ValidOptionalSha1();
        RuleFor(input => input.PreSha1).ValidOptionalSha1();
    }
}
