using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 获取云下载任务列表输入验证器。
/// </summary>
public sealed class GetOfflineTaskListInputValidator : AbstractValidator<GetOfflineTaskListInput>
{
    /// <summary>
    /// 初始化 <see cref="GetOfflineTaskListInputValidator"/> 类的新实例。
    /// </summary>
    public GetOfflineTaskListInputValidator()
    {
        RuleFor(input => input.Page).GreaterThanOrEqualTo(1);
        RuleFor(input => input.Stat).GreaterThanOrEqualTo(0);
    }
}
