using System.Text.Json;

using Application.CloudDrive.Dtos;

using Infrastructure.Http.Json;
using Infrastructure.Http.Mappings;
using Infrastructure.Http.Responses;

namespace Infrastructure.Tests.Http.Mappings;

/// <summary>
/// 115 网盘响应模型到应用 DTO 的映射测试。
/// </summary>
public sealed class CloudDriveResponseMappingTests
{
    /// <summary>
    /// 验证文件列表映射会规范化文件和文件夹字段。
    /// </summary>
    [Fact]
    public void FileList_ToDto_NormalizesFileAndFolderFields()
    {
        // Arrange（准备）
        const string json = """
            {
              "count": "2",
              "offset": 10,
              "limit": 2,
              "page_size": 2,
              "cid": "0",
              "is_asc": 0,
              "path": [{"aid":"1","cid":"0","isp":"1","name":"root","pid":"-1"}],
              "data": [
                {"fid":"folder-1","n":"Photos","s":"0","fc":0,"pc":"folder-pick","m":1,"iv":0,"pid":"0"},
                {"fid":"file-1","n":"clip.mp4","s":42,"fc":1,"pc":"file-pick","iv":1,"sha":"ABC","t":1776518000,"pid":"folder-1"}
              ]
            }
            """;

        CloudDriveFileListResponse response = JsonSerializer.Deserialize<CloudDriveFileListResponse>(
            json,
            CloudDrive115JsonSerializerOptions.Defaults)!;

        // Act（执行）
        var dto = response.ToDto();

        // Assert（断言）
        Assert.Equal(2, dto.TotalCount);
        Assert.Equal(10, dto.SkipCount);
        Assert.Null(dto.Items.First().IsDirectory);
        Assert.Null(dto.Items.Last().IsDirectory);
        Assert.True(dto.Items.Last().IsVideo);
        Assert.Equal("ABC", dto.Items.Last().Sha1);
        Assert.Equal("folder-1", dto.Items.Last().ParentId);
        Assert.Equal("1", dto.Paths![0].Aid);
        Assert.Equal("1", dto.Paths[0].IsSelectedPath);
        Assert.Equal("-1", dto.Paths[0].ParentId);
    }

    /// <summary>
    /// 验证回收站映射会保留顶层字段并映射非空条目。
    /// </summary>
    [Fact]
    public void RecycleBin_ToDto_MapsTopLevelFieldsAndLeavesItemsNotCollected()
    {
        // Arrange（准备）
        const string json = """
            {
              "count": "1",
              "offset": 0,
              "page_size": 30,
              "is_asc": 0,
              "data": [
                {
                  "id": "recycled-1",
                  "file_name": "Deleted Folder",
                  "type": "2",
                  "file_size": "64",
                  "dtime": "1776518000",
                  "cid": "0",
                  "parent_name": "root",
                  "status": "-1"
                }
              ]
            }
            """;

        CloudDriveRecycleBinResponse response = JsonSerializer.Deserialize<CloudDriveRecycleBinResponse>(
            json,
            CloudDrive115JsonSerializerOptions.Defaults)!;

        // Act（执行）
        var dto = response.ToDto();

        // Assert（断言）
        CloudDriveRecycleBinItemDto item = Assert.Single(dto.Items);
        Assert.Equal("recycled-1", item.Id);
        Assert.Equal("Deleted Folder", item.FileName);
        Assert.Equal("64", item.FileSize);
        Assert.Equal("root", item.ParentName);
        Assert.Equal("available", item.SchemaStatus);
        Assert.Equal(1, dto.Count);
        Assert.Equal(30, dto.PageSize);
        Assert.Equal("available", dto.ItemSchemaStatus);
    }

    /// <summary>
    /// 验证离线配额映射会规范化配额字段。
    /// </summary>
    [Fact]
    public void OfflineQuota_ToDto_NormalizesQuotaFields()
    {
        // Arrange（准备）
        const string quotaJson = """{"package":{"vip":true},"count":10,"surplus":8,"max_size":"1024","used":"2"}""";

        CloudDriveOfflineQuotaResponse quota = JsonSerializer.Deserialize<CloudDriveOfflineQuotaResponse>(
            quotaJson,
            CloudDrive115JsonSerializerOptions.Defaults)!;

        // Act（执行）
        var dto = quota.ToDto();

        // Assert（断言）
        Assert.NotNull(dto.Package);
        Assert.Equal(10, dto.Count);
        Assert.Equal(8, dto.Surplus);
        Assert.Equal(1024L, dto.MaxSize);
        Assert.Equal(2L, dto.Used);
    }

    /// <summary>
    /// 验证离线统计映射会规范化统计字段。
    /// </summary>
    [Fact]
    public void OfflineStats_ToDto_NormalizesStatsFields()
    {
        // Arrange（准备）
        const string statsJson = """
            {
              "total_count": 9,
              "failed_count": 1,
              "finished_count": 6,
              "downloading_count": 2
            }
            """;

        CloudDriveOfflineStatsResponse stats = JsonSerializer.Deserialize<CloudDriveOfflineStatsResponse>(
            statsJson,
            CloudDrive115JsonSerializerOptions.Defaults)!;

        // Act（执行）
        var dto = stats.ToDto();

        // Assert（断言）
        Assert.Equal(9, dto.Total);
        Assert.Equal(1, dto.Failed);
        Assert.Equal(6, dto.Completed);
        Assert.Equal(2, dto.Active);
    }

    /// <summary>
    /// 验证搜索结构已采集时映射可用状态与搜索结果项。
    /// </summary>
    [Fact]
    public void FileSearch_ToDto_WhenSchemaCollected_MapsItemsWithAvailableStatus()
    {
        // Arrange（准备）
        const string json = """
            {
              "count": "1",
              "offset": 0,
              "page_size": 20,
              "page_count": 1,
              "is_asc": 0,
              "order": "user_ptime",
              "suffix": "",
              "fc_mix": 0,
              "type": 0,
              "state": true,
              "data": [
                {"fid":"file-1","n":"<em>result</em>.txt","s":42,"fc":1,"pc":"pick-1","sha":"abc","ns":"<em>result</em>.txt","dp":"My Files","iv":0}
              ],
              "folder": []
            }
            """;

        CloudDriveFileSearchResponse response = JsonSerializer.Deserialize<CloudDriveFileSearchResponse>(
            json,
            CloudDrive115JsonSerializerOptions.Defaults)!;

        // Act（执行）
        var dto = response.ToDto();

        // Assert（断言）
        Assert.Equal("available", dto.ItemSchemaStatus);
        Assert.Single(dto.Items);
        Assert.Equal("file-1", dto.Items[0].FileId);
        Assert.Equal("<em>result</em>.txt", dto.Items[0].HighlightNameRaw);
        Assert.Equal("My Files", dto.Items[0].DirectoryName);
        Assert.Equal("available", dto.Items[0].SchemaStatus);
        Assert.Equal(1, dto.Count);
        Assert.Equal(20, dto.PageSize);
        Assert.True(dto.State);
    }

    /// <summary>
    /// 验证搜索结构未采集时返回空结果项并标记未采集。
    /// </summary>
    [Fact]
    public void FileSearch_ToDto_WhenSchemaUncollected_ReturnsEmptyItemsWithNotCollectedStatus()
    {
        // Arrange（准备）
        const string json = """
            {
              "count": "5",
              "offset": 0,
              "page_size": 20,
              "page_count": 1,
              "is_asc": 1,
              "order": "file_name",
              "state": true,
              "data": [
                {"fid":"file-1","n":"demo.mp4","s":100}
              ]
            }
            """;

        CloudDriveFileSearchResponse response = JsonSerializer.Deserialize<CloudDriveFileSearchResponse>(
            json,
            CloudDrive115JsonSerializerOptions.Defaults)!;

        // Act（执行）
        var dto = response.ToDto(includeItems: false);

        // Assert（断言）
        Assert.Equal("not_collected", dto.ItemSchemaStatus);
        Assert.Empty(dto.Items);
        Assert.Equal(5, dto.Count);
        Assert.Equal(20, dto.PageSize);
        Assert.Equal(1, dto.IsAsc);
        Assert.Equal("file_name", dto.Order);
    }

    /// <summary>
    /// 验证视频播放结构未采集时不暴露播放明细字段。
    /// </summary>
    [Fact]
    public void VideoPlay_ToDto_WhenSchemaUncollected_DoesNotExposePlaybackFields()
    {
        // Arrange（准备）
        const string json = """
            {
              "play_long": 120,
              "definition": 2,
              "user_def": 4,
              "user_rotate": 90,
              "user_turn": 1,
              "restriction_reason": "vip_required",
              "video_url": [
                {"definition":4,"definition_n":1080,"title":"1080P","url":"https://play.example/video.m3u8","height":1080,"width":1920}
              ],
              "definition_list_new": [
                {"definition":4,"definition_n":1080,"name":"1080P"}
              ],
              "multitrack_list": [
                {"track":0,"title":"Main","selected":1},
                {"track":1,"title":"Commentary","selected":0}
              ]
            }
            """;

        CloudDriveVideoPlayResponse response = JsonSerializer.Deserialize<CloudDriveVideoPlayResponse>(
            json,
            CloudDrive115JsonSerializerOptions.Defaults)!;

        // Act（执行）
        var dto = response.ToDto();

        // Assert（断言）
        Assert.Equal(VideoPlayStatus.SchemaNotCollected, dto.Status);
        Assert.True(dto.IsTemporary);
        Assert.Equal("not_collected", dto.PlaybackSchemaStatus);
        Assert.NotNull(dto.UsageRestrictionNotice);
        Assert.Empty(dto.QualityOptions);
        Assert.Empty(dto.AudioTracks);
        Assert.Null(dto.Duration);
        Assert.Null(dto.UserDef);
        Assert.Null(dto.SelectedDefinition);
        Assert.Null(dto.UserRotate);
        Assert.Null(dto.UserTurn);
        Assert.Null(dto.RestrictionReason);
        Assert.Null(dto.PlaybackMetadata);
    }
}
