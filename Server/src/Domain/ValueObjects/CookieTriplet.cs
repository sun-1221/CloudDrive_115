namespace Domain.ValueObjects;

/// <summary>
/// 115 Cookie 三元组值对象（UID/CID/USSID）。
/// </summary>
/// <param name="Uid">115 账号用户标识。</param>
/// <param name="Cid">115 Cookie 中的 CID。</param>
/// <param name="Ussid">115 Cookie 中的 USSID。</param>
public readonly record struct CookieTriplet(string Uid, string Cid, string Ussid);
