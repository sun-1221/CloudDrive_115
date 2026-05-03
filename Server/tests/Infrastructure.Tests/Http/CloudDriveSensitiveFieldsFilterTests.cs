using Application.Abstractions.CloudDrive;
using Application.CloudDrive;

using Infrastructure.Http;

using Microsoft.Extensions.Options;

namespace Infrastructure.Tests.Http;

public sealed class CloudDriveSensitiveFieldsFilterTests
{
    [Fact]
    public void Redact_WhenTextContainsCookieAndSignature_RemovesSensitiveValues()
    {
        // 准备
        ICloudDriveSensitiveFieldsFilter filter = CreateFilter();
        const string text = "Cookie: UID=123456; CID=abcdef; USSID=secret; sign=signature-value; path=/files";

        // 执行
        string redacted = filter.Redact(text);

        // 断言
        Assert.Contains("UID=[REDACTED]", redacted);
        Assert.Contains("CID=[REDACTED]", redacted);
        Assert.Contains("USSID=[REDACTED]", redacted);
        Assert.Contains("sign=[REDACTED]", redacted);
        Assert.DoesNotContain("123456", redacted);
        Assert.DoesNotContain("abcdef", redacted);
        Assert.DoesNotContain("secret", redacted);
        Assert.DoesNotContain("signature-value", redacted);
    }

    [Fact]
    public void RedactFields_WhenDictionaryContainsSensitiveKeys_RedactsValues()
    {
        // 准备
        ICloudDriveSensitiveFieldsFilter filter = CreateFilter();
        var fields = new Dictionary<string, string?>
        {
            ["uid"] = "123456",
            ["SignatureKey"] = "secret-sign-key",
            ["endpoint"] = "/files",
        };

        // 执行
        IReadOnlyDictionary<string, string?> redacted = filter.RedactFields(fields);

        // 断言
        Assert.Equal("[REDACTED]", redacted["uid"]);
        Assert.Equal("[REDACTED]", redacted["SignatureKey"]);
        Assert.Equal("/files", redacted["endpoint"]);
    }

    [Fact]
    public void RedactFields_WhenUserProfileContainsPrivacyFields_RedactsAllBlacklistedValues()
    {
        // 准备
        ICloudDriveSensitiveFieldsFilter filter = CreateFilter();
        var fields = new Dictionary<string, string?>
        {
            ["pub_mobile"] = "13800000000",
            ["pub_email"] = "user@example.com",
            ["alipay"] = "pay-account",
            ["address"] = "secret-address",
            ["birthday"] = "2000-01-01",
            ["gender"] = "secret-gender",
            ["education"] = "secret-education",
            ["like_music"] = "secret-music",
            ["nickname"] = "public-name",
        };

        // 执行
        IReadOnlyDictionary<string, string?> redacted = filter.RedactFields(fields);

        // 断言
        Assert.Equal("[REDACTED]", redacted["pub_mobile"]);
        Assert.Equal("[REDACTED]", redacted["pub_email"]);
        Assert.Equal("[REDACTED]", redacted["alipay"]);
        Assert.Equal("[REDACTED]", redacted["address"]);
        Assert.Equal("[REDACTED]", redacted["birthday"]);
        Assert.Equal("[REDACTED]", redacted["gender"]);
        Assert.Equal("[REDACTED]", redacted["education"]);
        Assert.Equal("[REDACTED]", redacted["like_music"]);
        Assert.Equal("public-name", redacted["nickname"]);
    }

    [Fact]
    public void Redact_WhenExceptionTextContainsSensitiveFields_RedactsNestedValues()
    {
        // 准备
        ICloudDriveSensitiveFieldsFilter filter = CreateFilter();
        const string text = "Problem sign=abc123 pub_mobile=13800000000 encryptedCookiePayload=payload SignatureKey=secret";

        // 执行
        string redacted = filter.Redact(text);

        // 断言
        Assert.DoesNotContain("abc123", redacted);
        Assert.DoesNotContain("13800000000", redacted);
        Assert.DoesNotContain("payload", redacted);
        Assert.DoesNotContain("secret", redacted);
        Assert.Contains("sign=[REDACTED]", redacted);
        Assert.Contains("pub_mobile=[REDACTED]", redacted);
    }

    private static ICloudDriveSensitiveFieldsFilter CreateFilter()
    {
        return new CloudDriveSensitiveFieldsFilter(Options.Create(new CloudDriveAppConfig
        {
            CookieEncryptionKeyName = "CookieKey",
            SignatureKeyName = "SignatureKey",
        }));
    }
}
