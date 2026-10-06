namespace Application.Abstractions.CloudDrive;

/// <summary>
/// 标记接口：声明此 MediatR 请求需要 115 账号上下文。
/// <see cref="Behaviors.CloudDriveAccountContextBehavior{TRequest,TResponse}"/> 会在 Pipeline 中
/// 为实现此接口的请求自动解析账号上下文并写入 <see cref="ICloudDriveAccountContextAccessor"/>。
/// </summary>
public interface ICloudDriveAccountScopedRequest
{
    /// <summary>
    /// 系统用户 ID。
    /// </summary>
    Guid UserId { get; }
}
