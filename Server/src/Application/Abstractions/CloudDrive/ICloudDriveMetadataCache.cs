namespace Application.Abstractions.CloudDrive;

/// <summary>
/// 115 网盘元数据缓存抽象。按用户 + 115 账号 + 目录/文件维度缓存文件列表与详情。
/// </summary>
/// <remarks>
/// 定义在 Application 层，基础设施层由 <c>CloudDriveMetadataCache</c> 实现
/// （内部使用 <c>IDistributedCache</c> + <c>DistributedCacheExtensions</c>）。
/// Application 层不直接引用 <c>IDistributedCache</c>，遵循 Ports &amp; Adapters 模式。
/// </remarks>
public interface ICloudDriveMetadataCache
{
    /// <summary>
    /// 获取或设置缓存。缓存未命中时调用 <paramref name="factory"/> 获取数据并写入缓存。
    /// </summary>
    /// <returns>命中时返回缓存值；未命中时返回工厂生成并写入缓存的值。</returns>
    Task<T?> GetOrSetAsync<T>(
        string cacheKey,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 使指定目录缓存失效。
    /// </summary>
    /// <returns>表示异步目录缓存失效操作的 <see cref="Task"/>。</returns>
    Task InvalidateDirectoryAsync(
        Guid userId,
        string cloudAccountUid,
        string directoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 使指定文件缓存失效。
    /// </summary>
    /// <returns>表示异步文件缓存失效操作的 <see cref="Task"/>。</returns>
    Task InvalidateFileAsync(
        Guid userId,
        string cloudAccountUid,
        string fileId,
        CancellationToken cancellationToken = default);
}
