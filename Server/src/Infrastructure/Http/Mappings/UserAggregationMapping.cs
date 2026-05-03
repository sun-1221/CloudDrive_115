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
            CloudAccountUid = cloudAccountUid,
            UserName = response.UserName ?? string.Empty,
            AvatarSmallUrl = response.UserFaceSmall ?? response.UserFace,
            AvatarMediumUrl = response.UserFaceMedium,
            AvatarLargeUrl = response.UserFaceLarge,
            SpaceInfo = spaceInfo,
            VipInfo = vipInfo,
        };
    }

    public static CloudDriveSpaceInfoDto ToDto(this CloudDriveStorageInfoResponse response)
    {
        return new CloudDriveSpaceInfoDto
        {
            AllTotal = response.AllTotal ?? string.Empty,
            AllUse = response.AllUse ?? string.Empty,
            AllRemain = response.AllRemain ?? string.Empty,
            AllTotalBytes = response.AllTotalBytes ?? 0,
            AllUseBytes = response.AllUseBytes ?? 0,
            AllRemainBytes = response.AllRemainBytes ?? 0,
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
