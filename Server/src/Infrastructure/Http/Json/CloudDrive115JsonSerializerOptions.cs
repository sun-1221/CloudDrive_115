using System.Text.Json;

namespace Infrastructure.Http.Json;

internal static class CloudDrive115JsonSerializerOptions
{
    public static JsonSerializerOptions Defaults { get; } = CreateDefaults();

    private static JsonSerializerOptions CreateDefaults()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false,
        };
    }
}
