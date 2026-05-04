using System.Diagnostics.Metrics;

namespace Infrastructure.Diagnostics;

internal static class CloudDriveMetrics
{
    internal const string MeterName = "CloudDrive.Infrastructure";

    private static readonly Meter Meter = new(MeterName, "1.0.0");
    private static readonly Counter<long> RateLimitQueuedCounter =
        Meter.CreateCounter<long>("cloud_drive_rate_limit_queued_total");
    private static readonly Counter<long> RateLimitRejectedCounter =
        Meter.CreateCounter<long>("cloud_drive_rate_limit_rejected_total");
    private static readonly Counter<long> RateLimitThrottleSignalCounter =
        Meter.CreateCounter<long>("cloud_drive_rate_limit_throttle_signals_total");
    private static readonly Counter<long> HttpRetryCounter =
        Meter.CreateCounter<long>("cloud_drive_http_retry_total");
    private static readonly Counter<long> ApiFailureCounter =
        Meter.CreateCounter<long>("cloud_drive_api_failure_total");

    public static void RecordRateLimitQueued(string category)
    {
        RateLimitQueuedCounter.Add(
            1,
            new KeyValuePair<string, object?>("category", NormalizeTag(category)));
    }

    public static void RecordRateLimitRejected(string category)
    {
        RateLimitRejectedCounter.Add(
            1,
            new KeyValuePair<string, object?>("category", NormalizeTag(category)));
    }

    public static void RecordRateLimitThrottleSignal(string category)
    {
        RateLimitThrottleSignalCounter.Add(
            1,
            new KeyValuePair<string, object?>("category", NormalizeTag(category)));
    }

    public static void RecordHttpRetry(string clientName)
    {
        HttpRetryCounter.Add(
            1,
            new KeyValuePair<string, object?>("client", NormalizeTag(clientName)));
    }

    public static void RecordApiFailure(string clientName, string host, string failureType)
    {
        ApiFailureCounter.Add(
            1,
            new KeyValuePair<string, object?>("client", NormalizeTag(clientName)),
            new KeyValuePair<string, object?>("host", NormalizeTag(host)),
            new KeyValuePair<string, object?>("failure_type", NormalizeTag(failureType)));
    }

    private static string NormalizeTag(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "unknown"
            : value.Trim().ToLowerInvariant();
    }
}
