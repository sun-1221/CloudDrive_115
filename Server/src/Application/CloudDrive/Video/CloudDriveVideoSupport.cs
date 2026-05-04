using System.Globalization;
using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;

using Domain.Results;

namespace Application.CloudDrive.Video;

internal static class CloudDriveVideoSupport
{
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "mp4",
        "mkv",
        "mov",
        "avi",
        "wmv",
        "flv",
        "webm",
        "m4v",
        "ts",
        "mpeg",
        "mpg",
    };

    public static readonly Error PickCodeRequired = new(
        "cloud_drive_video_pick_code_required",
        "缺少 PickCode，无法调用 115 视频接口。");

    public static readonly Error NotVideoFile = new(
        "cloud_drive_video_file_type_invalid",
        "当前文件不是视频文件。");

    public static readonly Error TimeInvalid = new(
        "cloud_drive_video_progress_time_invalid",
        "播放进度不能小于 0。");

    public static CloudDriveApiContext CreateVideoPlayContext(string pickCode, int? definition)
    {
        Dictionary<string, string> queryParameters = new(StringComparer.Ordinal)
        {
            ["pickcode"] = pickCode,
            ["share_id"] = "0",
            ["local"] = "1",
            ["format"] = "json",
        };

        if (definition.HasValue)
        {
            queryParameters["definition"] = definition.Value.ToString(CultureInfo.InvariantCulture);
        }

        return new CloudDriveApiContext
        {
            Category = "video",
            QueryParameters = queryParameters,
        };
    }

    public static CloudDriveApiContext CreateTranscodeContext(string pickCode, string op)
    {
        return new CloudDriveApiContext
        {
            Category = "video",
            QueryParameters = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["pickcode"] = pickCode,
                ["op"] = string.IsNullOrWhiteSpace(op) ? "vip_push" : op,
                ["format"] = "json",
            },
        };
    }

    public static CloudDriveApiContext CreateSubtitleContext(string pickCode)
    {
        return new CloudDriveApiContext
        {
            Category = "video",
            QueryParameters = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["pickcode"] = pickCode,
                ["format"] = "json",
            },
        };
    }

    public static CloudDriveApiContext CreateProgressReadContext(string pickCode)
    {
        return new CloudDriveApiContext
        {
            Category = "video-history",
            QueryParameters = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["pick_code"] = pickCode,
                ["fetch"] = "one",
                ["category"] = "1",
                ["format"] = "json",
            },
        };
    }

    public static CloudDriveApiContext CreateProgressWriteContext(RememberVideoPlayProgressInput input, string pickCode)
    {
        return new CloudDriveApiContext
        {
            Category = "video-history",
            FormFields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["pick_code"] = pickCode,
                ["time"] = input.Time.ToString(CultureInfo.InvariantCulture),
                ["category"] = "1",
                ["op"] = input.WatchEnd ? "end" : "update",
            },
        };
    }

    public static bool IsVideo(CloudDriveFileDetailDto file)
    {
        if (file.IsVideo)
        {
            return true;
        }

        string extension = string.IsNullOrWhiteSpace(file.FileType)
            ? Path.GetExtension(file.Name).TrimStart('.')
            : file.FileType.TrimStart('.');

        return VideoExtensions.Contains(extension);
    }

    public static CloudDriveVideoPlayDto MapVideoPlay(JsonElement response, int? selectedDefinition)
    {
        return new CloudDriveVideoPlayDto
        {
            Status = VideoPlayStatus.SchemaNotCollected,
            QualityOptions = [],
            AudioTracks = [],
            PlaybackSchemaStatus = "not_collected",
            IsTemporary = true,
            UsageRestrictionNotice = "视频播放成功响应 schema 尚未采集，暂不返回播放地址。",
        };
    }

    public static CloudDriveSubtitleListDto MapSubtitles(JsonElement response)
    {
        return new CloudDriveSubtitleListDto
        {
            AutoloadSubtitle = null,
            Items = [],
            ItemSchemaStatus = "not_collected",
        };
    }

    public static CloudDriveVideoPlayProgressDto MapProgress(JsonElement response, string pickCode)
    {
        JsonElement source = GetPayload(response);

        return new CloudDriveVideoPlayProgressDto
        {
            FileId = ReadString(source, "file_id"),
            FileName = ReadString(source, "file_name"),
            Hash = ReadString(source, "hash"),
            PickCode = ReadString(source, "pick_code") ?? ReadString(source, "pickcode") ?? pickCode,
            PlayedSeconds = ReadInt64(source, "time")
                ?? ReadInt64(source, "played_seconds")
                ?? ReadInt64(source, "watch_time"),
            AddTime = ReadInt64(source, "add_time"),
            SchemaStatus = "available",
        };
    }

    public static string? ReadString(JsonElement source, string propertyName)
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

    public static int? ReadInt32(JsonElement source, string propertyName)
    {
        long? value = ReadInt64(source, propertyName);
        return value.HasValue ? checked((int)Math.Min(value.Value, int.MaxValue)) : null;
    }

    public static long? ReadInt64(JsonElement source, string propertyName)
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

    private static JsonElement GetPayload(JsonElement response)
    {
        if (response.ValueKind == JsonValueKind.Object
            && response.TryGetProperty("data", out JsonElement data)
            && data.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
        {
            return data;
        }

        return response;
    }

}
