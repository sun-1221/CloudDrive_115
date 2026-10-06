using Application.Abstractions;
using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;

using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.Results;

namespace Application.CloudDrive.Auth;

/// <summary>解析当前用户登录会话选择的 115 账号上下文。</summary>
public sealed class CloudDriveAccountContextResolver : ICloudDriveAccountContextResolver
{
    private static readonly Error NoActiveAccount = new("cloud_drive_account_context_unavailable", "当前用户没有可用的 115 Cookie 会话。");
    private static readonly Error MultipleActiveAccounts = new("cloud_drive_account_context_ambiguous", "当前会话存在多条 Active 115 Cookie 会话，无法确定账号。");
    private readonly ICookieSessionRepository _cookieSessionRepository;
    private readonly IUserSessionRepository? _userSessionRepository;
    private readonly IUserSessionContextAccessor? _sessionContextAccessor;

    public CloudDriveAccountContextResolver(ICookieSessionRepository cookieSessionRepository, IUserSessionRepository userSessionRepository, IUserSessionContextAccessor sessionContextAccessor)
    {
        _cookieSessionRepository = cookieSessionRepository;
        _userSessionRepository = userSessionRepository;
        _sessionContextAccessor = sessionContextAccessor;
    }

    public CloudDriveAccountContextResolver(ICookieSessionRepository cookieSessionRepository)
    {
        _cookieSessionRepository = cookieSessionRepository;
    }

    public async Task<Result<CloudDriveAccountContext>> ResolveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CookieSession> sessions = await _cookieSessionRepository.GetByUserAsync(userId, cancellationToken);
        List<CookieSession> activeSessions = sessions.Where(session => session.Status == CookieSessionStatus.Active).ToList();
        if (activeSessions.Count == 0)
        {
            return Result.Failure<CloudDriveAccountContext>(NoActiveAccount);
        }

        UserSession? selectedSession = _sessionContextAccessor?.SessionId is Guid sessionId && _userSessionRepository is not null
            ? await _userSessionRepository.GetActiveAsync(sessionId, cancellationToken)
            : null;
        string? selectedUid = selectedSession?.UserId == userId ? selectedSession.ActiveCloudAccountUid : null;

        CookieSession? selected = activeSessions.FirstOrDefault(session => string.Equals(session.CloudAccountUid, selectedUid, StringComparison.Ordinal));
        if (selected is null && activeSessions.Count > 1)
        {
            return Result.Failure<CloudDriveAccountContext>(MultipleActiveAccounts);
        }

        CookieSession session = selected ?? activeSessions[0];
        return Result.Success(new CloudDriveAccountContext(userId, session.CloudAccountUid, session.CloudAccountUid));
    }
}
