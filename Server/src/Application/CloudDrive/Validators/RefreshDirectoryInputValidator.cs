using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 刷新目录输入验证器。
/// </summary>
public sealed class RefreshDirectoryInputValidator : AbstractValidator<RefreshDirectoryInput>
{
    /// <summary>
    /// 初始化 <see cref="RefreshDirectoryInputValidator"/> 类的新实例。
    /// </summary>
    public RefreshDirectoryInputValidator()
    {
    }
}
