using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDrive115BreadcrumbListJsonConverter : JsonConverter<IReadOnlyList<CloudDrive115BreadcrumbItem>>
{
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

    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<CloudDrive115BreadcrumbItem> value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }

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

    private static IReadOnlyList<CloudDrive115BreadcrumbItem> ReadObject(JsonElement root, JsonSerializerOptions options)
    {
        List<CloudDrive115BreadcrumbItem> items = [];
        foreach (JsonProperty property in root.EnumerateObject())
        {
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
