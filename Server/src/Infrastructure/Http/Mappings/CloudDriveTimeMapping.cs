namespace Infrastructure.Http.Mappings;

internal static class CloudDriveTimeMapping
{
    public static DateTime FromUnixSeconds(long? timestamp)
    {
        return timestamp.HasValue && timestamp.Value > 0
            ? DateTimeOffset.FromUnixTimeSeconds(timestamp.Value).UtcDateTime
            : default;
    }

    public static DateTime? FromUnixSecondsOrNull(long? timestamp)
    {
        return timestamp.HasValue && timestamp.Value > 0
            ? DateTimeOffset.FromUnixTimeSeconds(timestamp.Value).UtcDateTime
            : null;
    }
}
