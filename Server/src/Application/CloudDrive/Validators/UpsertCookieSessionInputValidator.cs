using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

/// <summary>
/// 注入或更新 Cookie 会话输入验证器。
/// </summary>
public sealed class UpsertCookieSessionInputValidator : AbstractValidator<UpsertCookieSessionInput>
{
    /// <summary>
    /// 初始化 <see cref="UpsertCookieSessionInputValidator"/> 类的新实例。
    /// </summary>
    public UpsertCookieSessionInputValidator()
    {
        RuleFor(input => input.CloudAccountUid).NotEmptyText("CloudAccountUid 不能为空。");
        RuleFor(input => input.Uid).NotEmptyText("UID 不能为空。");
        RuleFor(input => input.Cid).NotEmptyText("CID 不能为空。");
        RuleFor(input => input.Ussid).NotEmptyText("USSID 不能为空。");
        RuleFor(input => input.ExtraFields)
            .Must(fields => fields is null || fields.All(field =>
                !string.IsNullOrWhiteSpace(field.Key) && !string.IsNullOrWhiteSpace(field.Value)))
            .WithMessage("ExtraFields 不能包含空键或空值。");
    }
}
