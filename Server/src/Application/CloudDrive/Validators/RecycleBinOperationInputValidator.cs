using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 回收站操作输入验证器。
/// </summary>
public sealed class RecycleBinOperationInputValidator : AbstractValidator<RecycleBinOperationInput>
{
    /// <summary>
    /// 初始化 <see cref="RecycleBinOperationInputValidator"/> 类的新实例。
    /// </summary>
    public RecycleBinOperationInputValidator()
    {
        this.RequireCloudAccount();
        this.RequireNonEmptyItems(input => input.RecycleBinItemIds, "RecycleBinItemIds");
    }
}
