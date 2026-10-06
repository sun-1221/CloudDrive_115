using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 获取回收站列表输入验证器。
/// </summary>
public sealed class GetRecycleBinListInputValidator : AbstractValidator<GetRecycleBinListInput>
{
    private readonly CloudDriveAppConfig _config;

    /// <summary>
    /// 初始化 <see cref="GetRecycleBinListInputValidator"/> 类的新实例。
    /// </summary>
    public GetRecycleBinListInputValidator(Microsoft.Extensions.Options.IOptions<CloudDriveAppConfig> configOptions)
    {
        _config = configOptions.Value;

        RuleFor(input => input.Offset).GreaterThanOrEqualTo(0);
        RuleFor(input => input.Limit)
            .InclusiveBetween(1, _config.MaxPageSize)
            .WithMessage(_ => $"Limit 超出系统保护上限 CloudDrive:MaxPageSize={_config.MaxPageSize}。");
    }
}
