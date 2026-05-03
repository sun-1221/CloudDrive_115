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
        IReadOnlyList<VideoQualityOption> qualityOptions = response.VideoUrls
            .Select(item => item.ToDto(response.AvailableDefinitions))
            .ToList();

        return new CloudDriveVideoPlayDto
        {
            Status = string.Equals(response.Status, "transcoding", StringComparison.OrdinalIgnoreCase)
                ? VideoPlayStatus.Transcoding
                : VideoPlayStatus.Ready,
            QualityOptions = qualityOptions,
            AudioTracks = response.AudioTracks.Select(track => track.ToDto()).ToList(),
            Duration = checked((int)Math.Min(response.PlayDuration ?? 0, int.MaxValue)),
            UserDef = response.UserDef ?? response.UserSelectedDefinition,
            IsTemporary = true,
            SelectedDefinition = response.UserSelectedDefinition,
            UserRotate = response.UserRotate,
            UserTurn = response.UserTurn,
            RestrictionReason = response.RestrictionReason,
            UsageRestrictionNotice = "播放地址仅限当前已绑定用户在本系统内短时使用。",
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
            AutoloadSubtitle = response.AutoLoad.FirstOrDefault()?.ToDto(),
            Items = response.AutoLoad.Concat(response.Items).Select(item => item.ToDto()).ToList(),
        };
    }

    public static CloudDriveSubtitleDto ToDto(this CloudDriveSubtitleItemResponse response)
    {
        return new CloudDriveSubtitleDto
        {
            SubtitleId = response.Id ?? string.Empty,
            Language = response.Language ?? string.Empty,
            Title = response.Title ?? response.FileName ?? string.Empty,
            SubtitleUrl = response.Url ?? string.Empty,
            FileName = response.FileName,
        };
    }
}
