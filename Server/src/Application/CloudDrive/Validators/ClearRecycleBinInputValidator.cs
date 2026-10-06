using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 清空回收站输入验证器。
/// </summary>
public sealed class ClearRecycleBinInputValidator : AbstractValidator<ClearRecycleBinInput>
{
    /// <summary>
    /// 初始化 <see cref="ClearRecycleBinInputValidator"/> 类的新实例。
    /// </summary>
    public ClearRecycleBinInputValidator()
    {
    }
}
