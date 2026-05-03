using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveRecycleItemJsonConverter : JsonConverter<CloudDriveRecycleItemResponse>
{
    public override CloudDriveRecycleItemResponse Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        int? type = JsonElementReaders.GetInt32(root, "type");

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
