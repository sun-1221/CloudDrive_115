using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveVideoUrlVariantJsonConverter : JsonConverter<CloudDriveVideoUrlVariantResponse>
{
    public override CloudDriveVideoUrlVariantResponse Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        if (root.ValueKind is JsonValueKind.String)
        {
            return new CloudDriveVideoUrlVariantResponse
            {
                Url = root.GetString() ?? string.Empty,
                Raw = root.Clone(),
            };
        }

        return new CloudDriveVideoUrlVariantResponse
        {
            Definition = JsonElementReaders.GetInt32(root, "definition", "def", "type", "quality"),
            DefinitionName = JsonElementReaders.GetString(root, "name", "title", "definition_name"),
            Url = JsonElementReaders.GetString(root, "url", "video_url") ?? string.Empty,
            Size = JsonElementReaders.GetInt64(root, "size", "file_size"),
            Raw = root.Clone(),
        };
    }

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
