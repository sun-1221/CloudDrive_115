using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 获取文件列表输入验证器。
/// </summary>
public sealed class GetFileListInputValidator : AbstractValidator<GetFileListInput>
{
    private readonly CloudDriveAppConfig _config;

    /// <summary>
    /// 初始化 <see cref="GetFileListInputValidator"/> 类的新实例。
    /// </summary>
    public GetFileListInputValidator(Microsoft.Extensions.Options.IOptions<CloudDriveAppConfig> configOptions)
    {
        _config = configOptions.Value;

        this.RequireCloudAccount();
        RuleFor(input => input.Offset).GreaterThanOrEqualTo(0);
        RuleFor(input => input.Limit)
            .InclusiveBetween(1, _config.MaxPageSize)
            .WithMessage(_ => $"Limit 超出系统保护上限 CloudDrive:MaxPageSize={_config.MaxPageSize}。");
        RuleFor(input => input.Asc).Must(CloudDriveValidationRules.IsValidBinaryFlag).WithMessage("Asc 必须为 0 或 1。");
        RuleFor(input => input.Star).Must(CloudDriveValidationRules.IsValidBinaryFlag).WithMessage("Star 必须为 0 或 1。");
        RuleFor(input => input.ShowDir).Must(CloudDriveValidationRules.IsValidBinaryFlag).WithMessage("ShowDir 必须为 0 或 1。");
    }
}
