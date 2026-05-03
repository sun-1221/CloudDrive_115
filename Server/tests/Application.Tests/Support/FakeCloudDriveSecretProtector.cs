using Application.Abstractions.CloudDrive;

namespace Application.Tests.Support;

/// <summary>
/// 测试用机密保护器。使用 Base64 作为伪加密以便于验证。
/// </summary>
internal sealed class FakeCloudDriveSecretProtector : ICloudDriveSecretProtector
{
    public const string Prefix = "PROTECTED:";

    public string Protect(string keyName, string plaintext) =>
        Prefix + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plaintext));

    public string Unprotect(string keyName, string ciphertext, int? keyVersion = null)
    {
        if (!ciphertext.StartsWith(Prefix))
            throw new InvalidOperationException("Invalid protected text");

        return System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(ciphertext[Prefix.Length..]));
    }
}
