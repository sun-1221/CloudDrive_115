using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 清空云下载任务输入验证器。
/// </summary>
public sealed class ClearOfflineTasksInputValidator : AbstractValidator<ClearOfflineTasksInput>
{
    /// <summary>
    /// 初始化 <see cref="ClearOfflineTasksInputValidator"/> 类的新实例。
    /// </summary>
    public ClearOfflineTasksInputValidator()
    {
        RuleFor(input => input.Stat).GreaterThanOrEqualTo(0).When(input => input.Stat.HasValue);
    }
}
