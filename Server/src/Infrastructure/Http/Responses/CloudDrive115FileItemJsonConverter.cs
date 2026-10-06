using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 将 115 文件对象字段转换为统一文件条目模型。
/// </summary>
internal sealed class CloudDrive115FileItemJsonConverter : JsonConverter<CloudDrive115FileItem>
{
    /// <summary>
    /// 读取 JSON 并转换为目标响应模型。
    /// </summary>
    public override CloudDrive115FileItem Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        // 文件列表接口历史字段名差异很大，按 115 缩写、新接口名称、通用名称依次兼容。
        return new CloudDrive115FileItem
        {
            FileId = JsonElementReaders.GetString(root, "fid", "file_id", "id") ?? string.Empty,
            Name = JsonElementReaders.GetString(root, "n", "name", "file_name") ?? string.Empty,
            Size = JsonElementReaders.GetInt64(root, "s", "size", "file_size") ?? 0L,
            FileCategory = JsonElementReaders.GetInt32(root, "fc", "category"),
            IsDirectory = JsonElementReaders.GetBoolean(root, "is_dir", "is_directory"),
            PickCode = JsonElementReaders.GetString(root, "pc", "pick_code", "pickCode"),
            ThumbnailUrl = JsonElementReaders.GetString(root, "tu", "thumb", "thumb_url"),
            OriginalUrl = JsonElementReaders.GetString(root, "u", "original_url", "url"),
            VideoImageUrl = JsonElementReaders.GetString(root, "tp", "video_img"),
            IsVideo = JsonElementReaders.GetBoolean(root, "iv", "is_video"),
            VideoDefinition = JsonElementReaders.GetString(root, "fvs", "video_definition", "def2"),
            PlayDuration = JsonElementReaders.GetInt64(root, "play_long", "play_duration"),
            UploadTime = JsonElementReaders.GetInt64(root, "te", "file_ptime", "upload_time"),
            UpdateTime = JsonElementReaders.GetInt64(root, "t", "file_utime", "update_time"),
            IsStarred = JsonElementReaders.GetBoolean(root, "m", "is_starred", "star"),
            IsPrivate = JsonElementReaders.GetBoolean(root, "hdf", "is_private"),
            UploadStatus = JsonElementReaders.GetInt32(root, "sta", "file_status", "upload_status"),
            Sha1 = JsonElementReaders.GetString(root, "sha", "sha1"),
            Icon = JsonElementReaders.GetString(root, "ico", "icon"),
            FileClass = JsonElementReaders.GetString(root, "class", "file_class"),
            Raw = root.Clone(),
        };
    }

    /// <summary>
    /// 将目标响应模型写入 JSON。
    /// </summary>
    public override void Write(Utf8JsonWriter writer, CloudDrive115FileItem value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("fid", value.FileId);
        writer.WriteString("n", value.Name);
        writer.WriteNumber("s", value.Size);
        if (value.FileCategory.HasValue)
        {
            writer.WriteNumber("fc", value.FileCategory.Value);
        }

        if (value.PickCode is not null)
        {
            writer.WriteString("pc", value.PickCode);
        }

        writer.WriteEndObject();
    }
}
