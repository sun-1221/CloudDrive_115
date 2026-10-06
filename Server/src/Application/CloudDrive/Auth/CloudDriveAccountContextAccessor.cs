using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;

namespace Application.CloudDrive.Auth;

/// <summary>
/// 请求作用域内的 115 账号上下文持有者（Scoped 生命周期）。
/// </summary>
internal sealed class CloudDriveAccountContextAccessor : ICloudDriveAccountContextAccessor
{
    /// <inheritdoc />
    public CloudDriveAccountContext? AccountContext { get; private set; }

    /// <inheritdoc />
    public string CloudAccountUid =>
        AccountContext?.CloudAccountUid
        ?? throw new InvalidOperationException(
            "115 账号上下文尚未解析，请确认请求已通过 CloudDriveAccountContextBehavior Pipeline。");

    /// <inheritdoc />
    public void SetContext(CloudDriveAccountContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        AccountContext = context;
    }
}
