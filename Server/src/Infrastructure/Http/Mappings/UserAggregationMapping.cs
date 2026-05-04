using Application.CloudDrive.Dtos;

using Infrastructure.Http.Responses;

namespace Infrastructure.Http.Mappings;

/// <summary>
/// 115 用户中心响应模型手动映射。
/// </summary>
internal static class UserAggregationMapping
{
    public static CloudDriveUserInfoDto ToDto(
        this CloudDriveUserInfoResponse response,
        string cloudAccountUid,
        CloudDriveSpaceInfoDto? spaceInfo = null,
        CloudDriveVipInfoDto? vipInfo = null)
    {
        return new CloudDriveUserInfoDto
        {
            Uid = cloudAccountUid,
            Nickname = response.UserName ?? string.Empty,
            AvatarUrl = response.UserFace ?? response.UserFaceSmall,
        };
    }

    public static CloudDriveSpaceInfoDto ToDto(this CloudDriveStorageInfoResponse response)
    {
        return new CloudDriveSpaceInfoDto
        {
            SchemaStatus = "not_collected",
        };
    }

    public static CloudDriveVipInfoDto ToDto(this CloudDriveVipCardResponse response)
    {
        return new CloudDriveVipInfoDto
        {
            LevelName = response.VipCount.HasValue ? $"VIP x{response.VipCount.Value}" : null,
        };
    }
}
