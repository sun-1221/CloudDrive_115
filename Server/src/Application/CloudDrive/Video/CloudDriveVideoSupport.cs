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
        JsonElement source = GetPayload(response);
        List<VideoQualityOption> qualityOptions = ReadQualityOptions(source);
        string? status = ReadString(source, "status");
        string? restrictionReason = ReadString(source, "restriction_reason")
            ?? ReadString(source, "restriction")
            ?? ReadString(source, "reason")
            ?? DetectVipRestriction(source, qualityOptions);

        return new CloudDriveVideoPlayDto
        {
            Status = IsTranscodingStatus(status) || qualityOptions.Count == 0
                ? VideoPlayStatus.Transcoding
                : VideoPlayStatus.Ready,
            QualityOptions = qualityOptions,
            AudioTracks = ReadAudioTracks(source),
            Duration = checked((int)Math.Min(ReadInt64(source, "play_long")
                ?? ReadInt64(source, "duration")
                ?? 0, int.MaxValue)),
            UserDef = ReadInt32(source, "user_def") ?? ReadInt32(source, "definition"),
            SelectedDefinition = selectedDefinition ?? ReadInt32(source, "definition"),
            UserRotate = ReadInt32(source, "user_rotate"),
            UserTurn = ReadInt32(source, "user_turn"),
            RestrictionReason = restrictionReason,
            PlaybackMetadata = ReadMetadata(source),
            IsTemporary = true,
            UsageRestrictionNotice = "播放资源仅限当前已绑定用户在本系统内短时使用，不得视为永久公开外链。",
        };
    }

    public static CloudDriveSubtitleListDto MapSubtitles(JsonElement response)
    {
        JsonElement source = GetPayload(response);
        List<CloudDriveSubtitleDto> autoload = ReadSubtitleArray(source, "autoload");
        List<CloudDriveSubtitleDto> items = autoload
            .Concat(ReadSubtitleArray(source, "list"))
            .ToList();

        return new CloudDriveSubtitleListDto
        {
            AutoloadSubtitle = autoload.FirstOrDefault(),
            Items = items,
        };
    }

    public static CloudDriveVideoPlayProgressDto MapProgress(JsonElement response, string pickCode)
    {
        JsonElement source = GetPayload(response);

        return new CloudDriveVideoPlayProgressDto
        {
            FileId = ReadString(source, "file_id") ?? string.Empty,
            FileName = ReadString(source, "file_name") ?? string.Empty,
            Hash = ReadString(source, "hash") ?? string.Empty,
            PickCode = ReadString(source, "pick_code") ?? ReadString(source, "pickcode") ?? pickCode,
            PlayedSeconds = ReadInt64(source, "time")
                ?? ReadInt64(source, "played_seconds")
                ?? ReadInt64(source, "watch_time")
                ?? 0,
            AddTime = ReadInt64(source, "add_time"),
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

    private static List<VideoQualityOption> ReadQualityOptions(JsonElement source)
    {
        if (!source.TryGetProperty("video_url", out JsonElement value))
        {
            return [];
        }

        List<VideoQualityOption> result = [];
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in value.EnumerateArray())
            {
                AddQualityOption(result, item);
            }
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in value.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    result.Add(new VideoQualityOption
                    {
                        Definition = ParseDefinition(property.Name),
                        PlayUrl = property.Value.GetString() ?? string.Empty,
                    });
                }
                else
                {
                    AddQualityOption(result, property.Value, ParseDefinition(property.Name));
                }
            }
        }

        return result.Where(option => !string.IsNullOrWhiteSpace(option.PlayUrl)).ToList();
    }

    private static void AddQualityOption(List<VideoQualityOption> result, JsonElement item, int fallbackDefinition = 0)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        result.Add(new VideoQualityOption
        {
            Definition = ReadInt32(item, "definition") ?? fallbackDefinition,
            DefinitionNew = ReadInt32(item, "definition_n") ?? ReadInt32(item, "definition_new"),
            Title = ReadString(item, "title") ?? ReadString(item, "name") ?? ReadString(item, "definition_name"),
            PlayUrl = ReadString(item, "url") ?? ReadString(item, "play_url") ?? string.Empty,
            Height = ReadInt32(item, "height"),
            Width = ReadInt32(item, "width"),
        });
    }

    private static IReadOnlyList<AudioTrackOption> ReadAudioTracks(JsonElement source)
    {
        if (!source.TryGetProperty("multitrack_list", out JsonElement value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        List<AudioTrackOption> result = [];
        int fallbackIndex = 0;
        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            result.Add(new AudioTrackOption
            {
                TrackIndex = ReadInt32(item, "track_index") ?? ReadInt32(item, "index") ?? fallbackIndex,
                Title = ReadString(item, "title") ?? ReadString(item, "name") ?? string.Empty,
                IsSelected = ReadBoolean(item, "selected") ?? ReadBoolean(item, "is_selected") ?? false,
            });
            fallbackIndex++;
        }

        return result;
    }

    private static List<CloudDriveSubtitleDto> ReadSubtitleArray(JsonElement source, string propertyName)
    {
        if (!source.TryGetProperty(propertyName, out JsonElement value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        List<CloudDriveSubtitleDto> result = [];
        foreach (JsonElement item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            result.Add(new CloudDriveSubtitleDto
            {
                SubtitleId = ReadString(item, "sid") ?? ReadString(item, "id") ?? string.Empty,
                Language = ReadString(item, "language") ?? ReadString(item, "lang") ?? string.Empty,
                Title = ReadString(item, "title") ?? ReadString(item, "file_name") ?? string.Empty,
                SubtitleUrl = ReadString(item, "url") ?? ReadString(item, "subtitle_url") ?? string.Empty,
                Type = ReadString(item, "type"),
                Sha1 = ReadString(item, "sha1"),
                FileId = ReadString(item, "file_id"),
                FileName = ReadString(item, "file_name"),
                PickCode = ReadString(item, "pick_code") ?? ReadString(item, "pickcode"),
                CaptionMapId = ReadString(item, "caption_map_id"),
                IsCaptionMap = ReadBoolean(item, "is_caption_map"),
                SyncTime = ReadInt32(item, "sync_time"),
            });
        }

        return result;
    }

    private static Dictionary<string, string>? ReadMetadata(JsonElement source)
    {
        if (source.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            ["pickcode"] = ReadString(source, "pickcode") ?? ReadString(source, "pick_code") ?? string.Empty,
        };

        string? fileName = ReadString(source, "file_name");
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            metadata["file_name"] = fileName;
        }

        return metadata;
    }

    private static bool? ReadBoolean(JsonElement source, string propertyName)
    {
        if (source.ValueKind != JsonValueKind.Object
            || !source.TryGetProperty(propertyName, out JsonElement value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetInt32(out int number) => number != 0,
            JsonValueKind.String when bool.TryParse(value.GetString(), out bool parsed) => parsed,
            JsonValueKind.String when int.TryParse(value.GetString(), out int number) => number != 0,
            _ => null,
        };
    }

    private static bool IsTranscodingStatus(string? status)
    {
        return !string.IsNullOrWhiteSpace(status)
            && status.Contains("transcod", StringComparison.OrdinalIgnoreCase);
    }

    private static string? DetectVipRestriction(JsonElement source, IReadOnlyList<VideoQualityOption> qualityOptions)
    {
        string text = source.GetRawText();
        if (text.Contains("upgrade", StringComparison.OrdinalIgnoreCase)
            || text.Contains("vip", StringComparison.OrdinalIgnoreCase)
            || text.Contains("会员", StringComparison.Ordinal))
        {
            return "当前清晰度受会员等级限制。";
        }

        if (qualityOptions.Any(option => option.Definition is 5 or 100
            && string.IsNullOrWhiteSpace(option.PlayUrl)))
        {
            return "4K 或原画播放地址暂不可用。";
        }

        return null;
    }

    private static int ParseDefinition(string value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int definition)
            ? definition
            : 0;
    }
}
