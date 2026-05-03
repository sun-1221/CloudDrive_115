using System.Text.Json;
using System.Text.Json.Serialization;

using Infrastructure.Http.Json;

namespace Infrastructure.Http.Responses;

internal sealed class CloudDriveVideoUrlVariantListJsonConverter : JsonConverter<IReadOnlyList<CloudDriveVideoUrlVariantResponse>>
{
    public override IReadOnlyList<CloudDriveVideoUrlVariantResponse> Read(
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
            _ => throw new JsonException($"Cannot convert JSON token {root.ValueKind} to video URL list."),
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyList<CloudDriveVideoUrlVariantResponse> value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, CloudDrive115JsonSerializerOptions.Defaults);
    }

    private static IReadOnlyList<CloudDriveVideoUrlVariantResponse> ReadArray(JsonElement root)
    {
        List<CloudDriveVideoUrlVariantResponse> items = [];
        foreach (JsonElement element in root.EnumerateArray())
        {
            CloudDriveVideoUrlVariantResponse? item = element.Deserialize<CloudDriveVideoUrlVariantResponse>(
                CloudDrive115JsonSerializerOptions.Defaults);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    private static IReadOnlyList<CloudDriveVideoUrlVariantResponse> ReadObject(JsonElement root)
    {
        List<CloudDriveVideoUrlVariantResponse> items = [];
        foreach (JsonProperty property in root.EnumerateObject())
        {
            JsonElement value = property.Value;
            if (value.ValueKind is JsonValueKind.String)
            {
                items.Add(new CloudDriveVideoUrlVariantResponse
                {
                    DefinitionName = property.Name,
                    Url = value.GetString() ?? string.Empty,
                    Raw = value.Clone(),
                });
                continue;
            }

            if (value.ValueKind is JsonValueKind.Object)
            {
                CloudDriveVideoUrlVariantResponse? item = value.Deserialize<CloudDriveVideoUrlVariantResponse>(
                    CloudDrive115JsonSerializerOptions.Defaults);
                if (item is not null)
                {
                    items.Add(item);
                }
            }
        }

        return items;
    }
}
