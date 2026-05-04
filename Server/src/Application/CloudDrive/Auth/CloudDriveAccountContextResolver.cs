using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;

using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.Results;

namespace Application.CloudDrive.Auth;

/// <summary>
/// 通过当前用户唯一 Active Cookie 会话解析 115 账号上下文。
/// </summary>
public sealed class CloudDriveAccountContextResolver : ICloudDriveAccountContextResolver
{
    private static readonly Error NoActiveAccount = new(
        "cloud_drive_account_context_unavailable",
        "当前用户没有可用的 115 Cookie 会话，请由运维注入 Cookie。");

    private static readonly Error MultipleActiveAccounts = new(
        "cloud_drive_account_context_ambiguous",
        "当前用户存在多条 Active 115 Cookie 会话，无法确定唯一账号上下文。");

    private readonly ICookieSessionRepository _cookieSessionRepository;

    public CloudDriveAccountContextResolver(ICookieSessionRepository cookieSessionRepository)
    {
        _cookieSessionRepository = cookieSessionRepository;
    }

    /// <inheritdoc />
    public async Task<Result<CloudDriveAccountContext>> ResolveAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CookieSession> sessions = await _cookieSessionRepository.GetByUserAsync(
            userId,
            cancellationToken);

        List<CookieSession> activeSessions = sessions
            .Where(session => session.Status == CookieSessionStatus.Active)
            .ToList();

        if (activeSessions.Count == 0)
        {
            return Result.Failure<CloudDriveAccountContext>(NoActiveAccount);
        }

        if (activeSessions.Count > 1)
        {
            return Result.Failure<CloudDriveAccountContext>(MultipleActiveAccounts);
        }

        CookieSession session = activeSessions[0];
        return Result.Success(new CloudDriveAccountContext(
            userId,
            session.CloudAccountUid,
            session.CloudAccountUid));
    }
}
