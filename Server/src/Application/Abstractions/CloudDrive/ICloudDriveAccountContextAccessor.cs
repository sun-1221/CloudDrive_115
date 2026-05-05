using Application.CloudDrive.Dtos;

namespace Application.Abstractions.CloudDrive;

/// <summary>
/// 提供当前请求作用域内已解析的 115 账号上下文。
/// 由 <see cref="Behaviors.CloudDriveAccountContextBehavior{TRequest,TResponse}"/> 在 Pipeline 中填充，
/// Handler 通过注入此接口获取当前账号 UID，无需从输入 DTO 读取。
/// </summary>
public interface ICloudDriveAccountContextAccessor
{
    /// <summary>
    /// 获取当前请求的 115 账号上下文；未解析时为 <see langword="null"/>。
    /// </summary>
    CloudDriveAccountContext? AccountContext { get; }

    /// <summary>
    /// 获取当前请求的 115 账号 UID。
    /// </summary>
    /// <exception cref="InvalidOperationException">账号上下文未解析。</exception>
    string CloudAccountUid { get; }

    /// <summary>
    /// 设置当前请求的账号上下文。仅由 Pipeline Behavior 调用。
    /// </summary>
    /// <param name="context">已解析的账号上下文。</param>
    void SetContext(CloudDriveAccountContext context);
}
