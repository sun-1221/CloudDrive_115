using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 将 115 回收站条目数组或对象转换为统一列表。
/// </summary>
internal sealed class CloudDriveRecycleItemListJsonConverter : JsonConverter<IReadOnlyList<CloudDriveRecycleItemResponse>>
{
    /// <summary>
    /// 读取 JSON 并转换为目标响应模型。
    /// </summary>
    public override IReadOnlyList<CloudDriveRecycleItemResponse> Read(
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
            _ => throw new JsonException($"Cannot convert JSON token {root.ValueKind} to recycle item list."),
        };
    }

    /// <summary>
    /// 将目标响应模型写入 JSON。
    /// </summary>
    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<CloudDriveRecycleItemResponse> value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, CloudDrive115JsonSerializerOptions.Defaults);
    }

    /// <summary>
    /// 从 JSON 数组读取响应模型列表。
    /// </summary>
    private static IReadOnlyList<CloudDriveRecycleItemResponse> ReadArray(JsonElement root)
    {
        List<CloudDriveRecycleItemResponse> items = [];
        foreach (JsonElement element in root.EnumerateArray())
        {
            CloudDriveRecycleItemResponse? item = element.Deserialize<CloudDriveRecycleItemResponse>(
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
    private static IReadOnlyList<CloudDriveRecycleItemResponse> ReadObject(JsonElement root)
    {
        List<CloudDriveRecycleItemResponse> items = [];
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (property.Value.ValueKind is not JsonValueKind.Object)
            {
                continue;
            }

            CloudDriveRecycleItemResponse? item = property.Value.Deserialize<CloudDriveRecycleItemResponse>(
                CloudDrive115JsonSerializerOptions.Defaults);
            if (item is null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.Id))
            {
                // 字典形态响应会把文件 ID 放在属性名里，条目对象本身可能不再重复 id 字段。
                item = new CloudDriveRecycleItemResponse
                {
                    Id = property.Name,
                    FileName = item.FileName,
                    Type = item.Type,
                    IsDirectory = item.IsDirectory,
                    FileSize = item.FileSize,
                    DeletedTime = item.DeletedTime,
                    ThumbnailUrl = item.ThumbnailUrl,
                    OriginalParentId = item.OriginalParentId,
                    OriginalParentName = item.OriginalParentName,
                    PickCode = item.PickCode,
                    RestoreStatus = item.RestoreStatus,
                    Raw = item.Raw,
                };
            }

            items.Add(item);
        }

        return items;
    }
}
