using System.Globalization;
using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;

using Domain.Results;

namespace Application.CloudDrive.Share;

internal static class CloudDriveShareSupport
{
    public static readonly Error ShareSampleUnavailable = new(
        "share_sample_unavailable",
        "分享接口当前缺少可用成功样本，待联调补齐字段映射。");

    public static CloudDriveApiContext CreateListContext(GetShareListInput input)
    {
        return new CloudDriveApiContext
        {
            Category = "share",
            QueryParameters = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["offset"] = input.Offset.ToString(CultureInfo.InvariantCulture),
                ["limit"] = input.Limit.ToString(CultureInfo.InvariantCulture),
                ["format"] = "json",
            },
        };
    }

    public static PagedResultDto<TShare> MapList<TShare>(
        JsonElement response,
        GetShareListInput input,
        Func<CloudDriveShareItemDto, TShare> convert)
    {
        JsonElement source = GetPayload(response);
        if (!source.TryGetProperty("list", out JsonElement list)
            || list.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Share list response does not contain a list array.");
        }

        List<TShare> items = [];
        foreach (JsonElement item in list.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object)
            {
                items.Add(convert(MapItem(item)));
            }
        }

        return new PagedResultDto<TShare>
        {
            Items = items,
            TotalCount = ReadInt64(source, "count") ?? items.Count,
            SkipCount = input.Offset,
            MaxResultCount = input.Limit,
        };
    }

    public static CloudDriveShareSendItemDto ToSendItem(CloudDriveShareItemDto item)
    {
        return new CloudDriveShareSendItemDto
        {
            ShareCode = item.ShareCode,
            ShareTitle = item.ShareTitle,
            FileId = item.FileId,
            FileName = item.FileName,
            PickCode = item.PickCode,
            FileSize = item.FileSize,
            CreateTime = item.CreateTime,
            ExpireTime = item.ExpireTime,
            Url = item.Url,
        };
    }

    public static CloudDriveShareReceiveItemDto ToReceiveItem(CloudDriveShareItemDto item)
    {
        return new CloudDriveShareReceiveItemDto
        {
            ShareCode = item.ShareCode,
            ShareTitle = item.ShareTitle,
            FileId = item.FileId,
            FileName = item.FileName,
            PickCode = item.PickCode,
            FileSize = item.FileSize,
            CreateTime = item.CreateTime,
            ExpireTime = item.ExpireTime,
            Url = item.Url,
        };
    }

    private static CloudDriveShareItemDto MapItem(JsonElement item)
    {
        return new CloudDriveShareItemDto
        {
            ShareCode = ReadString(item, "share_code") ?? ReadString(item, "share_id"),
            ShareTitle = ReadString(item, "share_title") ?? ReadString(item, "title"),
            FileId = ReadString(item, "file_id") ?? ReadString(item, "fid"),
            FileName = ReadString(item, "file_name") ?? ReadString(item, "n"),
            PickCode = ReadString(item, "pick_code") ?? ReadString(item, "pickcode"),
            FileSize = ReadInt64(item, "file_size") ?? ReadInt64(item, "size"),
            CreateTime = ReadInt64(item, "create_time"),
            ExpireTime = ReadInt64(item, "expire_time"),
            Url = ReadString(item, "url"),
        };
    }

    private static JsonElement GetPayload(JsonElement response)
    {
        if (response.ValueKind == JsonValueKind.Object
            && response.TryGetProperty("data", out JsonElement data)
            && data.ValueKind == JsonValueKind.Object)
        {
            return data;
        }

        return response;
    }

    private static string? ReadString(JsonElement source, string propertyName)
    {
        if (source.ValueKind != JsonValueKind.Object
            || !source.TryGetProperty(propertyName, out JsonElement value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null,
        };
    }

    private static long? ReadInt64(JsonElement source, string propertyName)
    {
        if (source.ValueKind != JsonValueKind.Object
            || !source.TryGetProperty(propertyName, out JsonElement value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String
            && long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long stringNumber)
                ? stringNumber
                : null;
    }
}
