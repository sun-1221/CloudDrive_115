using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveOfflineTaskJsonConverter : JsonConverter<CloudDriveOfflineTaskResponse>
{
    public override CloudDriveOfflineTaskResponse Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        return new CloudDriveOfflineTaskResponse
        {
            InfoHash = JsonElementReaders.GetString(root, "info_hash", "hash") ?? string.Empty,
            Name = JsonElementReaders.GetString(root, "name") ?? string.Empty,
            Size = JsonElementReaders.GetInt64(root, "size") ?? 0L,
            AddTime = JsonElementReaders.GetInt64(root, "add_time"),
            LastUpdate = JsonElementReaders.GetInt64(root, "last_update"),
            PercentDone = ReadDecimal(root, "percentDone", "percent_done"),
            Url = JsonElementReaders.GetString(root, "url"),
            FileId = JsonElementReaders.GetString(root, "file_id"),
            ParentId = JsonElementReaders.GetString(root, "wp_path_id", "parent_id"),
            VideoDefinition = JsonElementReaders.GetString(root, "def2", "video_definition"),
            PlayDuration = JsonElementReaders.GetInt64(root, "play_long"),
            Status = JsonElementReaders.GetInt32(root, "status"),
            Raw = root.Clone(),
        };
    }

    public override void Write(Utf8JsonWriter writer, CloudDriveOfflineTaskResponse value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("info_hash", value.InfoHash);
        writer.WriteString("name", value.Name);
        writer.WriteNumber("size", value.Size);
        writer.WriteEndObject();
    }

    private static decimal? ReadDecimal(JsonElement root, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (!root.TryGetProperty(propertyName, out JsonElement property))
            {
                continue;
            }

            if (property.ValueKind is JsonValueKind.Number && property.TryGetDecimal(out decimal decimalValue))
            {
                return decimalValue;
            }

            if (property.ValueKind is JsonValueKind.String
                && decimal.TryParse(property.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimalValue))
            {
                return decimalValue;
            }
        }

        return null;
    }
}
