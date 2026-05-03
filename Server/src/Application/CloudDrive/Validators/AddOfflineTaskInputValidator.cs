using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 新增云下载任务输入验证器。
/// </summary>
public sealed class AddOfflineTaskInputValidator : AbstractValidator<AddOfflineTaskInput>
{
    /// <summary>
    /// 初始化 <see cref="AddOfflineTaskInputValidator"/> 类的新实例。
    /// </summary>
    public AddOfflineTaskInputValidator()
    {
        this.RequireCloudAccount();
        RuleFor(input => input.Urls)
            .NotNull()
            .Must(urls => urls.Count > 0)
            .WithMessage("Urls 至少包含一项。")
            .Must(urls => urls.All(CloudDriveValidationRules.IsHttpUrl))
            .WithMessage("Urls 必须全部为有效的 HTTP 或 HTTPS 地址。");
    }
}
