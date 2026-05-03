using Application.CloudDrive.Dtos;

namespace Application.CloudDrive.Mappings;

/// <summary>
/// 文件相关手动映射锚点。115 网页端响应模型位于 Infrastructure 层，由同名基础设施映射扩展完成归一化。
/// </summary>
public static class FileMapping
{
    /// <summary>
    /// 将文件 DTO 映射为搜索结果 DTO，未提供的路径保持空值。
    /// </summary>
    public static CloudDriveSearchResultDto ToDto(this CloudDriveFileDto file, string parentId)
    {
        return new CloudDriveSearchResultDto
        {
            FileId = file.FileId,
            Name = file.Name,
            Size = file.Size,
            FileType = file.FileType,
            IsDirectory = file.IsDirectory,
            ModifiedTime = file.ModifiedTime,
            PickCode = file.PickCode,
            Sha1 = file.Sha1,
            IsStar = file.IsStar,
            Labels = file.Labels,
            IsVideo = file.IsVideo,
            PlayLong = file.PlayLong,
            VideoDefinition = file.VideoDefinition,
            ThumbnailUrl = file.ThumbnailUrl,
            OriginalImageUrl = file.OriginalImageUrl,
            ParentId = parentId,
        };
    }
}
