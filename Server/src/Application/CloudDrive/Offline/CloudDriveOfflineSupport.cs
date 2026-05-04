using System.Globalization;
using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;

using Domain.Exceptions;
using Domain.Results;

namespace Application.CloudDrive.Offline;

internal static class CloudDriveOfflineSupport
{
    public static readonly Error OfflineSignatureUnavailable = new(
        "offline_signature_not_available",
        "云下载签名服务未配置，暂无法执行云下载操作。");

    public static CloudDriveApiContext CreateTaskListContext(GetOfflineTaskListInput input)
    {
        return new CloudDriveApiContext
        {
            Category = "offline",
            FormFields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["page"] = input.Page.ToString(CultureInfo.InvariantCulture),
                ["stat"] = input.Stat.ToString(CultureInfo.InvariantCulture),
            },
        };
    }

    public static CloudDriveApiContext CreateQuotaContext()
    {
        return new CloudDriveApiContext
        {
            Category = "offline",
        };
    }

    public static CloudDriveApiContext CreateStatsContext()
    {
        return new CloudDriveApiContext
        {
            Category = "offline",
            FormFields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["stat"] = "0",
            },
        };
    }

    public static CloudDriveApiContext CreateAddTaskContext(AddOfflineTaskInput input)
    {
        Dictionary<string, string> formFields = new(StringComparer.Ordinal)
        {
            ["url"] = input.Urls.Count == 1 ? input.Urls[0] : string.Join('\n', input.Urls),
        };

        if (!string.IsNullOrWhiteSpace(input.SavePath))
        {
            formFields["wp_path_id"] = input.SavePath;
        }

        return new CloudDriveApiContext
        {
            Category = "offline-write",
            FormFields = formFields,
        };
    }

    public static CloudDriveApiContext CreateCancelTaskContext(CancelOfflineTaskInput input)
    {
        return new CloudDriveApiContext
        {
            Category = "offline-write",
            FormFields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["hash[0]"] = input.InfoHash,
            },
        };
    }

    public static CloudDriveApiContext CreateClearTasksContext(ClearOfflineTasksInput input)
    {
        Dictionary<string, string> formFields = new(StringComparer.Ordinal);
        if (input.Stat.HasValue)
        {
            formFields["flag"] = input.Stat.Value.ToString(CultureInfo.InvariantCulture);
        }

        return new CloudDriveApiContext
        {
            Category = "offline-write",
            FormFields = formFields,
        };
    }

    public static PagedResultDto<CloudDriveOfflineTaskDto> MapTaskList(JsonElement response, GetOfflineTaskListInput input)
    {
        JsonElement source = GetPayload(response);
        List<CloudDriveOfflineTaskDto> items = ReadTaskArray(source);
        int pageSize = ReadInt32(source, "page_size") ?? ReadInt32(source, "page_row") ?? items.Count;

        return new PagedResultDto<CloudDriveOfflineTaskDto>
        {
            Items = items,
            TotalCount = ReadInt64(source, "count") ?? ReadInt64(source, "total") ?? items.Count,
            SkipCount = Math.Max(input.Page - 1, 0) * pageSize,
            MaxResultCount = pageSize,
        };
    }

    public static CloudDriveOfflineQuotaDto MapQuota(JsonElement quotaResponse, JsonElement? statsResponse)
    {
        JsonElement quota = GetPayload(quotaResponse);
        JsonElement? stats = statsResponse.HasValue ? GetPayload(statsResponse.Value) : null;

        return new CloudDriveOfflineQuotaDto
        {
            Package = quota.TryGetProperty("package", out JsonElement package)
                ? JsonSerializer.Deserialize<Dictionary<string, object?>>(package.GetRawText())
                : null,
            Count = ReadInt32(quota, "count") ?? 0,
            Surplus = ReadInt32(quota, "surplus") ?? 0,
            MaxSize = ReadInt64(quota, "max_size") ?? 0,
            Used = ReadInt64(quota, "used") ?? 0,
            Total = stats.HasValue ? ReadInt32(stats.Value, "total_count") ?? 0 : 0,
            Failed = stats.HasValue ? ReadInt32(stats.Value, "failed_count") ?? 0 : 0,
            Completed = stats.HasValue ? ReadInt32(stats.Value, "finished_count") ?? 0 : 0,
            Active = stats.HasValue ? ReadInt32(stats.Value, "downloading_count") ?? 0 : 0,
        };
    }

    public static CloudDriveOperationResultDto CreateSyncResult(string message)
    {
        return CloudDriveFileHandlerSupport.CreateSyncResult(message);
    }

    public static bool IsOfflineSignatureUnavailable(CloudDriveConfigurationException exception)
    {
        return string.Equals(exception.ErrorCode, "offline_signature_not_available", StringComparison.Ordinal);
    }

    public static string ResolveAddAction(AddOfflineTaskInput input)
    {
        return input.Urls.Count == 1 ? "add_task_url" : "add_task_urls";
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

    private static List<CloudDriveOfflineTaskDto> ReadTaskArray(JsonElement source)
    {
        if (!source.TryGetProperty("tasks", out JsonElement tasks)
            || tasks.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        List<CloudDriveOfflineTaskDto> result = [];
        foreach (JsonElement item in tasks.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            result.Add(new CloudDriveOfflineTaskDto
            {
                InfoHash = ReadString(item, "info_hash") ?? string.Empty,
                Name = ReadString(item, "name") ?? string.Empty,
                Size = ReadInt64(item, "size") ?? 0,
                AddTime = ReadInt64(item, "add_time"),
                LastUpdate = ReadInt64(item, "last_update"),
                PercentDone = ReadDecimal(item, "percentDone") ?? ReadDecimal(item, "percent_done"),
                Url = ReadString(item, "url"),
                FileId = ReadString(item, "file_id"),
                ParentId = ReadString(item, "wp_path_id"),
                VideoDefinition = ReadString(item, "def2"),
                PlayDuration = ReadInt64(item, "play_long"),
                Status = NormalizeStatus(ReadInt32(item, "status")),
            });
        }

        return result;
    }

    private static string? NormalizeStatus(int? status)
    {
        return status switch
        {
            -1 => "failed",
            0 => "allocating",
            1 => "downloading",
            2 => "success",
            _ => null,
        };
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

    private static int? ReadInt32(JsonElement source, string propertyName)
    {
        long? value = ReadInt64(source, propertyName);
        return value.HasValue ? checked((int)Math.Min(value.Value, int.MaxValue)) : null;
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

    private static decimal? ReadDecimal(JsonElement source, string propertyName)
    {
        if (source.ValueKind != JsonValueKind.Object
            || !source.TryGetProperty(propertyName, out JsonElement value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out decimal number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String
            && decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal stringNumber)
                ? stringNumber
                : null;
    }
}
