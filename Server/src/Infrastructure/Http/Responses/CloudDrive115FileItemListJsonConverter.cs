using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 将 115 文件列表数组或对象转换为统一列表。
/// </summary>
internal sealed class CloudDrive115FileItemListJsonConverter : JsonConverter<IReadOnlyList<CloudDrive115FileItem>>
{
    /// <summary>
    /// 读取 JSON 并转换为目标响应模型。
    /// </summary>
    public override IReadOnlyList<CloudDrive115FileItem> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        return root.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => [],
            JsonValueKind.Array => ReadArray(root),
            JsonValueKind.Object => ReadObject(root),
            _ => throw new JsonException($"Cannot convert JSON token {root.ValueKind} to file item list."),
        };
    }

    /// <summary>
    /// 将目标响应模型写入 JSON。
    /// </summary>
    public override void Write(Utf8JsonWriter writer, IReadOnlyList<CloudDrive115FileItem> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, CloudDrive115JsonSerializerOptions.Defaults);
    }

    /// <summary>
    /// 从 JSON 数组读取响应模型列表。
    /// </summary>
    private static IReadOnlyList<CloudDrive115FileItem> ReadArray(JsonElement root)
    {
        List<CloudDrive115FileItem> items = [];
        foreach (JsonElement element in root.EnumerateArray())
        {
            CloudDrive115FileItem? item = element.Deserialize<CloudDrive115FileItem>(
                CloudDrive115JsonSerializerOptions.Defaults);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    /// <summary>
    /// 从 JSON 对象读取响应模型列表。
    /// </summary>
    private static IReadOnlyList<CloudDrive115FileItem> ReadObject(JsonElement root)
    {
        List<CloudDrive115FileItem> items = [];
        foreach (JsonProperty property in root.EnumerateObject())
        {
            // 115 有些列表会返回以文件 ID 为属性名的对象字典，非对象属性不是文件条目。
            if (property.Value.ValueKind is not JsonValueKind.Object)
            {
                continue;
            }

            CloudDrive115FileItem? item = property.Value.Deserialize<CloudDrive115FileItem>(
                CloudDrive115JsonSerializerOptions.Defaults);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }
}
