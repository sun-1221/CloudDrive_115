using Application.Abstractions.CloudDrive;

using Microsoft.AspNetCore.DataProtection;

namespace Infrastructure.Security;

internal sealed class DataProtectionCloudDriveSecretProtector(
    IDataProtectionProvider dataProtectionProvider) : ICloudDriveSecretProtector
{
    public string Protect(string keyName, string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyName);
        ArgumentException.ThrowIfNullOrEmpty(plaintext);

        return CreateProtector(keyName).Protect(plaintext);
    }

    public string Unprotect(string keyName, string ciphertext, int? keyVersion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyName);
        ArgumentException.ThrowIfNullOrEmpty(ciphertext);

        return CreateProtector(keyName, keyVersion).Unprotect(ciphertext);
    }

    private IDataProtector CreateProtector(string keyName, int? keyVersion = null)
    {
        string purpose = keyVersion.HasValue
            ? $"CloudDrive.Secret.{keyName}.v{keyVersion.Value}"
            : $"CloudDrive.Secret.{keyName}.v1";

        return dataProtectionProvider.CreateProtector(purpose);
    }
}
