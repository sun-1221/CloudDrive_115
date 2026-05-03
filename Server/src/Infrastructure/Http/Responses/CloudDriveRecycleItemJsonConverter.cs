using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 将 115 回收站条目对象转换为统一条目模型。
/// </summary>
internal sealed class CloudDriveRecycleItemJsonConverter : JsonConverter<CloudDriveRecycleItemResponse>
{
    /// <summary>
    /// 读取 JSON 并转换为目标响应模型。
    /// </summary>
    public override CloudDriveRecycleItemResponse Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        int? type = JsonElementReaders.GetInt32(root, "type");

        // 回收站接口 type=2 表示目录，和文件列表 fc=0 的目录规则不同，不能复用文件列表判断。
        return new CloudDriveRecycleItemResponse
        {
            Id = JsonElementReaders.GetString(root, "id", "fid", "file_id") ?? string.Empty,
            FileName = JsonElementReaders.GetString(root, "file_name", "n", "name") ?? string.Empty,
            Type = type,
            IsDirectory = type == 2,
            FileSize = JsonElementReaders.GetInt64(root, "file_size", "size", "s") ?? 0L,
            DeletedTime = JsonElementReaders.GetInt64(root, "dtime", "deleted_time"),
            ThumbnailUrl = JsonElementReaders.GetString(root, "thumb_url", "tu", "thumb"),
            OriginalParentId = JsonElementReaders.GetString(root, "cid", "parent_id"),
            OriginalParentName = JsonElementReaders.GetString(root, "parent_name"),
            PickCode = JsonElementReaders.GetString(root, "pick_code", "pc"),
            RestoreStatus = JsonElementReaders.GetInt32(root, "status"),
            Raw = root.Clone(),
        };
    }

    /// <summary>
    /// 将目标响应模型写入 JSON。
    /// </summary>
    public override void Write(Utf8JsonWriter writer, CloudDriveRecycleItemResponse value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("id", value.Id);
        writer.WriteString("file_name", value.FileName);
        if (value.Type.HasValue)
        {
            writer.WriteNumber("type", value.Type.Value);
        }

        writer.WriteNumber("file_size", value.FileSize);
        writer.WriteEndObject();
    }
}
