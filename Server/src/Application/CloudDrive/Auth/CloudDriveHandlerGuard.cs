using Domain.Exceptions;

namespace Application.CloudDrive.Auth;

internal static class CloudDriveHandlerGuard
{
    public static void EnsureEnabled(CloudDriveAppConfig config)
    {
        if (!config.CloudDriveFeatureEnabled)
        {
            throw new CloudDriveConfigurationException();
        }
    }
}
