using Application.CloudDrive.Dtos;

using Infrastructure.Http.Responses;

namespace Infrastructure.Http.Mappings;

/// <summary>
/// 115 分享响应模型手动映射。
/// </summary>
internal static class ShareMapping
{
    public static PagedResultDto<CloudDriveShareItemDto> ToDto(this CloudDriveShareListResponse response)
    {
        return new PagedResultDto<CloudDriveShareItemDto>
        {
            Items = response.Items.Select(item => item.ToDto()).ToList(),
            TotalCount = response.Count,
            MaxResultCount = response.Items.Count,
        };
    }

    public static CloudDriveShareItemDto ToDto(this CloudDriveShareItemResponse response)
    {
        return new CloudDriveShareItemDto
        {
            ShareCode = response.ShareCode,
            ShareTitle = response.ShareTitle,
            FileId = response.FileId,
            FileName = response.FileName,
            PickCode = response.PickCode,
            FileSize = response.FileSize,
            CreateTime = response.CreateTime,
            ExpireTime = response.ExpireTime,
            Url = response.Url,
        };
    }

    public static CloudDriveShareSendItemDto ToDto(this CloudDriveShareItemResponse response, CloudDriveShareSendItemDto _)
    {
        CloudDriveShareItemDto item = response.ToDto();
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

    public static CloudDriveShareReceiveItemDto ToDto(this CloudDriveShareItemResponse response, CloudDriveShareReceiveItemDto _)
    {
        CloudDriveShareItemDto item = response.ToDto();
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
}
