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
            QueryParameters = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["page"] = input.Page.ToString(CultureInfo.InvariantCulture),
                ["page_size"] = "15",
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
            QueryParameters = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["stat"] = "0",
            },
        };
    }

    public static CloudDriveOfflineTaskListDto MapTaskList(JsonElement response, GetOfflineTaskListInput input)
    {
        JsonElement source = GetPayload(response);

        return new CloudDriveOfflineTaskListDto
        {
            Items = [],
            Page = ReadInt32(source, "page") ?? input.Page,
            PageCount = ReadInt32(source, "page_count") ?? 0,
            PageRow = ReadInt32(source, "page_row") ?? 0,
            PageSize = ReadInt32(source, "page_size") ?? 0,
            Count = ReadInt32(source, "count") ?? 0,
            Quota = ReadInt32(source, "quota") ?? 0,
            Total = ReadInt32(source, "total") ?? 0,
            RealState = ReadBoolean(source, "real_state"),
            ErrType = ReadString(source, "errtype"),
            ItemSchemaStatus = "not_collected",
        };
    }

    public static CloudDriveOfflineQuotaDto MapQuota(JsonElement quotaResponse)
    {
        JsonElement quota = GetPayload(quotaResponse);

        return new CloudDriveOfflineQuotaDto
        {
            Package = quota.TryGetProperty("package", out JsonElement package)
                ? JsonSerializer.Deserialize<Dictionary<string, object?>>(package.GetRawText())
                : null,
            Count = ReadInt32(quota, "count") ?? 0,
            Surplus = ReadInt32(quota, "surplus") ?? 0,
            MaxSize = ReadInt64(quota, "max_size") ?? 0,
            Used = ReadInt64(quota, "used") ?? 0,
        };
    }

    public static CloudDriveOfflineStatsDto MapStats(JsonElement statsResponse)
    {
        JsonElement stats = GetPayload(statsResponse);

        return new CloudDriveOfflineStatsDto
        {
            Total = ReadInt32(stats, "total_count") ?? 0,
            Failed = ReadInt32(stats, "failed_count") ?? 0,
            Completed = ReadInt32(stats, "finished_count") ?? 0,
            Active = ReadInt32(stats, "downloading_count") ?? 0,
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

    private static bool? ReadBoolean(JsonElement source, string propertyName)
    {
        if (source.ValueKind != JsonValueKind.Object
            || !source.TryGetProperty(propertyName, out JsonElement value))
        {
            return null;
        }

        if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number))
        {
            return number != 0;
        }

        return value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out bool parsed)
            ? parsed
            : null;
    }
}
