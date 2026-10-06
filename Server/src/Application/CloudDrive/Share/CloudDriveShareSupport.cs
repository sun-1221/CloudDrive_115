using System.Globalization;
using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;

namespace Application.CloudDrive.Share;

internal static class CloudDriveShareSupport
{
    public static CloudDriveApiContext CreateListContext(GetShareListInput input)
    {
        return new CloudDriveApiContext
        {
            Category = "share",
            QueryParameters = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["offset"] = input.Offset.ToString(CultureInfo.InvariantCulture),
                ["limit"] = input.Limit.ToString(CultureInfo.InvariantCulture),
                ["format"] = "json",
            },
        };
    }

    public static CloudDriveShareListDto MapList(JsonElement response)
    {
        JsonElement source = GetPayload(response);

        IReadOnlyList<CloudDriveShareItemDto> items = MapItems(source);

        return new CloudDriveShareListDto
        {
            Items = items,
            Count = ReadInt32(source, "count"),
            DefineReceiveCode = ReadInt32(source, "define_receive_code"),
            BindReceiveCode = ReadInt32(source, "bind_receive_code"),
            Request = ReadString(source, "request"),
            ItemSchemaStatus = "available",
        };
    }

    private static IReadOnlyList<CloudDriveShareItemDto> MapItems(JsonElement source)
    {
        if (source.ValueKind != JsonValueKind.Object
            || !source.TryGetProperty("list", out JsonElement list)
            || list.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        List<CloudDriveShareItemDto> items = [];
        foreach (JsonElement item in list.EnumerateArray())
        {
            items.Add(MapItem(item));
        }

        return items;
    }

    private static CloudDriveShareItemDto MapItem(JsonElement item)
    {
        long? createTime = ReadInt64(item, "create_time");
        long? expireTime = ReadInt64(item, "expire_time");

        return new CloudDriveShareItemDto
        {
            ShareCode = ReadString(item, "share_code") ?? string.Empty,
            ShareTitle = ReadString(item, "share_title") ?? string.Empty,
            FileId = ReadString(item, "file_id") ?? string.Empty,
            FileName = ReadString(item, "file_name") ?? string.Empty,
            PickCode = ReadString(item, "pick_code"),
            FileSize = ReadInt64(item, "file_size"),
            CreateTime = createTime.HasValue && createTime.Value > 0
                ? DateTimeOffset.FromUnixTimeSeconds(createTime.Value).UtcDateTime
                : null,
            ExpireTime = expireTime.HasValue && expireTime.Value > 0
                ? DateTimeOffset.FromUnixTimeSeconds(expireTime.Value).UtcDateTime
                : null,
            Url = ReadString(item, "url"),
            SchemaStatus = "available",
        };
    }

    private static JsonElement GetPayload(JsonElement response)
    {
        if (response.ValueKind == JsonValueKind.Object
            && response.TryGetProperty("data", out JsonElement data)
            && data.ValueKind == JsonValueKind.Object)
        {
            return data;
        }

        return response;
    }

    private static string? ReadString(JsonElement source, string propertyName)
    {
        if (source.ValueKind != JsonValueKind.Object
            || !source.TryGetProperty(propertyName, out JsonElement value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null,
        };
    }

    private static int? ReadInt32(JsonElement source, string propertyName)
    {
        if (source.ValueKind != JsonValueKind.Object
            || !source.TryGetProperty(propertyName, out JsonElement value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String
            && int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int stringNumber)
                ? stringNumber
                : null;
    }

    private static long? ReadInt64(JsonElement source, string propertyName)
    {
        if (source.ValueKind != JsonValueKind.Object
            || !source.TryGetProperty(propertyName, out JsonElement value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String
            && long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long stringNumber)
                ? stringNumber
                : null;
    }
}
