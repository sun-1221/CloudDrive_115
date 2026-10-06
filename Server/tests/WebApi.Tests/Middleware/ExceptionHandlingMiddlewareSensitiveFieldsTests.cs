using System.Text.Json;

using Application.Abstractions.CloudDrive;

using Domain.Exceptions;

using Microsoft.AspNetCore.Http;

using WebApi.Middleware;
using WebApi.Tests.Support;

namespace WebApi.Tests.Middleware;

public sealed class ExceptionHandlingMiddlewareSensitiveFieldsTests
{
    [Fact]
    public async Task InvokeAsync_WhenDomainExceptionContainsSensitiveFields_RedactsProblemDetails()
    {
        // Arrange
        var logger = new FakeLogger<ExceptionHandlingMiddleware>();
        var environment = new FakeHostEnvironment();
        RequestDelegate next = _ => throw new CloudDriveApiException(
            "upstream failed UID=123456; CID=abcdef; USSID=secret sign=signature");
        var middleware = new ExceptionHandlingMiddleware(next, logger, environment, new TestSensitiveFieldsFilter());

        var context = new DefaultHttpContext();
        context.Request.Path = "/api/cloud-drive/files";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Body.Position = 0;
        using JsonDocument document = await JsonDocument.ParseAsync(context.Response.Body);
        string body = document.RootElement.ToString();

        Assert.Contains("[REDACTED]", body);
        Assert.DoesNotContain("123456", body);
        Assert.DoesNotContain("abcdef", body);
        Assert.DoesNotContain("secret", body);
        Assert.DoesNotContain("signature", body);
    }

    private sealed class TestSensitiveFieldsFilter : ICloudDriveSensitiveFieldsFilter
    {
        public string Redact(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("123456", "[REDACTED]", StringComparison.Ordinal)
                .Replace("abcdef", "[REDACTED]", StringComparison.Ordinal)
                .Replace("secret", "[REDACTED]", StringComparison.Ordinal)
                .Replace("signature", "[REDACTED]", StringComparison.Ordinal);
        }

        public IReadOnlyDictionary<string, string?> RedactFields(IReadOnlyDictionary<string, string?> fields)
        {
            return fields.ToDictionary(
                pair => pair.Key,
                pair => pair.Value is null ? null : Redact(pair.Value),
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
