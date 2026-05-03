using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveVideoDefinitionListJsonConverter : JsonConverter<IReadOnlyList<CloudDriveVideoDefinitionResponse>>
{
    public override IReadOnlyList<CloudDriveVideoDefinitionResponse> Read(
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
            _ => throw new JsonException($"Cannot convert JSON token {root.ValueKind} to video definition list."),
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<CloudDriveVideoDefinitionResponse> value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, CloudDrive115JsonSerializerOptions.Defaults);
    }

    private static IReadOnlyList<CloudDriveVideoDefinitionResponse> ReadArray(JsonElement root)
    {
        List<CloudDriveVideoDefinitionResponse> items = [];
        foreach (JsonElement element in root.EnumerateArray())
        {
            CloudDriveVideoDefinitionResponse? item = element.Deserialize<CloudDriveVideoDefinitionResponse>(
                CloudDrive115JsonSerializerOptions.Defaults);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    private static IReadOnlyList<CloudDriveVideoDefinitionResponse> ReadObject(JsonElement root)
    {
        List<CloudDriveVideoDefinitionResponse> items = [];
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (int.TryParse(property.Name, out int definition))
            {
                items.Add(new CloudDriveVideoDefinitionResponse
                {
                    Definition = definition,
                    Name = property.Value.ValueKind is JsonValueKind.String ? property.Value.GetString() : null,
                });
            }
        }

        return items;
    }
}
