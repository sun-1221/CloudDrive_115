namespace WebApi.Models;

public sealed class CreateFolderRequest
{
    public string ParentId { get; set; } = "0";

    public string FolderName { get; set; } = string.Empty;
}

public sealed class FileIdsRequest
{
    public List<string> FileIds { get; set; } = [];
}

public sealed class MoveFilesRequest
{
    public List<string> FileIds { get; set; } = [];

    public string TargetDirectoryId { get; set; } = string.Empty;
}

public sealed class RenameFileRequest
{
    public string NewFileName { get; set; } = string.Empty;
}

public sealed class ToggleStarRequest
{
    public bool Starred { get; set; }
}

public sealed class RecycleBinItemsRequest
{
    public List<string> RecycleBinItemIds { get; set; } = [];
}

public sealed class VideoProgressRequest
{
    public long Time { get; set; }

    public bool WatchEnd { get; set; }
}

public sealed class OfflineUrlTasksRequest
{
    public List<string> Urls { get; set; } = [];

    public string? SaveDirectoryId { get; set; }
}

public sealed class OfflineBtTaskRequest
{
    public string InfoHash { get; set; } = string.Empty;

    public string? SavePath { get; set; }

    public string? SaveDirectoryId { get; set; }
}

public sealed class ClearOfflineTasksRequest
{
    public int? Stat { get; set; }
}
