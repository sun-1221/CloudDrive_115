using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 撤销 Cookie 会话输入验证器。
/// </summary>
public sealed class RevokeCookieSessionInputValidator : AbstractValidator<RevokeCookieSessionInput>
{
    /// <summary>
    /// 初始化 <see cref="RevokeCookieSessionInputValidator"/> 类的新实例。
    /// </summary>
    public RevokeCookieSessionInputValidator()
    {
        this.RequireCloudAccount();
    }
}
