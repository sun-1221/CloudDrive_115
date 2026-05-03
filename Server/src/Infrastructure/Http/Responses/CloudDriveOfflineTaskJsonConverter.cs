using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 将 115 云下载任务对象转换为统一任务模型。
/// </summary>
internal sealed class CloudDriveOfflineTaskJsonConverter : JsonConverter<CloudDriveOfflineTaskResponse>
{
    /// <summary>
    /// 读取 JSON 并转换为目标响应模型。
    /// </summary>
    public override CloudDriveOfflineTaskResponse Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        // 云下载列表的新旧接口字段命名不一致，统一投影到应用层稳定模型。
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

    /// <summary>
    /// 将目标响应模型写入 JSON。
    /// </summary>
    public override void Write(Utf8JsonWriter writer, CloudDriveOfflineTaskResponse value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("info_hash", value.InfoHash);
        writer.WriteString("name", value.Name);
        writer.WriteNumber("size", value.Size);
        writer.WriteEndObject();
    }

    /// <summary>
    /// 按候选字段名读取十进制数值。
    /// </summary>
    private static decimal? ReadDecimal(JsonElement root, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (!root.TryGetProperty(propertyName, out JsonElement property))
            {
                continue;
            }

            // percentDone 可能是 JSON 数字，也可能是字符串百分比数值；统一用 invariant culture 解析。
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
