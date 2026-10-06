using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 获取任务状态输入验证器。
/// </summary>
public sealed class GetJobStatusInputValidator : AbstractValidator<GetJobStatusInput>
{
    /// <summary>
    /// 初始化 <see cref="GetJobStatusInputValidator"/> 类的新实例。
    /// </summary>
    public GetJobStatusInputValidator()
    {
        RuleFor(input => input.JobId).NotEmpty();
    }
}
