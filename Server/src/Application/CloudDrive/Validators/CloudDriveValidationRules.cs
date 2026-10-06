using Application.CloudDrive.Dtos;

using FluentValidation;

namespace Application.CloudDrive.Validators;

internal static class CloudDriveValidationRules
{
    public const int MaxDefaultListLimit = 200;

    private static readonly int[] VideoDefinitions = [1, 2, 3, 4, 5, 100];
    private static readonly char[] InvalidFileNameCharacters = ['/', '\\', ':', '*', '?', '"', '<', '>', '|'];

    public static IRuleBuilderOptions<T, string> NotEmptyText<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        string message)
    {
        return ruleBuilder.NotEmpty().Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage(message);
    }

    public static IRuleBuilderOptions<T, string> ValidSha1<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Must(IsSha1).WithMessage("SHA1 必须是 40 位十六进制字符串。");
    }

    public static IRuleBuilderOptions<T, string?> ValidOptionalSha1<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => string.IsNullOrWhiteSpace(value) || IsSha1(value))
            .WithMessage("SHA1 必须是 40 位十六进制字符串。");
    }

    public static void RequireCloudAccount<T>(this AbstractValidator<T> validator)
        where T : CloudDriveAccountScopedInput
    {
        validator.RuleFor(input => input.CloudAccountUid).NotEmptyText("CloudAccountUid 不能为空。");
    }

    public static void RequireNonEmptyItems<T>(
        this AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, List<string>>> expression,
        string propertyName)
    {
        validator.RuleFor(expression)
            .NotNull()
            .Must(items => items.Count > 0)
            .WithMessage($"{propertyName} 至少包含一项。")
            .Must(items => items.All(item => !string.IsNullOrWhiteSpace(item)))
            .WithMessage($"{propertyName} 不能包含空值。");
    }

    public static bool IsValidBinaryFlag(int? value)
    {
        return !value.HasValue || value.Value is 0 or 1;
    }

    public static bool IsValidVideoDefinition(int? value)
    {
        return !value.HasValue || VideoDefinitions.Contains(value.Value);
    }

    public static bool IsValidFolderName(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Length <= 255
            && value.IndexOfAny(InvalidFileNameCharacters) < 0;
    }

    public static bool IsValidRenameName(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && System.Text.Encoding.UTF8.GetByteCount(value) <= 255
            && value.IndexOfAny(InvalidFileNameCharacters) < 0;
    }

    public static bool IsValidDate(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            || DateOnly.TryParseExact(value, "yyyy-MM-dd", out _);
    }

    public static bool IsHttpUrl(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            && uri.Scheme is "http" or "https";
    }

    private static bool IsSha1(string? value)
    {
        return value is { Length: 40 } && value.All(Uri.IsHexDigit);
    }
}
