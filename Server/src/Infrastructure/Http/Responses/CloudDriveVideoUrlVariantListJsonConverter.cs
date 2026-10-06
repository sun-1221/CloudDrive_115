using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 将 115 视频地址数组或对象转换为统一列表。
/// </summary>
internal sealed class CloudDriveVideoUrlVariantListJsonConverter : JsonConverter<IReadOnlyList<CloudDriveVideoUrlVariantResponse>>
{
    /// <summary>
    /// 读取 JSON 并转换为目标响应模型。
    /// </summary>
    public override IReadOnlyList<CloudDriveVideoUrlVariantResponse> Read(
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
            _ => throw new JsonException($"Cannot convert JSON token {root.ValueKind} to video URL list."),
        };
    }

    /// <summary>
    /// 将目标响应模型写入 JSON。
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<CloudDriveVideoUrlVariantResponse> value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, CloudDrive115JsonSerializerOptions.Defaults);
    }

    /// <summary>
    /// 从 JSON 数组读取响应模型列表。
    /// </summary>
    private static IReadOnlyList<CloudDriveVideoUrlVariantResponse> ReadArray(JsonElement root)
    {
        List<CloudDriveVideoUrlVariantResponse> items = [];
        foreach (JsonElement element in root.EnumerateArray())
        {
            CloudDriveVideoUrlVariantResponse? item = element.Deserialize<CloudDriveVideoUrlVariantResponse>(
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
    private static IReadOnlyList<CloudDriveVideoUrlVariantResponse> ReadObject(JsonElement root)
    {
        List<CloudDriveVideoUrlVariantResponse> items = [];
        foreach (JsonProperty property in root.EnumerateObject())
        {
            JsonElement value = property.Value;
            if (value.ValueKind is JsonValueKind.String)
            {
                // 对象形态可能是 "高清":"url" 的轻量映射，属性名就是清晰度名称。
                items.Add(new CloudDriveVideoUrlVariantResponse
                {
                    DefinitionName = property.Name,
                    Url = value.GetString() ?? string.Empty,
                    Raw = value.Clone(),
                });
                continue;
            }

            if (value.ValueKind is JsonValueKind.Object)
            {
                CloudDriveVideoUrlVariantResponse? item = value.Deserialize<CloudDriveVideoUrlVariantResponse>(
                    CloudDrive115JsonSerializerOptions.Defaults);
                if (item is not null)
                {
                    items.Add(item);
                }
            }
        }

        return items;
    }
}
