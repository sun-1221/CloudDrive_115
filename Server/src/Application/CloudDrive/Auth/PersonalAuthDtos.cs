namespace Application.CloudDrive.Auth;

public sealed class RegisterInput
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginInput
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class BindCloudDriveAccountInput
{
    public string CloudAccountUid { get; set; } = string.Empty;
    public string Uid { get; set; } = string.Empty;
    public string Cid { get; set; } = string.Empty;
    public string Ussid { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

public sealed class AuthSessionDto
{
    public Guid UserId { get; init; }
    public Guid SessionId { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string UserName { get; init; } = string.Empty;
}

public sealed class CurrentUserDto
{
    public Guid UserId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string? CurrentCloudAccountUid { get; init; }
    public IReadOnlyList<Application.CloudDrive.Dtos.CloudDriveAccountSummaryDto> Accounts { get; init; } = [];
}
