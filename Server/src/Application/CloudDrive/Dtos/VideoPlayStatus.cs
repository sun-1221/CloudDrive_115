namespace Application.CloudDrive.Dtos;

/// <summary>
/// 视频播放状态。
/// </summary>
public enum VideoPlayStatus
{
    /// <summary>
    /// 成功播放 schema 尚未采集。
    /// </summary>
    SchemaNotCollected,

    /// <summary>
    /// 已可播放。
    /// </summary>
    Ready,

    /// <summary>
    /// 正在转码。
    /// </summary>
    Transcoding
}
