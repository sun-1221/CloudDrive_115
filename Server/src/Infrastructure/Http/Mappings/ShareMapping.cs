using Application.CloudDrive.Dtos;

using Infrastructure.Http.Responses;

namespace Infrastructure.Http.Mappings;

/// <summary>
/// 115 分享响应模型手动映射。
/// </summary>
internal static class ShareMapping
{
    public static CloudDriveShareListDto ToDto(this CloudDriveShareListResponse response)
    {
        return new CloudDriveShareListDto
        {
            Items = response.Items.Select(item => item.ToDto()).ToList(),
            Count = response.Count,
            DefineReceiveCode = response.DefineReceiveCode,
            BindReceiveCode = response.BindReceiveCode,
            ItemSchemaStatus = "available",
        };
    }

    public static CloudDriveShareItemDto ToDto(this CloudDriveShareItemResponse item)
    {
        return new CloudDriveShareItemDto
        {
            ShareCode = item.ShareCode ?? string.Empty,
            ShareTitle = item.ShareTitle ?? string.Empty,
            FileId = item.FileId ?? string.Empty,
            FileName = item.FileName ?? string.Empty,
            PickCode = item.PickCode,
            FileSize = item.FileSize,
            CreateTime = CloudDriveTimeMapping.FromUnixSecondsOrNull(item.CreateTime),
            ExpireTime = CloudDriveTimeMapping.FromUnixSecondsOrNull(item.ExpireTime),
            Url = item.Url,
            SchemaStatus = "available",
        };
    }
}
