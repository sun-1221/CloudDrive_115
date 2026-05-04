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
            Items = [],
            Count = response.Count,
            DefineReceiveCode = response.DefineReceiveCode,
            BindReceiveCode = response.BindReceiveCode,
            ItemSchemaStatus = "not_collected",
        };
    }
}
