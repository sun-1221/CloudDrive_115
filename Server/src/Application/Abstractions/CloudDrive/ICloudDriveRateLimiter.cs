namespace Application.Abstractions.CloudDrive;

/// <summary>
/// 115 网盘频控限流抽象。基于令牌桶算法与有界排队/拒绝策略，
/// 按用户 + 115 账号 + 场景维度限流。
/// </summary>
/// <remarks>
/// 定义在 Application 层，基础设施层由 <c>CloudDriveRateLimiter</c> 实现。
/// 单机部署使用进程内内存状态；多实例部署时可替换为分布式限流方案。
/// </remarks>
public interface ICloudDriveRateLimiter
{
    /// <summary>
    /// 获取执行许可。根据 <see cref="CloudDriveAppConfig.RateLimitOverflowStrategy"/> 选择排队或立即拒绝。
    /// 返回 <see cref="IDisposable"/> 许可，调用方通过 <c>using</c> 释放。
    /// </summary>
    Task<IDisposable> AcquireAsync(
        Guid userId,
        string cloudAccountUid,
        string? category = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录 API 返回的频控信号（如 HTTP 429），触发退避。
    /// </summary>
    void RecordThrottleSignal(Guid userId, string cloudAccountUid, string? category = null);
}
