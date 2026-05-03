using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 将 115 视频清晰度数组或对象转换为统一列表。
/// </summary>
internal sealed class CloudDriveVideoDefinitionListJsonConverter : JsonConverter<IReadOnlyList<CloudDriveVideoDefinitionResponse>>
{
    /// <summary>
    /// 读取 JSON 并转换为目标响应模型。
    /// </summary>
    public override IReadOnlyList<CloudDriveVideoDefinitionResponse> Read(
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
            _ => throw new JsonException($"Cannot convert JSON token {root.ValueKind} to video definition list."),
        };
    }

    /// <summary>
    /// 将目标响应模型写入 JSON。
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<CloudDriveVideoDefinitionResponse> value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, CloudDrive115JsonSerializerOptions.Defaults);
    }

    /// <summary>
    /// 从 JSON 数组读取响应模型列表。
    /// </summary>
    private static IReadOnlyList<CloudDriveVideoDefinitionResponse> ReadArray(JsonElement root)
    {
        List<CloudDriveVideoDefinitionResponse> items = [];
        foreach (JsonElement element in root.EnumerateArray())
        {
            CloudDriveVideoDefinitionResponse? item = element.Deserialize<CloudDriveVideoDefinitionResponse>(
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
    private static IReadOnlyList<CloudDriveVideoDefinitionResponse> ReadObject(JsonElement root)
    {
        List<CloudDriveVideoDefinitionResponse> items = [];
        foreach (JsonProperty property in root.EnumerateObject())
        {
            // 对象形态用清晰度数字作为 key，例如 {"1":"标清"}，需要把 key 还原为 Definition。
            if (int.TryParse(property.Name, out int definition))
            {
                items.Add(new CloudDriveVideoDefinitionResponse
                {
                    Definition = definition,
                    Name = property.Value.ValueKind is JsonValueKind.String ? property.Value.GetString() : null,
                });
            }
        }

        return items;
    }
}
