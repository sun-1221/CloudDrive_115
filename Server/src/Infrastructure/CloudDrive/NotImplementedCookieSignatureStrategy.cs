using Application.Abstractions.CloudDrive;

namespace Infrastructure.CloudDrive;

internal sealed class NotImplementedCookieSignatureStrategy : ICookieSignatureStrategy
{
    public CookieSignature Generate(Guid userId, string cloudAccountUid, string requestBody)
    {
        return default;
    }
}
