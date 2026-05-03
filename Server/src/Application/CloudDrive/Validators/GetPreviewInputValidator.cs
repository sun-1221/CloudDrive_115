using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 获取预览输入验证器。
/// </summary>
public sealed class GetPreviewInputValidator : AbstractValidator<GetPreviewInput>
{
    /// <summary>
    /// 初始化 <see cref="GetPreviewInputValidator"/> 类的新实例。
    /// </summary>
    public GetPreviewInputValidator()
    {
        this.RequireCloudAccount();
        RuleFor(input => input.FileId).NotEmptyText("FileId 不能为空。");
    }
}
