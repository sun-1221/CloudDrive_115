namespace Infrastructure.Http;

/// <summary>
/// 115 Cookie 登录态探测结果。
/// </summary>
internal sealed record LoginStatusProbeResult(
    bool? IsLoggedIn,
    string? FailureReason);
