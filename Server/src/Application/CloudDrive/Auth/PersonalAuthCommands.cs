using Application.Abstractions;
using Application.Commands;

namespace Application.CloudDrive.Auth;

public sealed record RegisterCommand(RegisterInput Input) : ICommand<AuthSessionDto>, IHasInput<RegisterInput>;

public sealed record LoginCommand(LoginInput Input) : ICommand<AuthSessionDto>, IHasInput<LoginInput>;

public sealed record LogoutCommand(Guid UserId, Guid SessionId) : ICommand;

public sealed record RefreshCommand(Guid UserId, Guid SessionId) : ICommand<AuthSessionDto>;

public sealed record GetCurrentUserQuery(Guid UserId, Guid SessionId) : Application.Queries.IQuery<CurrentUserDto>;

public sealed record BindCloudDriveAccountCommand(Guid UserId, Guid SessionId, BindCloudDriveAccountInput Input)
    : ICommand, IHasInput<BindCloudDriveAccountInput>;

public sealed record GetCloudDriveAccountsQuery(Guid UserId)
    : Application.Queries.IQuery<List<Application.CloudDrive.Dtos.CloudDriveAccountSummaryDto>>;

public sealed record SelectCloudDriveAccountCommand(Guid UserId, Guid SessionId, string CloudAccountUid) : ICommand;

public sealed record UnbindCloudDriveAccountCommand(Guid UserId, Guid SessionId, string CloudAccountUid) : ICommand;
