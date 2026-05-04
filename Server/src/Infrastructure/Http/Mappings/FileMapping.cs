using Application.CloudDrive.Dtos;

using Infrastructure.Http.Responses;

namespace Infrastructure.Http.Mappings;

/// <summary>
/// 115 文件响应模型手动映射。
/// </summary>
internal static class FileMapping
{
    public static PagedResultDto<CloudDriveFileDto> ToDto(this CloudDriveFileListResponse response)
    {
        return new PagedResultDto<CloudDriveFileDto>
        {
            Items = response.Items.Select(item => item.ToDto()).ToList(),
            TotalCount = response.Count,
            SkipCount = response.Offset,
            MaxResultCount = response.Limit,
        };
    }

    public static PagedResultDto<CloudDriveSearchResultDto> ToDto(this CloudDriveFileSearchResponse response)
    {
        IReadOnlyList<CloudDriveSearchResultDto> items = response.Items
            .Concat(response.Folders)
            .Select(item => item.ToDto(parentId: string.Empty))
            .ToList();

        return new PagedResultDto<CloudDriveSearchResultDto>
        {
            Items = items,
            TotalCount = response.Count,
            SkipCount = response.Offset,
            MaxResultCount = response.PageSize,
        };
    }

    public static CloudDriveFileDetailDto? ToDto(this CloudDriveFileDetailResponse response)
    {
        CloudDrive115FileItem? item = response.File ?? response.Data;
        return item?.ToDto(paths: null);
    }

    public static CloudDriveFileDto ToDto(this CloudDrive115FileItem item)
    {
        return new CloudDriveFileDto
        {
            FileId = item.FileId,
            Name = item.Name,
            Size = item.Size,
            FileType = item.FileClass ?? item.Icon ?? string.Empty,
            IsDirectory = item.IsDirectory,
            ModifiedTime = CloudDriveTimeMapping.FromUnixSeconds(item.UpdateTime ?? item.UploadTime),
            PickCode = item.PickCode,
            Sha1 = item.Sha1,
            IsStar = item.IsStarred ?? false,
            IsVideo = item.IsVideo ?? false,
            PlayLong = item.PlayDuration,
            VideoDefinition = ParseNullableInt(item.VideoDefinition),
            ThumbnailUrl = item.ThumbnailUrl ?? item.VideoImageUrl,
            OriginalImageUrl = item.OriginalUrl,
        };
    }

    public static CloudDriveFileDetailDto ToDto(
        this CloudDrive115FileItem item,
        IReadOnlyList<CloudDrivePathNodeDto>? paths)
    {
        CloudDriveFileDto file = item.ToDto();
        return new CloudDriveFileDetailDto
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
            CreatedTime = CloudDriveTimeMapping.FromUnixSeconds(item.UploadTime),
            SizeBytes = item.Size,
            Paths = paths,
        };
    }

    public static CloudDriveSearchResultDto ToDto(this CloudDrive115FileItem item, string parentId)
    {
        CloudDriveFileDto file = item.ToDto();
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

    public static CloudDrivePathNodeDto ToDto(this CloudDrive115BreadcrumbItem item)
    {
        return new CloudDrivePathNodeDto
        {
            FileId = item.DirectoryId,
            FileName = item.Name,
        };
    }

    public static CloudDriveDownloadDto ToDto(this CloudDriveDownloadUrlResponse response)
    {
        return new CloudDriveDownloadDto
        {
            DownloadUrl = response.Url ?? string.Empty,
            FileName = response.FileName ?? string.Empty,
            FileSize = 0,
            IsTemporary = true,
        };
    }

    public static CloudDriveUploadSessionDto ToDto(this CloudDriveUploadInitResponse response)
    {
        Dictionary<string, string>? formFields = response.HasOssCredential
            ? new Dictionary<string, string>
            {
                ["key"] = response.Object ?? string.Empty,
                ["OSSAccessKeyId"] = response.AccessId ?? string.Empty,
                ["policy"] = response.Policy ?? string.Empty,
                ["signature"] = response.Signature ?? string.Empty,
                ["callback"] = response.Callback ?? string.Empty,
            }
            : null;

        return new CloudDriveUploadSessionDto
        {
            PickCode = response.PickCode,
            Status = response.IsInstantUpload ? 2 : 1,
            Target = response.DirectoryId,
            ObjectKey = response.Object,
            Callback = response.Callback,
            UploadEndpoint = response.Host,
            FormFields = formFields,
            File = response.IsInstantUpload
                ? new CloudDriveUploadedFileDto
                {
                    FileId = response.FileId ?? string.Empty,
                    Name = response.FileName ?? string.Empty,
                    Path = response.DirectoryId,
                }
                : null,
            ExpiresAt = CloudDriveTimeMapping.FromUnixSecondsOrNull(response.Expire),
        };
    }

    public static CloudDriveUploadedFileDto ToDto(this CloudDriveUploadCompleteResponse response)
    {
        return new CloudDriveUploadedFileDto
        {
            FileId = response.FileId ?? string.Empty,
            Name = response.FileName ?? string.Empty,
            Path = response.DirectoryId,
        };
    }

    public static PagedResultDto<CloudDriveRecycleBinItemDto> ToDto(this CloudDriveRecycleBinResponse response)
    {
        return new PagedResultDto<CloudDriveRecycleBinItemDto>
        {
            Items = response.Items.Select(item => item.ToDto()).ToList(),
            TotalCount = response.Count,
            SkipCount = response.Offset,
            MaxResultCount = response.PageSize,
        };
    }

    public static CloudDriveRecycleBinItemDto ToDto(this CloudDriveRecycleItemResponse item)
    {
        return new CloudDriveRecycleBinItemDto
        {
            FileId = item.Id,
            RecycleBinItemId = item.Id,
            Name = item.FileName,
            Size = item.FileSize,
            FileType = string.Empty,
            IsDirectory = item.IsDirectory,
            ModifiedTime = CloudDriveTimeMapping.FromUnixSeconds(item.DeletedTime),
            PickCode = item.PickCode,
            ThumbnailUrl = item.ThumbnailUrl,
            RestoreStatus = item.RestoreStatus ?? 0,
            DeletedTime = CloudDriveTimeMapping.FromUnixSeconds(item.DeletedTime),
            OriginalParentId = item.OriginalParentId,
            OriginalParentName = item.OriginalParentName,
        };
    }

    private static int? ParseNullableInt(string? value)
    {
        return int.TryParse(value, out int result) ? result : null;
    }
}
