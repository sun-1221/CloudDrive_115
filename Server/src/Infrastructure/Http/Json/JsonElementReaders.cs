using System.Globalization;
using System.Text.Json;

namespace Infrastructure.Http.Json;

internal static class JsonElementReaders
{
    public static string? GetString(JsonElement element, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out JsonElement property))
            {
                return ReadString(property);
            }
        }

        return null;
    }

    public static int? GetInt32(JsonElement element, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out JsonElement property))
            {
                return ReadInt32(property);
            }
        }

        return null;
    }

    public static long? GetInt64(JsonElement element, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out JsonElement property))
            {
                return ReadInt64(property);
            }
        }

        return null;
    }

    public static bool? GetBoolean(JsonElement element, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out JsonElement property))
            {
                return ReadBoolean(property);
            }
        }

        return null;
    }

    public static string? ReadString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => element.GetRawText(),
        };
    }

    public static int? ReadInt32(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.Number => element.GetInt32(),
            JsonValueKind.String when string.IsNullOrWhiteSpace(element.GetString()) => null,
            JsonValueKind.String when int.TryParse(
                element.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int value) => value,
            _ => null,
        };
    }

    public static long? ReadInt64(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.Number => element.GetInt64(),
            JsonValueKind.String when string.IsNullOrWhiteSpace(element.GetString()) => null,
            JsonValueKind.String when long.TryParse(
                element.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long value) => value,
            _ => null,
        };
    }

    public static bool? ReadBoolean(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => element.GetInt64() != 0,
            JsonValueKind.String when string.IsNullOrWhiteSpace(element.GetString()) => null,
            JsonValueKind.String when bool.TryParse(element.GetString(), out bool value) => value,
            JsonValueKind.String when long.TryParse(
                element.GetString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long value) => value != 0,
            _ => null,
        };
    }
}
