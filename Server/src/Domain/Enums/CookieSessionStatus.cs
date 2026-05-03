namespace Domain.Enums;

/// <summary>
/// Cookie 会话状态。
/// </summary>
public enum CookieSessionStatus
{
    /// <summary>会话有效，可用于 API 调用。</summary>
    Active = 0,

    /// <summary>会话已失效（Cookie 过期或被服务端拒绝），进入冷却期后等待运维重新注入。</summary>
    Invalidated = 1,

    /// <summary>会话已被主动撤销（运维手动解绑）。</summary>
    Revoked = 2
}
