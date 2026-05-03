using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;

using Domain.Entities;
using Domain.ValueObjects;

namespace Application.CloudDrive.Mappings;

/// <summary>
/// Cookie 会话相关手动映射。
/// </summary>
public static class CookieMapping
{
    /// <summary>
    /// 将 Cookie 会话聚合根映射为 DTO。
    /// </summary>
    public static CloudDriveCookieSessionDto ToDto(this CookieSession session)
    {
        return new CloudDriveCookieSessionDto
        {
            CloudAccountUid = session.CloudAccountUid,
            Status = session.Status,
            CreationTime = session.CreationTime,
            LastModificationTime = session.LastModificationTime,
            CooldownUntil = session.CooldownUntil,
        };
    }

    /// <summary>
    /// 将 Cookie 会话摘要映射为 DTO。
    /// </summary>
    public static CloudDriveCookieSessionDto ToDto(this CookieSessionSummary summary)
    {
        return new CloudDriveCookieSessionDto
        {
            CloudAccountUid = summary.CloudAccountUid,
            Status = summary.Status,
            CreationTime = summary.CreationTime,
            LastModificationTime = summary.LastModificationTime,
            CooldownUntil = summary.CooldownUntil,
        };
    }

    /// <summary>
    /// 将 Cookie 会话摘要映射为账号摘要 DTO。
    /// </summary>
    public static CloudDriveAccountSummaryDto ToDto(this CookieSessionSummary summary, bool includeOnlyAccountSummary)
    {
        return new CloudDriveAccountSummaryDto
        {
            CloudAccountUid = summary.CloudAccountUid,
            Status = summary.Status,
            CreationTime = summary.CreationTime,
            LastModificationTime = summary.LastModificationTime,
            CooldownUntil = summary.CooldownUntil,
        };
    }

    /// <summary>
    /// 将注入输入映射为 Cookie 三元组值对象。
    /// </summary>
    public static CookieTriplet ToEntity(this UpsertCookieSessionInput input)
    {
        return new CookieTriplet(input.Uid, input.Cid, input.Ussid);
    }
}
