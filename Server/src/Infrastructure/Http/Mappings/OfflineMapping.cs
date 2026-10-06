using System.Text.Json;

using Application.CloudDrive.Dtos;

using Infrastructure.Http.Responses;

namespace Infrastructure.Http.Mappings;

/// <summary>
/// 115 云下载响应模型手动映射。
/// </summary>
internal static class OfflineMapping
{
    public static CloudDriveOfflineTaskListDto ToDto(this CloudDriveOfflineTaskListResponse response)
    {
        return new CloudDriveOfflineTaskListDto
        {
            Items = [],
            Page = response.Page,
            PageCount = response.PageCount,
            PageRow = response.PageRow,
            PageSize = response.PageSize,
            Count = response.Count,
            Quota = response.Quota,
            Total = response.Total,
            RealState = response.RealState,
            ErrType = response.ErrorType,
            ItemSchemaStatus = "not_collected",
        };
    }

    public static CloudDriveOfflineQuotaDto ToDto(
        this CloudDriveOfflineQuotaResponse quota,
        CloudDriveOfflineStatsResponse? stats = null)
    {
        return new CloudDriveOfflineQuotaDto
        {
            Package = ToDictionary(quota.Package),
            Count = quota.Count,
            Surplus = quota.Surplus,
            MaxSize = quota.MaxSize,
            Used = quota.Used,
        };
    }

    public static CloudDriveOfflineStatsDto ToDto(this CloudDriveOfflineStatsResponse stats)
    {
        return new CloudDriveOfflineStatsDto
        {
            Total = stats.TotalCount,
            Failed = stats.FailedCount,
            Completed = stats.FinishedCount,
            Active = stats.DownloadingCount,
        };
    }

    private static Dictionary<string, object?>? ToDictionary(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<Dictionary<string, object?>>(element.GetRawText());
    }
}
