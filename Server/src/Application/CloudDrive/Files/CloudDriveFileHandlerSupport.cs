using System.Globalization;
using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;

using Domain.Exceptions;
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
        AddOptional(queryParameters, "asc", input.Asc);
        AddOptional(queryParameters, "o", input.OrderBy);
        AddOptional(queryParameters, "custom_order", input.CustomOrder);
        AddOptional(queryParameters, "fc_mix", input.FcMix);
        AddOptional(queryParameters, "nf", input.Nf);
        AddOptional(queryParameters, "stdir", input.Stdir);
        AddOptional(queryParameters, "show_dir", input.ShowDir);
        AddOptional(queryParameters, "natsort", input.NatSort);
        AddOptional(queryParameters, "count_folders", input.CountFolders);

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
                // 115 下载契约只观察到这两个 number-string 查询字段；不把它们扩展为其它业务参数。
                ["_"] = "1",
                ["dl"] = "1",
                ["pickcode"] = input.PickCode,
            },
        };
    }

    public static CloudDriveApiContext CreateCreateFolderContext(CreateFolderInput input)
    {
        return new CloudDriveApiContext
        {
            Category = "file-write",
            FormFields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["cname"] = input.FolderName,
                ["pid"] = input.ParentId,
            },
        };
    }

    public static CloudDriveApiContext CreateCopyContext(string fileId, string targetDirectoryId)
    {
        return new CloudDriveApiContext
        {
            Category = "file-write",
            FormFields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["fid[0]"] = fileId,
                ["format"] = "json",
                ["pid"] = targetDirectoryId,
            },
        };
    }

    public static CloudDriveApiContext CreateRenameContext(string fileId, string newFileName)
    {
        return new CloudDriveApiContext
        {
            Category = "file-write",
            FormFields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [$"files_new_name[{fileId}]"] = newFileName,
                ["format"] = "json",
            },
        };
    }

    public static CloudDriveApiContext CreateToggleStarContext(ToggleStarInput input)
    {
        return new CloudDriveApiContext
        {
            Category = "file-write",
            FormFields = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["file_id"] = input.FileId,
                ["format"] = "json",
                ["star"] = input.Star ? "1" : "0",
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
                ["asc"] = "0",
                ["offset"] = input.Offset.ToString(CultureInfo.InvariantCulture),
                ["limit"] = input.Limit.ToString(CultureInfo.InvariantCulture),
                ["format"] = "json",
                ["o"] = "file_name",
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

    public static JsonElement Payload(JsonElement response) =>
        response.ValueKind == JsonValueKind.Object
            && response.TryGetProperty("data", out JsonElement data)
            ? data
            : response;

    public static string? ReadString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out JsonElement value)
            || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    public static long ReadLong(JsonElement element, string propertyName) =>
        long.TryParse(ReadString(element, propertyName), NumberStyles.Integer, CultureInfo.InvariantCulture, out long result)
            ? result
            : 0;

    /// <summary>
    /// 验证已采集写操作或下载响应的成功 envelope，避免未知响应被误报为完成。
    /// </summary>
    /// <param name="response">上游响应。</param>
    /// <param name="operationName">操作名称。</param>
    /// <param name="codePropertyName">成功码字段名称。</param>
    /// <param name="codeKind">成功码字段的 JSON 类型。</param>
    public static void EnsureSuccessEnvelope(
        JsonElement response,
        string operationName,
        string? codePropertyName = null,
        JsonValueKind codeKind = JsonValueKind.Undefined)
    {
        if (response.ValueKind != JsonValueKind.Object
            || !response.TryGetProperty("state", out JsonElement state)
            || state.ValueKind != JsonValueKind.True)
        {
            throw new CloudDriveApiException(
                $"115 {operationName} 返回未知成功响应结构。",
                "unknown_error_shape");
        }

        if (codePropertyName is null)
        {
            return;
        }

        if (!response.TryGetProperty(codePropertyName, out JsonElement code)
            || code.ValueKind != codeKind
            || (codeKind == JsonValueKind.String && !string.IsNullOrEmpty(code.GetString()))
            || (codeKind == JsonValueKind.Number && (!code.TryGetInt64(out long number) || number != 0)))
        {
            throw new CloudDriveApiException(
                $"115 {operationName} 返回未知成功响应结构。",
                "unknown_error_shape");
        }
    }

    /// <summary>
    /// 要求上游响应包含非空字符串字段。
    /// </summary>
    /// <param name="response">上游响应。</param>
    /// <param name="propertyName">字段名称。</param>
    /// <param name="operationName">操作名称。</param>
    /// <returns>字段值。</returns>
    public static string RequireString(JsonElement response, string propertyName, string operationName)
    {
        string? value = ReadString(response, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CloudDriveApiException(
                $"115 {operationName} 缺少成功响应字段。",
                "unknown_error_shape");
        }

        return value;
    }

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
