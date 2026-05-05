using System.Globalization;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;

using Domain.Results;

namespace Application.CloudDrive.Files;

internal static class CloudDriveFileHandlerSupport
{
    public const int DefaultAsyncBatchThreshold = 100;

    public static readonly Error CloudAccountUidRequired = new(
        "cloud_drive_cloud_account_uid_required",
        "缺少 CloudAccountUid，请指定 115 账号。");

    public static Error FileNotFound(string resourceName) => new(
        "cloud_drive_file_not_found",
        $"{resourceName} 不存在或不属于当前账号上下文。");

    public static Error UploadStreamRequired = new(
        "cloud_drive_upload_stream_required",
        "同步上传需要提供文件流；如需后台上传，请先暂存文件并设置 TempFilePath。");

    public static Error ApiNotCollected(string operationName) => new(
        "cloud_drive_api_not_collected",
        $"{operationName} 115 网页端接口尚未采集，待补采集后再接入。");

    public static CloudDriveApiContext CreateFileListContext(GetFileListInput input)
    {
        Dictionary<string, string> queryParameters = new(StringComparer.Ordinal)
        {
            ["aid"] = "1",
            ["cid"] = NormalizeDirectoryId(input.DirectoryId),
            ["offset"] = input.Offset.ToString(CultureInfo.InvariantCulture),
            ["limit"] = input.Limit.ToString(CultureInfo.InvariantCulture),
            ["format"] = "json",
        };

        AddOptional(queryParameters, "type", input.Type);
        AddOptional(queryParameters, "suffix", input.Suffix);
        AddOptional(queryParameters, "asc", input.Asc);
        AddOptional(queryParameters, "o", input.OrderBy);
        AddOptional(queryParameters, "custom_order", input.CustomOrder);
        AddOptional(queryParameters, "stdir", input.Stdir);
        AddOptional(queryParameters, "star", input.Star);
        AddOptional(queryParameters, "cur", input.Cur);
        AddOptional(queryParameters, "show_dir", input.ShowDir);
        AddOptional(queryParameters, "natsort", input.NatSort);
        AddOptional(queryParameters, "snap", input.Snap);
        AddOptional(queryParameters, "count_folders", input.CountFolders);
        AddOptional(queryParameters, "record_open_time", input.RecordOpenTime);
        AddOptional(queryParameters, "code", input.Code);
        AddOptional(queryParameters, "scid", input.Scid);
        AddOptional(queryParameters, "source", input.Source);

        return new CloudDriveApiContext
        {
            Category = "file-list",
            QueryParameters = queryParameters,
        };
    }

    public static CloudDriveApiContext CreateFileDetailContext(GetFileInput input)
    {
        Dictionary<string, string> queryParameters = new(StringComparer.Ordinal)
        {
            ["format"] = "json",
        };

        AddOptional(queryParameters, "file_id", input.FileId);
        AddOptional(queryParameters, "path", input.Path);

        return new CloudDriveApiContext
        {
            Category = "file-detail",
            QueryParameters = queryParameters,
        };
    }

    public static CloudDriveApiContext CreateSearchContext(SearchFileInput input)
    {
        Dictionary<string, string> queryParameters = new(StringComparer.Ordinal)
        {
            ["aid"] = "1",
            ["cid"] = NormalizeDirectoryId(input.DirectoryId),
            ["offset"] = input.Offset.ToString(CultureInfo.InvariantCulture),
            ["limit"] = input.Limit.ToString(CultureInfo.InvariantCulture),
            ["search_value"] = input.Keyword,
            ["format"] = "json",
        };

        AddOptional(queryParameters, "type", input.Type);
        AddOptional(queryParameters, "date", input.Date);
        AddOptional(queryParameters, "pick_code", input.PickCode);
        AddOptional(queryParameters, "count_folders", input.CountFolders);
        AddOptional(queryParameters, "source", input.Source);

        return new CloudDriveApiContext
        {
            Category = "file-search",
            QueryParameters = queryParameters,
        };
    }

    public static CloudDriveApiContext CreateDownloadContext(GetDownloadUrlInput input)
    {
        return new CloudDriveApiContext
        {
            Category = "download",
            QueryParameters = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["pickcode"] = input.PickCode,
                ["format"] = "json",
            },
        };
    }

    public static CloudDriveApiContext CreateRecycleBinListContext(GetRecycleBinListInput input)
    {
        return new CloudDriveApiContext
        {
            Category = "recycle-bin",
            QueryParameters = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["aid"] = "7",
                ["cid"] = "0",
                ["offset"] = input.Offset.ToString(CultureInfo.InvariantCulture),
                ["limit"] = input.Limit.ToString(CultureInfo.InvariantCulture),
                ["format"] = "json",
            },
        };
    }

    public static CloudDriveApiContext CreateUploadInitContext(string cloudAccountUid, InitUploadInput input)
    {
        return CreateUploadInitContext(
            cloudAccountUid,
            input.DirectoryId,
            input.FileName,
            input.FileSize);
    }

    public static CloudDriveApiContext CreateUploadInitContext(string cloudAccountUid, UploadFileInput input)
    {
        return CreateUploadInitContext(
            cloudAccountUid,
            input.DirectoryId,
            input.FileName,
            input.FileSize);
    }

    public static CloudDriveOperationResultDto CreateSyncResult(string message)
    {
        return new CloudDriveOperationResultDto
        {
            Accepted = false,
            Message = message,
        };
    }

    public static CloudDriveOperationResultDto CreateAcceptedResult(Guid jobId, string message)
    {
        return new CloudDriveOperationResultDto
        {
            Accepted = true,
            JobId = jobId,
            Message = message,
        };
    }

    public static CloudDriveUploadExecutionDto CreateAcceptedUploadResult(Guid jobId)
    {
        return new CloudDriveUploadExecutionDto
        {
            Accepted = true,
            JobId = jobId,
            Message = "上传任务已受理。",
        };
    }

    public static bool ShouldRunBatchAsync(int itemCount) => itemCount > DefaultAsyncBatchThreshold;

    public static string NormalizeDirectoryId(string? directoryId) =>
        string.IsNullOrWhiteSpace(directoryId) ? "0" : directoryId;

    public static string[] UniqueDirectories(params string?[] directoryIds)
    {
        return directoryIds
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public static CloudDriveUploadedFileDto CreateUploadedFileFromSession(
        CloudDriveUploadSessionDto session,
        UploadFileInput input)
    {
        return session.File ?? new CloudDriveUploadedFileDto
        {
            FileId = session.PickCode ?? input.PickCode ?? string.Empty,
            Name = input.FileName,
            Path = input.DirectoryId,
        };
    }

    public static Error InvalidInput(string code, string message) => new(code, message);

    private static CloudDriveApiContext CreateUploadInitContext(
        string cloudAccountUid,
        string directoryId,
        string fileName,
        long fileSize)
    {
        return new CloudDriveApiContext
        {
            Category = "upload",
            FormFields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["userid"] = cloudAccountUid,
                ["filename"] = fileName,
                ["filesize"] = fileSize.ToString(CultureInfo.InvariantCulture),
                ["target"] = $"U_1_{directoryId}",
            },
        };
    }

    private static void AddOptional(
        IDictionary<string, string> parameters,
        string key,
        int? value)
    {
        if (value.HasValue)
        {
            parameters[key] = value.Value.ToString(CultureInfo.InvariantCulture);
        }
    }

    private static void AddOptional(
        IDictionary<string, string> parameters,
        string key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parameters[key] = value;
        }
    }

}
