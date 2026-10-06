using Application.CloudDrive;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.DependencyInjection;

internal sealed class CloudDriveAppConfigPostConfigureOptions(
    ILogger<CloudDriveAppConfigPostConfigureOptions> logger) : IPostConfigureOptions<CloudDriveAppConfig>
{
    public void PostConfigure(string? name, CloudDriveAppConfig options)
    {
        ArgumentNullException.ThrowIfNull(options);

        IReadOnlyList<string> missingFields = options.ValidateStartupConfiguration();
        if (missingFields.Count == 0)
        {
            return;
        }

        options.CloudDriveFeatureEnabled = false;
        logger.LogError(
            "115 网盘模块配置不完整，已禁用 CloudDriveFeatureEnabled。MissingFields={MissingFields}",
            string.Join(',', missingFields));
    }
}
