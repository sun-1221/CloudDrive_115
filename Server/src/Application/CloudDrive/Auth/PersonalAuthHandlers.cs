using Application.Abstractions;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Mappings;
using Application.Commands;
using Application.Queries;

using Domain.Entities;
using Domain.Repositories;
using Domain.Results;
using Domain.ValueObjects;

using DomainUser = Domain.Entities.User;

namespace Application.CloudDrive.Auth;

internal static class PersonalAuthErrors
{
    public static readonly Error InvalidCredentials = new("invalid_credentials", "用户名或密码错误。");
    public static readonly Error UserNameTaken = new("user_name_taken", "用户名已存在。");
    public static readonly Error SessionUnavailable = new("session_unavailable", "登录会话已失效，请重新登录。");
    public static readonly Error AccountUnavailable = new("cloud_drive_account_unavailable", "当前 115 账号不可用。");
}

internal sealed class RegisterCommandHandler(
    IUserRepository users,
    IUserSessionRepository sessions,
    ISecretPasswordHasher passwordHasher) : ICommandHandler<RegisterCommand, AuthSessionDto>
{
    public async Task<Result<AuthSessionDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        string userName = request.Input.UserName.Trim().ToLowerInvariant();
        if (await users.GetByUserNameAsync(userName, cancellationToken) is not null)
        {
            return Result.Failure<AuthSessionDto>(PersonalAuthErrors.UserNameTaken);
        }

        DomainUser user = DomainUser.Create(userName, passwordHasher.Hash(request.Input.Password));
        await users.InsertAsync(user, cancellationToken);
        UserSession session = UserSession.Create(user.Id, TimeSpan.FromDays(30));
        await sessions.InsertAsync(session, cancellationToken);
        return Result.Success(new AuthSessionDto { UserId = user.Id, SessionId = session.Id, ExpiresAt = session.ExpiresAt, UserName = user.UserName });
    }
}

internal sealed class LoginCommandHandler(
    IUserRepository users,
    IUserSessionRepository sessions,
    ISecretPasswordHasher passwordHasher) : ICommandHandler<LoginCommand, AuthSessionDto>
{
    public async Task<Result<AuthSessionDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        string userName = request.Input.UserName.Trim().ToLowerInvariant();
        DomainUser? user = await users.GetByUserNameAsync(userName, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Input.Password, user.PasswordHash))
        {
            return Result.Failure<AuthSessionDto>(PersonalAuthErrors.InvalidCredentials);
        }

        UserSession session = UserSession.Create(user.Id, TimeSpan.FromDays(30));
        await sessions.InsertAsync(session, cancellationToken);
        return Result.Success(new AuthSessionDto { UserId = user.Id, SessionId = session.Id, ExpiresAt = session.ExpiresAt, UserName = user.UserName });
    }
}

internal sealed class LogoutCommandHandler(IUserSessionRepository sessions) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        UserSession? session = await sessions.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is not null && session.UserId == request.UserId)
        {
            session.Revoke();
            await sessions.UpdateAsync(session, cancellationToken);
        }
        return Result.Success();
    }
}

internal sealed class RefreshCommandHandler(IUserSessionRepository sessions, IUserRepository users)
    : ICommandHandler<RefreshCommand, AuthSessionDto>
{
    public async Task<Result<AuthSessionDto>> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        UserSession? session = await sessions.GetActiveAsync(request.SessionId, cancellationToken);
        DomainUser? user = await users.GetByIdAsync(request.UserId, cancellationToken);
        if (session is null || user is null || session.UserId != request.UserId)
        {
            return Result.Failure<AuthSessionDto>(PersonalAuthErrors.SessionUnavailable);
        }
        session.Touch();
        await sessions.UpdateAsync(session, cancellationToken);
        return Result.Success(new AuthSessionDto { UserId = user.Id, SessionId = session.Id, ExpiresAt = session.ExpiresAt, UserName = user.UserName });
    }
}

internal sealed class GetCurrentUserQueryHandler(IUserRepository users, IUserSessionRepository userSessions, ICookieSessionRepository cookieSessions)
    : IQueryHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        DomainUser? user = await users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) return Result.Failure<CurrentUserDto>(PersonalAuthErrors.SessionUnavailable);
        UserSession? session = await userSessions.GetActiveAsync(request.SessionId, cancellationToken);
        IReadOnlyList<CookieSession> sessions = await cookieSessions.GetByUserAsync(user.Id, cancellationToken);
        return Result.Success(new CurrentUserDto
        {
            UserId = user.Id,
            UserName = user.UserName,
            CurrentCloudAccountUid = session?.UserId == user.Id ? session.ActiveCloudAccountUid : null,
            Accounts = sessions.Select(session => new CloudDriveAccountSummaryDto
            {
                CloudAccountUid = session.CloudAccountUid,
                Status = session.Status,
                CreationTime = session.CreationTime,
                LastModificationTime = session.LastModificationTime,
                CooldownUntil = session.CooldownUntil,
            }).ToList(),
        });
    }
}

internal sealed class BindCloudDriveAccountCommandHandler(
    CookieSessionManager cookieSessionManager,
    IUserSessionRepository userSessions) : ICommandHandler<BindCloudDriveAccountCommand>
{
    public async Task<Result> Handle(BindCloudDriveAccountCommand request, CancellationToken cancellationToken)
    {
        await cookieSessionManager.UpsertSessionAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            new CookieTriplet(request.Input.Uid, request.Input.Cid, request.Input.Ussid),
            cancellationToken: cancellationToken);
        UserSession? userSession = await userSessions.GetActiveAsync(request.SessionId, cancellationToken);
        if (userSession is not null && userSession.UserId == request.UserId && string.IsNullOrWhiteSpace(userSession.ActiveCloudAccountUid))
        {
            userSession.SelectCloudAccount(request.Input.CloudAccountUid);
            await userSessions.UpdateAsync(userSession, cancellationToken);
        }
        return Result.Success();
    }
}

internal sealed class GetCloudDriveAccountsQueryHandler(CookieSessionManager cookieSessionManager)
    : IQueryHandler<GetCloudDriveAccountsQuery, List<CloudDriveAccountSummaryDto>>
{
    public async Task<Result<List<CloudDriveAccountSummaryDto>>> Handle(GetCloudDriveAccountsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<CookieSessionSummary> summaries = await cookieSessionManager.GetBoundAccountsAsync(request.UserId, cancellationToken);
        return Result.Success(summaries.Select(summary => summary.ToDto(includeOnlyAccountSummary: true)).ToList());
    }
}

internal sealed class SelectCloudDriveAccountCommandHandler(IUserRepository users, IUserSessionRepository userSessions, ICookieSessionRepository sessions)
    : ICommandHandler<SelectCloudDriveAccountCommand>
{
    public async Task<Result> Handle(SelectCloudDriveAccountCommand request, CancellationToken cancellationToken)
    {
        CookieSession? session = await sessions.GetByAccountAsync(request.UserId, request.CloudAccountUid, cancellationToken);
        DomainUser? user = await users.GetByIdAsync(request.UserId, cancellationToken);
        if (session is null || user is null || session.Status != Domain.Enums.CookieSessionStatus.Active)
        {
            return Result.Failure(PersonalAuthErrors.AccountUnavailable);
        }
        UserSession? userSession = await userSessions.GetActiveAsync(request.SessionId, cancellationToken);
        if (userSession is null || userSession.UserId != request.UserId)
        {
            return Result.Failure(PersonalAuthErrors.SessionUnavailable);
        }
        userSession.SelectCloudAccount(request.CloudAccountUid);
        await userSessions.UpdateAsync(userSession, cancellationToken);
        return Result.Success();
    }
}

internal sealed class UnbindCloudDriveAccountCommandHandler(CookieSessionManager cookieSessionManager, IUserSessionRepository userSessions)
    : ICommandHandler<UnbindCloudDriveAccountCommand>
{
    public async Task<Result> Handle(UnbindCloudDriveAccountCommand request, CancellationToken cancellationToken)
    {
        await cookieSessionManager.RevokeSessionAsync(request.UserId, request.CloudAccountUid, cancellationToken);
        UserSession? userSession = await userSessions.GetActiveAsync(request.SessionId, cancellationToken);
        if (userSession is not null && userSession.UserId == request.UserId && userSession.ActiveCloudAccountUid == request.CloudAccountUid)
        {
            userSession.SelectCloudAccount(null);
            await userSessions.UpdateAsync(userSession, cancellationToken);
        }
        return Result.Success();
    }
}
