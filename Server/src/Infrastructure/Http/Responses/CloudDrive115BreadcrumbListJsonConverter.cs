using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 将 115 面包屑数组或对象转换为列表。
/// </summary>
internal sealed class CloudDrive115BreadcrumbListJsonConverter : JsonConverter<IReadOnlyList<CloudDrive115BreadcrumbItem>>
{
    /// <summary>
    /// 读取 JSON 并转换为目标响应模型。
    /// </summary>
    public override IReadOnlyList<CloudDrive115BreadcrumbItem> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        return root.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => [],
            JsonValueKind.Array => ReadArray(root, options),
            JsonValueKind.Object => ReadObject(root, options),
            _ => throw new JsonException($"Cannot convert JSON token {root.ValueKind} to breadcrumb list."),
        };
    }

    /// <summary>
    /// 将目标响应模型写入 JSON。
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<CloudDrive115BreadcrumbItem> value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }

    /// <summary>
    /// 从 JSON 数组读取响应模型列表。
    /// </summary>
    private static IReadOnlyList<CloudDrive115BreadcrumbItem> ReadArray(JsonElement root, JsonSerializerOptions options)
    {
        List<CloudDrive115BreadcrumbItem> items = [];
        foreach (JsonElement element in root.EnumerateArray())
        {
            CloudDrive115BreadcrumbItem? item = element.Deserialize<CloudDrive115BreadcrumbItem>(options);
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
    private static IReadOnlyList<CloudDrive115BreadcrumbItem> ReadObject(JsonElement root, JsonSerializerOptions options)
    {
        List<CloudDrive115BreadcrumbItem> items = [];
        foreach (JsonProperty property in root.EnumerateObject())
        {
            // 面包屑有时以层级或目录 ID 作为对象 key，只有对象值才包含可反序列化的目录信息。
            if (property.Value.ValueKind is not JsonValueKind.Object)
            {
                continue;
            }

            CloudDrive115BreadcrumbItem? item = property.Value.Deserialize<CloudDrive115BreadcrumbItem>(options);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }
}
