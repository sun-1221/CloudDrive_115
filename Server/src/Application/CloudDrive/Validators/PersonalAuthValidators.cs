using FluentValidation;

namespace Application.CloudDrive.Validators;

public sealed class RegisterInputValidator : AbstractValidator<Auth.RegisterInput>
{
    public RegisterInputValidator()
    {
        RuleFor(input => input.UserName).NotEmpty().MaximumLength(128).Matches("^[a-zA-Z0-9_.-]+$");
        RuleFor(input => input.Password).NotEmpty().MinimumLength(8).MaximumLength(256);
    }
}

public sealed class LoginInputValidator : AbstractValidator<Auth.LoginInput>
{
    public LoginInputValidator()
    {
        RuleFor(input => input.UserName).NotEmpty().MaximumLength(128);
        RuleFor(input => input.Password).NotEmpty().MaximumLength(256);
    }
}

public sealed class BindCloudDriveAccountInputValidator : AbstractValidator<Auth.BindCloudDriveAccountInput>
{
    public BindCloudDriveAccountInputValidator()
    {
        RuleFor(input => input.CloudAccountUid).NotEmpty().MaximumLength(128);
        RuleFor(input => input.Uid).NotEmpty().MaximumLength(256);
        RuleFor(input => input.Cid).NotEmpty().MaximumLength(256);
        RuleFor(input => input.Ussid).NotEmpty().MaximumLength(256);
        RuleFor(input => input.DisplayName).MaximumLength(128);
    }
}
