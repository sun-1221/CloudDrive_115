using Application.CloudDrive;

using Domain.Entities;
using Domain.Enums;

using Infrastructure.Http;
using Infrastructure.Jobs;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Tests.Jobs;

public sealed class CloudDriveJobAuditLoggerTests
{
    [Fact]
    public void LogState_WhenJobContainsSensitiveCheckpointAndError_RedactsAuditLog()
    {
        // Arrange
        var logger = new FakeLogger<CloudDriveJobAuditLogger>();
        var auditLogger = new CloudDriveJobAuditLogger(
            new CloudDriveSensitiveFieldsFilter(Options.Create(new CloudDriveAppConfig
            {
                CookieEncryptionKeyName = "CookieKey",
                SignatureKeyName = "SignatureKey"
            })),
            logger);
        CloudDriveJob job = CloudDriveJob.Create(
            Guid.NewGuid(),
            "uid=115001; CID=secret-cid; USSID=secret-ussid",
            CloudDriveJobType.Upload,
            payloadJson: """{"sign":"secret-sign"}""");
        job.UpdateCheckpoint("""{"UID":"secret-uid","sign":"secret-sign","safe":"value"}""");
        job.Fail("failed sign=secret-sign USSID=secret-ussid");

        // Act
        auditLogger.LogState(job, "checkpoint");

        // Assert
        FakeLogEntry entry = Assert.Single(logger.Entries);
        string combined = string.Join(
            ' ',
            entry.Message,
            string.Join(' ', entry.State.Values.Select(value => value?.ToString())));
        Assert.DoesNotContain("secret-cid", combined);
        Assert.DoesNotContain("secret-ussid", combined);
        Assert.DoesNotContain("secret-sign", combined);
        Assert.DoesNotContain("secret-uid", combined);
        Assert.Contains("[REDACTED]", combined);
        Assert.Contains("value", combined);
    }

    private sealed class FakeLogger<TCategory> : ILogger<TCategory>
    {
        private readonly List<FakeLogEntry> _entries = [];

        public IReadOnlyList<FakeLogEntry> Entries => _entries;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Dictionary<string, object?> values = new(StringComparer.Ordinal);
            if (state is IReadOnlyList<KeyValuePair<string, object?>> kvps)
            {
                foreach (KeyValuePair<string, object?> kvp in kvps)
                {
                    values[kvp.Key] = kvp.Value;
                }
            }

            _entries.Add(new FakeLogEntry(
                logLevel,
                formatter(state, exception),
                values,
                exception));
        }
    }

    private sealed record FakeLogEntry(
        LogLevel Level,
        string Message,
        IReadOnlyDictionary<string, object?> State,
        Exception? Exception);
}
