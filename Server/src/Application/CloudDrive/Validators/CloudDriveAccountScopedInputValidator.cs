using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 115 账号上下文输入验证器。
/// </summary>
public sealed class CloudDriveAccountScopedInputValidator : AbstractValidator<CloudDriveAccountScopedInput>
{
    /// <summary>
    /// 初始化 <see cref="CloudDriveAccountScopedInputValidator"/> 类的新实例。
    /// </summary>
    public CloudDriveAccountScopedInputValidator()
    {
        this.RequireCloudAccount();
    }
}
