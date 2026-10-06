using Application.CloudDrive.Dtos;

using Domain.Results;

namespace Application.Abstractions.CloudDrive;

/// <summary>
/// 解析当前用户唯一可用的 115 账号上下文。
/// </summary>
public interface ICloudDriveAccountContextResolver
{
    /// <summary>
    /// 根据系统用户解析唯一 Active Cookie 会话。
    /// </summary>
    /// <param name="userId">系统用户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>解析成功时返回账号上下文；否则返回业务错误。</returns>
    Task<Result<CloudDriveAccountContext>> ResolveAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
