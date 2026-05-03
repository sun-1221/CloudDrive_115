using System.Text.Json;

using Application.CloudDrive.Dtos;

using Infrastructure.Http.Responses;

namespace Infrastructure.Http.Mappings;

/// <summary>
/// 115 云下载响应模型手动映射。
/// </summary>
internal static class OfflineMapping
{
    public static PagedResultDto<CloudDriveOfflineTaskDto> ToDto(this CloudDriveOfflineTaskListResponse response)
    {
        return new PagedResultDto<CloudDriveOfflineTaskDto>
        {
            Items = response.Tasks.Select(item => item.ToDto()).ToList(),
            TotalCount = response.Count,
            SkipCount = Math.Max(response.Page - 1, 0) * response.PageSize,
            MaxResultCount = response.PageSize,
        };
    }

    public static CloudDriveOfflineTaskDto ToDto(this CloudDriveOfflineTaskResponse response)
    {
        return new CloudDriveOfflineTaskDto
        {
            InfoHash = response.InfoHash,
            Name = response.Name,
            Size = response.Size,
            AddTime = response.AddTime,
            LastUpdate = response.LastUpdate,
            PercentDone = response.PercentDone,
            Url = response.Url,
            FileId = response.FileId,
            ParentId = response.ParentId,
            VideoDefinition = response.VideoDefinition,
            PlayDuration = response.PlayDuration,
            Status = NormalizeStatus(response.Status),
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
            Total = stats?.TotalCount ?? 0,
            Failed = stats?.FailedCount ?? 0,
            Completed = stats?.FinishedCount ?? 0,
            Active = stats?.DownloadingCount ?? 0,
        };
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

    private static Dictionary<string, object?>? ToDictionary(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        return JsonSerializer.Deserialize<Dictionary<string, object?>>(element.GetRawText());
    }
}
