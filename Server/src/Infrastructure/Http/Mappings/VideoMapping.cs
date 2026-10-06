using Application.CloudDrive.Dtos;

using Infrastructure.Http.Responses;

namespace Infrastructure.Http.Mappings;

/// <summary>
/// 115 视频响应模型手动映射。
/// </summary>
internal static class VideoMapping
{
    public static CloudDriveVideoPlayDto ToDto(this CloudDriveVideoPlayResponse response)
    {
        return new CloudDriveVideoPlayDto
        {
            Status = VideoPlayStatus.SchemaNotCollected,
            QualityOptions = [],
            AudioTracks = [],
            IsTemporary = true,
            PlaybackSchemaStatus = "not_collected",
            UsageRestrictionNotice = "视频播放成功响应 schema 尚未采集，暂不返回播放地址。",
        };
    }

    public static VideoQualityOption ToDto(
        this CloudDriveVideoUrlVariantResponse response,
        IReadOnlyList<CloudDriveVideoDefinitionResponse>? definitions = null)
    {
        CloudDriveVideoDefinitionResponse? definition = definitions?
            .FirstOrDefault(item => item.Definition == response.Definition);

        return new VideoQualityOption
        {
            Definition = response.Definition ?? definition?.Definition ?? 0,
            DefinitionNew = response.DefinitionNew ?? definition?.DefinitionNew,
            Title = response.DefinitionName ?? definition?.Name,
            PlayUrl = response.Url,
            Height = response.Height,
            Width = response.Width,
        };
    }

    public static VideoQualityOption ToDto(this CloudDriveVideoDefinitionResponse response)
    {
        return new VideoQualityOption
        {
            Definition = response.Definition,
            DefinitionNew = response.DefinitionNew,
            Title = response.Name,
        };
    }

    public static AudioTrackOption ToDto(this CloudDriveVideoAudioTrackResponse response)
    {
        return new AudioTrackOption
        {
            TrackIndex = response.TrackIndex,
            Title = response.Title ?? string.Empty,
            IsSelected = response.IsSelected ?? false,
        };
    }

    public static CloudDriveVideoTranscodeDto ToDto(this CloudDriveVideoPushResponse response, string fileId)
    {
        return new CloudDriveVideoTranscodeDto
        {
            FileId = fileId,
            Status = response.Data ?? response.Request ?? string.Empty,
        };
    }

    public static CloudDriveSubtitleListDto ToDto(this CloudDriveSubtitleResponse response)
    {
        return new CloudDriveSubtitleListDto
        {
            AutoloadSubtitle = null,
            Items = [],
            ItemSchemaStatus = "not_collected",
        };
    }
}
