using System.Globalization;
using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;

using Domain.Results;

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

        return new CloudDriveShareListDto
        {
            Items = [],
            Count = ReadInt32(source, "count"),
            DefineReceiveCode = ReadInt32(source, "define_receive_code"),
            BindReceiveCode = ReadInt32(source, "bind_receive_code"),
            Request = ReadString(source, "request"),
            ItemSchemaStatus = "not_collected",
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
}
