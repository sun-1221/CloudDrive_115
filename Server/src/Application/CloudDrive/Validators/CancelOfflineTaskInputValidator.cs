using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 取消云下载任务输入验证器。
/// </summary>
public sealed class CancelOfflineTaskInputValidator : AbstractValidator<CancelOfflineTaskInput>
{
    /// <summary>
    /// 初始化 <see cref="CancelOfflineTaskInputValidator"/> 类的新实例。
    /// </summary>
    public CancelOfflineTaskInputValidator()
    {
        RuleFor(input => input.InfoHash).NotEmptyText("InfoHash 不能为空。");
    }
}
