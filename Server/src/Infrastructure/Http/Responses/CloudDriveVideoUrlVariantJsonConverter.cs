using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

/// <summary>
/// 将 115 视频地址对象转换为统一地址模型。
/// </summary>
internal sealed class CloudDriveVideoUrlVariantJsonConverter : JsonConverter<CloudDriveVideoUrlVariantResponse>
{
    /// <summary>
    /// 读取 JSON 并转换为目标响应模型。
    /// </summary>
    public override CloudDriveVideoUrlVariantResponse Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        if (root.ValueKind is JsonValueKind.String)
        {
            // 部分视频接口直接把清晰度地址返回成字符串，不能强制按对象读取。
            return new CloudDriveVideoUrlVariantResponse
            {
                Url = root.GetString() ?? string.Empty,
                Raw = root.Clone(),
            };
        }

        return new CloudDriveVideoUrlVariantResponse
        {
            Definition = JsonElementReaders.GetInt32(root, "definition", "def", "type", "quality"),
            DefinitionNew = JsonElementReaders.GetInt32(root, "definition_n", "definitionNew"),
            DefinitionName = JsonElementReaders.GetString(root, "name", "title", "definition_name"),
            Url = JsonElementReaders.GetString(root, "url", "video_url") ?? string.Empty,
            Size = JsonElementReaders.GetInt64(root, "size", "file_size"),
            Height = JsonElementReaders.GetInt32(root, "height"),
            Width = JsonElementReaders.GetInt32(root, "width"),
            Raw = root.Clone(),
        };
    }

    /// <summary>
    /// 将目标响应模型写入 JSON。
    /// </summary>
    public override void Write(Utf8JsonWriter writer, CloudDriveVideoUrlVariantResponse value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("url", value.Url);
        if (value.Definition.HasValue)
        {
            writer.WriteNumber("definition", value.Definition.Value);
        }

        writer.WriteEndObject();
    }
}
