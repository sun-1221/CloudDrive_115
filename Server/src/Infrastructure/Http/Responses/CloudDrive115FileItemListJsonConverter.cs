using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDrive115FileItemListJsonConverter : JsonConverter<IReadOnlyList<CloudDrive115FileItem>>
{
    public override IReadOnlyList<CloudDrive115FileItem> Read(
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
            _ => throw new JsonException($"Cannot convert JSON token {root.ValueKind} to file item list."),
        };
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyList<CloudDrive115FileItem> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, CloudDrive115JsonSerializerOptions.Defaults);
    }

    private static IReadOnlyList<CloudDrive115FileItem> ReadArray(JsonElement root)
    {
        List<CloudDrive115FileItem> items = [];
        foreach (JsonElement element in root.EnumerateArray())
        {
            CloudDrive115FileItem? item = element.Deserialize<CloudDrive115FileItem>(
                CloudDrive115JsonSerializerOptions.Defaults);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    private static IReadOnlyList<CloudDrive115FileItem> ReadObject(JsonElement root)
    {
        List<CloudDrive115FileItem> items = [];
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (property.Value.ValueKind is not JsonValueKind.Object)
            {
                continue;
            }

            CloudDrive115FileItem? item = property.Value.Deserialize<CloudDrive115FileItem>(
                CloudDrive115JsonSerializerOptions.Defaults);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }
}
