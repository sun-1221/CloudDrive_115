namespace Domain.Enums;

/// <summary>
/// 115 网盘后台任务类型。
/// </summary>
public enum CloudDriveJobType
{
    /// <summary>文件上传（大文件异步上传）。</summary>
    Upload = 0,

    /// <summary>批量复制。</summary>
    BatchCopy = 1,

    /// <summary>批量移动。</summary>
    BatchMove = 2,

    /// <summary>批量删除。</summary>
    BatchDelete = 3,

    /// <summary>批量回收站还原。</summary>
    BatchRecycleBinRestore = 4,

    /// <summary>批量回收站彻底删除。</summary>
    BatchRecycleBinDelete = 5,

    /// <summary>清空回收站。</summary>
    ClearRecycleBin = 6,

    /// <summary>视频转码。</summary>
    Transcode = 7
}
