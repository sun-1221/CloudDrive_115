using System.Text.Json;

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
            Paths = response.Path.Count > 0
                ? response.Path.Select(p => p.ToDto()).ToList()
                : null,
        };
    }

    public static CloudDriveSearchListDto ToDto(this CloudDriveFileSearchResponse response)
    {
        IReadOnlyList<CloudDriveSearchResultDto> items = response.Items
            .Concat(response.Folders)
            .Select(item => item.ToSearchDto())
            .ToList();

        return new CloudDriveSearchListDto
        {
            Items = items,
            Count = response.Count,
            PageSize = response.PageSize,
            PageCount = response.PageCount ?? 0,
            Offset = response.Offset,
            IsAsc = response.IsAscending,
            Order = response.Order,
            Suffix = response.Suffix,
            FcMix = response.FcMix,
            Type = response.Type,
            State = ReadBoolean(response.State),
            Error = response.Error,
            ErrCode = response.ErrCode,
            ItemSchemaStatus = "available",
        };
    }

    public static CloudDriveSearchListDto ToNotCollectedDto(this CloudDriveFileSearchResponse response)
    {
        return new CloudDriveSearchListDto
        {
            Items = [],
            Count = response.Count,
            PageSize = response.PageSize,
            PageCount = response.PageCount ?? 0,
            Offset = response.Offset,
            IsAsc = response.IsAscending,
            Order = response.Order,
            Suffix = response.Suffix,
            FcMix = response.FcMix,
            Type = response.Type,
            State = ReadBoolean(response.State),
            Error = response.Error,
            ErrCode = response.ErrCode,
            ItemSchemaStatus = "not_collected",
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
            RawCategoryCode = item.FileCategory,
            FileClass = item.FileClass,
            Ico = item.Icon,
            IsDirectory = item.IsDirectory,
            ModifiedTime = CloudDriveTimeMapping.FromUnixSeconds(item.UpdateTime ?? item.UploadTime),
            UploadTime = item.UploadTime.HasValue
                ? CloudDriveTimeMapping.FromUnixSeconds(item.UploadTime)
                : null,
            UpdateTime = item.UpdateTime.HasValue
                ? CloudDriveTimeMapping.FromUnixSeconds(item.UpdateTime)
                : null,
            PickCode = item.PickCode,
            Sha1 = item.Sha1,
            IsStar = item.IsStarred ?? false,
            IsVideo = item.IsVideo ?? false,
            PlayLong = item.PlayDuration,
            VideoDefinition = ParseNullableInt(item.VideoDefinition),
            IsPrivate = item.IsPrivate,
            UploadStatusRaw = item.UploadStatus,
            UploadStatus = "unknown",
            ThumbnailUrl = item.ThumbnailUrl ?? item.VideoImageUrl,
            OriginalImageUrl = item.OriginalUrl,
            VideoImageUrl = item.VideoImageUrl,
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
            RawCategoryCode = file.RawCategoryCode,
            FileClass = file.FileClass,
            Ico = file.Ico,
            IsDirectory = file.IsDirectory,
            ModifiedTime = file.ModifiedTime,
            UploadTime = file.UploadTime,
            UpdateTime = file.UpdateTime,
            PickCode = file.PickCode,
            Sha1 = file.Sha1,
            IsStar = file.IsStar,
            Labels = file.Labels,
            IsVideo = file.IsVideo,
            PlayLong = file.PlayLong,
            VideoDefinition = file.VideoDefinition,
            IsPrivate = file.IsPrivate,
            UploadStatusRaw = file.UploadStatusRaw,
            UploadStatus = file.UploadStatus,
            ThumbnailUrl = file.ThumbnailUrl,
            OriginalImageUrl = file.OriginalImageUrl,
            VideoImageUrl = file.VideoImageUrl,
            CreatedTime = CloudDriveTimeMapping.FromUnixSeconds(item.UploadTime),
            SizeBytes = item.Size,
            Paths = paths,
        };
    }

    public static CloudDriveSearchResultDto ToSearchDto(this CloudDrive115FileItem item)
    {
        return new CloudDriveSearchResultDto
        {
            FileId = item.FileId,
            Aid = ReadString(item.Raw, "aid") ?? string.Empty,
            ParentId = ReadString(item.Raw, "cid") ?? string.Empty,
            FileName = item.Name,
            Size = item.Size,
            PickCode = item.PickCode,
            RawCategoryCode = item.FileCategory,
            Category = "unknown",
            Ico = item.Icon,
            Sha1 = item.Sha1,
            DirectoryName = ReadString(item.Raw, "dp"),
            HighlightNameRaw = ReadString(item.Raw, "ns"),
            Thumb = item.ThumbnailUrl ?? item.OriginalUrl ?? item.VideoImageUrl,
            VideoImg = item.VideoImageUrl,
            IsVideo = item.IsVideo,
            IsStarred = item.IsStarred,
            IsPrivate = item.IsPrivate,
            UploadStatusRaw = item.UploadStatus,
            UploadStatus = "unknown",
            PlayDuration = item.PlayDuration,
            Labels = [],
            SchemaStatus = "available",
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

    public static CloudDriveRecycleListDto ToDto(this CloudDriveRecycleBinResponse response)
    {
        return new CloudDriveRecycleListDto
        {
            Items = [],
            Count = response.Count,
            RbPass = response.RecycleBinPasswordEnabled ?? 0,
            Offset = response.Offset,
            PageSize = response.PageSize,
            Order = response.Order,
            IsAsc = response.IsAscending,
            ItemSchemaStatus = "not_collected",
        };
    }

    private static int? ParseNullableInt(string? value)
    {
        return int.TryParse(value, out int result) ? result : null;
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

    private static bool? ReadBoolean(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetInt32(out int number) => number != 0,
            JsonValueKind.String when bool.TryParse(value.GetString(), out bool parsed) => parsed,
            JsonValueKind.String when int.TryParse(value.GetString(), out int number) => number != 0,
            _ => null,
        };
    }
}
