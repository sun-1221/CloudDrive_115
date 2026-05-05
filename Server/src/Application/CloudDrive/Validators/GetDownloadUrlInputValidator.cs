using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 获取下载地址输入验证器。
/// </summary>
public sealed class GetDownloadUrlInputValidator : AbstractValidator<GetDownloadUrlInput>
{
    /// <summary>
    /// 初始化 <see cref="GetDownloadUrlInputValidator"/> 类的新实例。
    /// </summary>
    public GetDownloadUrlInputValidator()
    {
        RuleFor(input => input.PickCode).NotEmptyText("PickCode 不能为空。");
    }
}
