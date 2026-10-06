using Application.Abstractions.CloudDrive;
using Application.CloudDrive;

using Infrastructure.Http;

using Microsoft.Extensions.Options;

namespace Infrastructure.Tests.Http;

/// <summary>
/// 115 网盘敏感字段脱敏过滤器测试。
/// </summary>
public sealed class CloudDriveSensitiveFieldsFilterTests
{
    /// <summary>
    /// 验证文本中包含 Cookie 和签名时会移除敏感值。
    /// </summary>
    [Fact]
    public void Redact_WhenTextContainsCookieAndSignature_RemovesSensitiveValues()
    {
        // Arrange（准备）
        ICloudDriveSensitiveFieldsFilter filter = CreateFilter();
        const string text = "Cookie: UID=123456; CID=abcdef; USSID=secret; sign=signature-value; path=/files";

        // Act（执行）
        string redacted = filter.Redact(text);

        // Assert（断言）
        Assert.Contains("UID=[REDACTED]", redacted);
        Assert.Contains("CID=[REDACTED]", redacted);
        Assert.Contains("USSID=[REDACTED]", redacted);
        Assert.Contains("sign=[REDACTED]", redacted);
        Assert.DoesNotContain("123456", redacted);
        Assert.DoesNotContain("abcdef", redacted);
        Assert.DoesNotContain("secret", redacted);
        Assert.DoesNotContain("signature-value", redacted);
    }

    /// <summary>
    /// 验证字段字典包含敏感键时会脱敏对应值。
    /// </summary>
    [Fact]
    public void RedactFields_WhenDictionaryContainsSensitiveKeys_RedactsValues()
    {
        // Arrange（准备）
        ICloudDriveSensitiveFieldsFilter filter = CreateFilter();
        var fields = new Dictionary<string, string?>
        {
            ["uid"] = "123456",
            ["SignatureKey"] = "secret-sign-key",
            ["endpoint"] = "/files",
        };

        // Act（执行）
        IReadOnlyDictionary<string, string?> redacted = filter.RedactFields(fields);

        // Assert（断言）
        Assert.Equal("[REDACTED]", redacted["uid"]);
        Assert.Equal("[REDACTED]", redacted["SignatureKey"]);
        Assert.Equal("/files", redacted["endpoint"]);
    }

    /// <summary>
    /// 验证用户资料隐私字段会全部脱敏。
    /// </summary>
    [Fact]
    public void RedactFields_WhenUserProfileContainsPrivacyFields_RedactsAllBlacklistedValues()
    {
        // Arrange（准备）
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

        // Act（执行）
        IReadOnlyDictionary<string, string?> redacted = filter.RedactFields(fields);

        // Assert（断言）
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

    /// <summary>
    /// 验证异常文本中嵌套的敏感字段会被脱敏。
    /// </summary>
    [Fact]
    public void Redact_WhenExceptionTextContainsSensitiveFields_RedactsNestedValues()
    {
        // Arrange（准备）
        ICloudDriveSensitiveFieldsFilter filter = CreateFilter();
        const string text = "Problem sign=abc123 pub_mobile=13800000000 encryptedCookiePayload=payload SignatureKey=secret";

        // Act（执行）
        string redacted = filter.Redact(text);

        // Assert（断言）
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
