using Application.Abstractions.CloudDrive;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

/// <summary>
/// 文件管理器。按 <c>UserId + CloudAccountUid</c> 上下文编排文件操作的缓存协作与 API 调用
/// （缓存优先读取、写操作后缓存失效等）。
/// </summary>
/// <remarks>
/// 普通类 + 构造函数注入。调用 <see cref="ICloudDriveOpenApiGateway"/> 时仅需传递
/// <c>UserId + CloudAccountUid</c>；网关内部从 <c>CookieSessionManager</c> 获取当前 Cookie 并注入请求头。
/// Cookie 方案不携带 <c>AppId</c> 维度。
/// </remarks>
public sealed class CloudDriveFileManager(
    ICloudDriveMetadataCache metadataCache,
    ICloudDriveOpenApiGateway openApiGateway,
    ICloudDriveRateLimiter rateLimiter,
    IOptions<CloudDriveAppConfig> configOptions,
    ILogger<CloudDriveFileManager> logger)
{
    /// <summary>
    /// 获取文件列表（缓存优先）。
    /// </summary>
    /// <typeparam name="T">文件列表结果类型。</typeparam>
    /// <param name="userId">系统用户 ID。</param>
    /// <param name="cloudAccountUid">115 账号唯一标识。</param>
    /// <param name="directoryId">目录 ID。</param>
    /// <param name="context">API 请求上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>文件列表结果。</returns>
    public async Task<T> GetFileListAsync<T>(
        Guid userId,
        string cloudAccountUid,
        string directoryId,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default)
    {
        CloudDriveAppConfig config = configOptions.Value;
        string cacheKey = BuildDirectoryCacheKey(userId, cloudAccountUid, directoryId, "list");

        T? result = await metadataCache.GetOrSetAsync(cacheKey, async token =>
        {
            using IDisposable permit = await rateLimiter.AcquireAsync(
                userId, cloudAccountUid, context.Category, token);

            return await openApiGateway.SendWebApiRequestAsync<T>(
                userId, cloudAccountUid, HttpMethod.Get, "files", context, token);
        }, TimeSpan.FromSeconds(config.CacheTtlSeconds), cancellationToken);

        return result!;
    }

    /// <summary>
    /// 获取文件详情（缓存优先）。
    /// </summary>
    /// <typeparam name="T">文件详情结果类型。</typeparam>
    /// <param name="userId">系统用户 ID。</param>
    /// <param name="cloudAccountUid">115 账号唯一标识。</param>
    /// <param name="fileId">文件 ID。</param>
    /// <param name="context">API 请求上下文。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>文件详情结果；files/file 接口尚未采集时返回 default。</returns>
    public Task<T?> GetFileDetailAsync<T>(
        Guid userId,
        string cloudAccountUid,
        string fileId,
        CloudDriveApiContext context,
        CancellationToken cancellationToken = default)
    {
        // 网页端详情接口（files/file）待补采集，不得使用猜测路径调用 115 API。
        // 调用方（Preview Handler 等）需处理 null 返回值。
        logger.LogDebug(
            "文件详情无法获取，files/file 接口尚未采集 FileId={FileId}",
            fileId);

        return Task.FromResult(default(T));
    }

    /// <summary>
    /// 执行写操作并使受影响目录缓存失效。
    /// 统一上传、创建文件夹、移动、复制、删除、重命名、还原等写路径的缓存失效编排。
    /// </summary>
    /// <typeparam name="T">操作结果类型。</typeparam>
    /// <param name="userId">系统用户 ID。</param>
    /// <param name="cloudAccountUid">115 账号唯一标识。</param>
    /// <param name="affectedDirectoryIds">受影响的目录 ID 集合。</param>
    /// <param name="action">执行的写操作。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>写操作结果。</returns>
    public async Task<T> ExecuteWithCacheInvalidationAsync<T>(
        Guid userId,
        string cloudAccountUid,
        string[] affectedDirectoryIds,
        Func<Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        T result = await action();

        foreach (string directoryId in affectedDirectoryIds)
        {
            try
            {
                await metadataCache.InvalidateDirectoryAsync(userId, cloudAccountUid, directoryId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "缓存失效失败 UserId={UserId}, CloudAccountUid={CloudAccountUid}, DirectoryId={DirectoryId}",
                    userId, cloudAccountUid, directoryId);
            }
        }

        return result;
    }

    private static string BuildDirectoryCacheKey(
        Guid userId, string cloudAccountUid, string directoryId, string suffix) =>
        $"cd:{userId}:{cloudAccountUid}:dir:{directoryId}:{suffix}";

    private static string BuildFileCacheKey(
        Guid userId, string cloudAccountUid, string fileId, string suffix) =>
        $"cd:{userId}:{cloudAccountUid}:file:{fileId}:{suffix}";
}
