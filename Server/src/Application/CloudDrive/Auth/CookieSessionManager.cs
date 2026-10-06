using System.Text.Json;

using Application.Abstractions.CloudDrive;

using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.ValueObjects;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Auth;

/// <summary>
/// Cookie 会话管理器。按 <c>UserId + CloudAccountUid</c> 编排 Cookie 会话生命周期
/// （读取、更新、加密存储、失效标记、冷却管理）。
/// </summary>
/// <remarks>
/// 普通类 + 构造函数注入，不继承框架基类。
/// 与 OAuth 方案不同，Cookie 没有自动刷新机制 — 失效后必须由管理员重新注入。
/// </remarks>
public sealed class CookieSessionManager(
    ICookieSessionRepository cookieSessionRepository,
    ICloudDriveSecretProtector secretProtector,
    ICookieInvalidationNotifier invalidationNotifier,
    IOptions<CloudDriveAppConfig> configOptions,
    ILogger<CookieSessionManager> logger)
{
    /// <summary>
    /// 存储或更新 Cookie 会话。将 UID/CID/USSID + 额外字段序列化为 JSON 并加密持久化。
    /// 同一 <c>UserId + CloudAccountUid</c> 上下文只保留一条记录。
    /// </summary>
    /// <returns>表示异步 Cookie 会话保存操作的 <see cref="Task"/>。</returns>
    public async Task UpsertSessionAsync(
        Guid userId,
        string cloudAccountUid,
        CookieTriplet cookies,
        IDictionary<string, string>? extraFields = null,
        CancellationToken cancellationToken = default)
    {
        CloudDriveAppConfig config = configOptions.Value;

        string payloadJson = SerializeCookiePayload(cookies, extraFields);
        string encryptedPayload = secretProtector.Protect(config.CookieEncryptionKeyName, payloadJson);

        CookieSession? existing = await cookieSessionRepository.GetByAccountAsync(
            userId, cloudAccountUid, cancellationToken);

        if (existing is not null)
        {
            existing.UpdatePayload(encryptedPayload, keyVersion: 1);
            await cookieSessionRepository.UpdateAsync(existing, cancellationToken);

            logger.LogInformation(
                "已更新 Cookie 会话 UserId={UserId}, CloudAccountUid={CloudAccountUid}",
                userId, cloudAccountUid);
        }
        else
        {
            CookieSession session = CookieSession.Create(userId, cloudAccountUid, encryptedPayload, keyVersion: 1);
            await cookieSessionRepository.InsertAsync(session, cancellationToken);

            logger.LogInformation(
                "已创建 Cookie 会话 UserId={UserId}, CloudAccountUid={CloudAccountUid}",
                userId, cloudAccountUid);
        }
    }

    /// <summary>
    /// 获取当前有效的 Cookie 字符串（用于请求头注入）。
    /// 若 Cookie 处于失效或冷却期内，抛出 <see cref="CloudDriveCookieInvalidatedException"/>。
    /// </summary>
    /// <returns>返回可注入 115 请求头的 Cookie 字符串。</returns>
    public async Task<string> GetValidCookieHeaderAsync(
        Guid userId,
        string cloudAccountUid,
        CancellationToken cancellationToken = default)
    {
        CloudDriveAppConfig config = configOptions.Value;

        CookieSession session = await GetRequiredSessionAsync(userId, cloudAccountUid, cancellationToken);

        if (session.Status != CookieSessionStatus.Active)
        {
            throw new CloudDriveCookieInvalidatedException(
                $"115 Cookie 会话不可用 CloudAccountUid={cloudAccountUid}, Reason={session.InvalidationReason ?? "session_not_active"}",
                "cloud_drive_cookie_invalidated");
        }

        if (session.IsInCooldown(DateTime.UtcNow))
        {
            throw new CloudDriveCookieInvalidatedException(
                $"115 Cookie 会话处于冷却期 CloudAccountUid={cloudAccountUid}, CooldownUntil={session.CooldownUntil}",
                "cloud_drive_cookie_in_cooldown");
        }

        try
        {
            string payloadJson = secretProtector.Unprotect(
                config.CookieEncryptionKeyName,
                session.EncryptedCookiePayload,
                session.KeyVersion);

            return AssembleCookieHeader(payloadJson);
        }
        catch (Exception ex) when (ex is not CloudDriveCookieInvalidatedException)
        {
            logger.LogError(
                ex,
                "Cookie 解密失败 UserId={UserId}, CloudAccountUid={CloudAccountUid}, KeyVersion={KeyVersion}",
                userId, cloudAccountUid, session.KeyVersion);

            throw new CloudDriveCookieInvalidatedException(
                $"Cookie 解密失败 CloudAccountUid={cloudAccountUid}",
                "cloud_drive_cookie_decryption_failed");
        }
    }

    /// <summary>
    /// 检查指定账号是否已绑定可用 Cookie。
    /// </summary>
    /// <returns>已绑定且处于可用状态时返回 <see langword="true"/>。</returns>
    public async Task<bool> IsAuthorizedAsync(
        Guid userId,
        string cloudAccountUid,
        CancellationToken cancellationToken = default)
    {
        CookieSession? session = await cookieSessionRepository.GetByAccountAsync(
            userId, cloudAccountUid, cancellationToken);

        return session is not null
            && session.Status == CookieSessionStatus.Active
            && !session.IsInCooldown(DateTime.UtcNow);
    }

    /// <summary>
    /// 标记 Cookie 失效并进入冷却期。
    /// </summary>
    /// <returns>表示异步失效标记操作的 <see cref="Task"/>。</returns>
    public async Task MarkInvalidatedAsync(
        Guid userId,
        string cloudAccountUid,
        string reason,
        CancellationToken cancellationToken = default)
    {
        CloudDriveAppConfig config = configOptions.Value;

        CookieSession session = await GetRequiredSessionAsync(userId, cloudAccountUid, cancellationToken);
        session.MarkInvalidated(reason, config.CookieCooldownSeconds);
        await cookieSessionRepository.UpdateAsync(session, cancellationToken);

        await invalidationNotifier.NotifyAsync(userId, cloudAccountUid, reason, cancellationToken);

        logger.LogWarning(
            "Cookie 已标记失效 UserId={UserId}, CloudAccountUid={CloudAccountUid}, Reason={Reason}, CooldownUntil={CooldownUntil}",
            userId, cloudAccountUid, reason, session.CooldownUntil);
    }

    /// <summary>
    /// 进入冷却期（可覆盖默认冷却时长）。供 <c>EnterCookieCooldownCommand</c> 使用。
    /// </summary>
    /// <returns>表示异步冷却期更新操作的 <see cref="Task"/>。</returns>
    public async Task EnterCooldownAsync(
        Guid userId,
        string cloudAccountUid,
        string reason,
        int? cooldownSeconds = null,
        CancellationToken cancellationToken = default)
    {
        CloudDriveAppConfig config = configOptions.Value;
        int effectiveCooldown = cooldownSeconds ?? config.CookieCooldownSeconds;

        CookieSession session = await GetRequiredSessionAsync(userId, cloudAccountUid, cancellationToken);
        session.EnterCooldown(reason, effectiveCooldown);
        await cookieSessionRepository.UpdateAsync(session, cancellationToken);

        await invalidationNotifier.NotifyAsync(userId, cloudAccountUid, reason, cancellationToken);
    }

    /// <summary>
    /// 撤销指定账号的 Cookie 会话（主动解绑）。
    /// </summary>
    /// <returns>表示异步会话撤销操作的 <see cref="Task"/>。</returns>
    public async Task RevokeSessionAsync(
        Guid userId,
        string cloudAccountUid,
        CancellationToken cancellationToken = default)
    {
        CookieSession session = await GetRequiredSessionAsync(userId, cloudAccountUid, cancellationToken);
        session.Revoke();
        await cookieSessionRepository.UpdateAsync(session, cancellationToken);

        logger.LogInformation(
            "Cookie 会话已撤销 UserId={UserId}, CloudAccountUid={CloudAccountUid}",
            userId, cloudAccountUid);
    }

    /// <summary>
    /// 撤销指定账号的 Cookie 会话（主动解绑）。
    /// </summary>
    /// <returns>表示异步会话撤销操作的 <see cref="Task"/>。</returns>
    public Task RevokeAsync(
        Guid userId,
        string cloudAccountUid,
        CancellationToken cancellationToken = default)
    {
        return RevokeSessionAsync(userId, cloudAccountUid, cancellationToken);
    }

    /// <summary>
    /// 返回当前用户下所有 Cookie 会话摘要列表。
    /// 不暴露 Cookie 原文。
    /// </summary>
    /// <returns>返回当前用户已绑定账号的会话摘要列表。</returns>
    public async Task<IReadOnlyList<CookieSessionSummary>> GetBoundAccountsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CookieSession> sessions = await cookieSessionRepository.GetByUserAsync(
            userId, cancellationToken);

        return sessions.Select(s => new CookieSessionSummary
        {
            CloudAccountUid = s.CloudAccountUid,
            Status = s.Status,
            CreationTime = s.CreationTime,
            LastModificationTime = s.LastModificationTime,
            CooldownUntil = s.CooldownUntil,
        }).ToList().AsReadOnly();
    }

    private async Task<CookieSession> GetRequiredSessionAsync(
        Guid userId,
        string cloudAccountUid,
        CancellationToken cancellationToken)
    {
        CookieSession? session = await cookieSessionRepository.GetByAccountAsync(
            userId, cloudAccountUid, cancellationToken);

        if (session is null)
        {
            throw new CloudDriveCookieInvalidatedException(
                $"未找到 Cookie 会话 CloudAccountUid={cloudAccountUid}",
                "cloud_drive_cookie_session_not_found");
        }

        return session;
    }

    private static string SerializeCookiePayload(
        CookieTriplet cookies,
        IDictionary<string, string>? extraFields)
    {
        var payload = new Dictionary<string, object>
        {
            ["uid"] = cookies.Uid,
            ["cid"] = cookies.Cid,
            ["ussid"] = cookies.Ussid,
        };

        if (extraFields is { Count: > 0 })
        {
            payload["extraFields"] = extraFields;
        }

        return JsonSerializer.Serialize(payload);
    }

    private static string AssembleCookieHeader(string payloadJson)
    {
        using JsonDocument doc = JsonDocument.Parse(payloadJson);
        JsonElement root = doc.RootElement;

        string uid = root.GetProperty("uid").GetString()!;
        string cid = root.GetProperty("cid").GetString()!;
        string ussid = root.GetProperty("ussid").GetString()!;

        var parts = new List<string>
        {
            $"UID={uid}",
            $"CID={cid}",
            $"USSID={ussid}",
        };

        if (root.TryGetProperty("extraFields", out JsonElement extras) &&
            extras.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty prop in extras.EnumerateObject())
            {
                parts.Add($"{prop.Name}={prop.Value.GetString()}");
            }
        }

        return string.Join("; ", parts);
    }
}

/// <summary>
/// Cookie 会话摘要（不暴露 Cookie 原文）。
/// </summary>
public sealed class CookieSessionSummary
{
    /// <summary>
    /// 115 账号唯一标识。
    /// </summary>
    public string CloudAccountUid { get; init; } = string.Empty;

    /// <summary>
    /// 会话状态。
    /// </summary>
    public CookieSessionStatus Status { get; init; }

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTime CreationTime { get; init; }

    /// <summary>
    /// 最后修改时间。
    /// </summary>
    public DateTime? LastModificationTime { get; init; }

    /// <summary>
    /// 冷却期截止时间。
    /// </summary>
    public DateTime? CooldownUntil { get; init; }
}
