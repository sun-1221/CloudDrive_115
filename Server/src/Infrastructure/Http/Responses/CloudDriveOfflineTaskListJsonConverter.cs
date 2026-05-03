using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 将 115 云下载任务数组或对象转换为统一列表。
/// </summary>
internal sealed class CloudDriveOfflineTaskListJsonConverter : JsonConverter<IReadOnlyList<CloudDriveOfflineTaskResponse>>
{
    /// <summary>
    /// 读取 JSON 并转换为目标响应模型。
    /// </summary>
    public override IReadOnlyList<CloudDriveOfflineTaskResponse> Read(
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
            _ => throw new JsonException($"Cannot convert JSON token {root.ValueKind} to offline task list."),
        };
    }

    /// <summary>
    /// 将目标响应模型写入 JSON。
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<CloudDriveOfflineTaskResponse> value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, CloudDrive115JsonSerializerOptions.Defaults);
    }

    /// <summary>
    /// 从 JSON 数组读取响应模型列表。
    /// </summary>
    private static IReadOnlyList<CloudDriveOfflineTaskResponse> ReadArray(JsonElement root)
    {
        List<CloudDriveOfflineTaskResponse> items = [];
        foreach (JsonElement element in root.EnumerateArray())
        {
            CloudDriveOfflineTaskResponse? item = element.Deserialize<CloudDriveOfflineTaskResponse>(
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
    private static IReadOnlyList<CloudDriveOfflineTaskResponse> ReadObject(JsonElement root)
    {
        List<CloudDriveOfflineTaskResponse> items = [];
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (property.Value.ValueKind is not JsonValueKind.Object)
            {
                continue;
            }

            CloudDriveOfflineTaskResponse? item = property.Value.Deserialize<CloudDriveOfflineTaskResponse>(
                CloudDrive115JsonSerializerOptions.Defaults);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }
}
