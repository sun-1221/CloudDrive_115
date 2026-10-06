using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Files;

internal sealed class GetFileDetailQueryHandler(
    CloudDriveFileManager fileManager,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetFileDetailQuery, CloudDriveFileDetailDto>
{
    public async Task<Result<CloudDriveFileDetailDto>> Handle(
        GetFileDetailQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        if (string.IsNullOrWhiteSpace(request.Input.FileId))
        {
            return Result.Failure<CloudDriveFileDetailDto>(
                CloudDriveFileHandlerSupport.InvalidInput(
                    "cloud_drive_file_id_required",
                    "文件 ID 不能为空。"));
        }

        string directoryId = CloudDriveFileHandlerSupport.NormalizeDirectoryId(request.Input.Path);
        GetFileListInput input = new()
        {
            DirectoryId = directoryId,
            Offset = 0,
            Limit = configOptions.Value.MaxPageSize,
        };

        PagedResultDto<CloudDriveFileDto> list = await fileManager.GetFileListAsync<PagedResultDto<CloudDriveFileDto>>(
            request.UserId,
            accountContextAccessor.CloudAccountUid,
            directoryId,
            CloudDriveFileHandlerSupport.CreateFileListContext(input),
            cancellationToken);

        CloudDriveFileDto? item = list.Items.FirstOrDefault(value =>
            string.Equals(value.FileId, request.Input.FileId, StringComparison.Ordinal));
        if (item is null)
        {
            return Result.Failure<CloudDriveFileDetailDto>(
                CloudDriveFileHandlerSupport.FileNotFound("文件"));
        }

        return Result.Success(new CloudDriveFileDetailDto
        {
            FileId = item.FileId,
            Name = item.Name,
            Size = item.Size,
            FileType = item.FileType,
            RawCategoryCode = item.RawCategoryCode,
            FileClass = item.FileClass,
            Ico = item.Ico,
            IsDirectory = item.IsDirectory,
            ModifiedTime = item.ModifiedTime,
            UploadTime = item.UploadTime,
            UpdateTime = item.UpdateTime,
            PickCode = item.PickCode,
            Sha1 = item.Sha1,
            IsStar = item.IsStar,
            Labels = item.Labels,
            IsVideo = item.IsVideo,
            PlayLong = item.PlayLong,
            VideoDefinition = item.VideoDefinition,
            IsPrivate = item.IsPrivate,
            UploadStatusRaw = item.UploadStatusRaw,
            UploadStatus = item.UploadStatus,
            ThumbnailUrl = item.ThumbnailUrl,
            OriginalImageUrl = item.OriginalImageUrl,
            VideoImageUrl = item.VideoImageUrl,
            ParentId = item.ParentId,
            Paths = list.Paths,
            SchemaSource = "file_list_derived",
        });
    }
}
