namespace Application.Abstractions.CloudDrive;

/// <summary>
/// Cookie 失效通知抽象。用于发布 Cookie 失效事件与运维告警。
/// </summary>
/// <remarks>
/// 定义在 Application 层，默认实现通过结构化日志输出脱敏告警信息；
/// 可扩展为发送运维通知、触发前端重新绑定引导等。
/// </remarks>
public interface ICookieInvalidationNotifier
{
    /// <summary>
    /// 通知 Cookie 失效。
    /// </summary>
    /// <param name="userId">系统用户 ID。</param>
    /// <param name="cloudAccountUid">115 账号唯一标识。</param>
    /// <param name="reason">失效原因（供日志与审计）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task NotifyAsync(Guid userId, string cloudAccountUid, string reason, CancellationToken cancellationToken = default);
}
