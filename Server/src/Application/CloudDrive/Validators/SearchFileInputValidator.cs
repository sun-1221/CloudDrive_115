using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 搜索文件输入验证器。
/// </summary>
public sealed class SearchFileInputValidator : AbstractValidator<SearchFileInput>
{
    private readonly CloudDriveAppConfig _config;

    /// <summary>
    /// 初始化 <see cref="SearchFileInputValidator"/> 类的新实例。
    /// </summary>
    public SearchFileInputValidator(Microsoft.Extensions.Options.IOptions<CloudDriveAppConfig> configOptions)
    {
        _config = configOptions.Value;

        RuleFor(input => input.Keyword).NotEmptyText("搜索关键字不能为空。");
        RuleFor(input => input.Offset).GreaterThanOrEqualTo(0);
        RuleFor(input => input.Limit)
            .InclusiveBetween(1, _config.MaxPageSize)
            .WithMessage(_ => $"Limit 超出系统保护上限 CloudDrive:MaxPageSize={_config.MaxPageSize}。");
        RuleFor(input => input.Date)
            .Must(CloudDriveValidationRules.IsValidDate)
            .WithMessage("Date 必须为 yyyy-MM-dd。");
    }
}
