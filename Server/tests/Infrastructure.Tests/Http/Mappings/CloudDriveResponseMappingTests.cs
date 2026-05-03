using System.Text.Json;

using Infrastructure.Http.Json;
using Infrastructure.Http.Mappings;
using Infrastructure.Http.Responses;

namespace Infrastructure.Tests.Http.Mappings;

public sealed class CloudDriveResponseMappingTests
{
    [Fact]
    public void FileList_ToDto_NormalizesFileAndFolderFields()
    {
        const string json = """
            {
              "count": "2",
              "offset": 10,
              "limit": 2,
              "page_size": 2,
              "cid": "0",
              "is_asc": 0,
              "data": [
                {"fid":"folder-1","n":"Photos","s":"0","fc":0,"pc":"folder-pick","m":1,"iv":0},
                {"fid":"file-1","n":"clip.mp4","s":42,"fc":1,"pc":"file-pick","iv":1,"sha":"ABC","t":1776518000}
              ]
            }
            """;

        CloudDriveFileListResponse response = JsonSerializer.Deserialize<CloudDriveFileListResponse>(
            json,
            CloudDrive115JsonSerializerOptions.Defaults)!;

        var dto = response.ToDto();

        Assert.Equal(2, dto.TotalCount);
        Assert.Equal(10, dto.SkipCount);
        Assert.True(dto.Items.First().IsDirectory);
        Assert.False(dto.Items.Last().IsDirectory);
        Assert.True(dto.Items.Last().IsVideo);
        Assert.Equal("ABC", dto.Items.Last().Sha1);
    }

    [Fact]
    public void RecycleBin_ToDto_UsesRecycleBinItemIdAndReverseTypeMapping()
    {
        const string json = """
            {
              "count": "1",
              "offset": 0,
              "page_size": 30,
              "is_asc": 0,
              "data": {
                "recycled-1": {
                  "file_name": "Deleted Folder",
                  "type": 2,
                  "file_size": "64",
                  "dtime": 1776518000,
                  "cid": "0",
                  "parent_name": "root",
                  "pick_code": "pick",
                  "status": -1
                }
              }
            }
            """;

        CloudDriveRecycleBinResponse response = JsonSerializer.Deserialize<CloudDriveRecycleBinResponse>(
            json,
            CloudDrive115JsonSerializerOptions.Defaults)!;

        var dto = response.ToDto();

        var item = dto.Items.Single();
        Assert.Equal("recycled-1", item.FileId);
        Assert.Equal("recycled-1", item.RecycleBinItemId);
        Assert.True(item.IsDirectory);
        Assert.Equal(-1, item.RestoreStatus);
    }

    [Fact]
    public void OfflineQuota_ToDto_NormalizesStatsFields()
    {
        const string quotaJson = """{"package":{"vip":true},"count":10,"surplus":8,"max_size":"1024","used":"2"}""";
        const string statsJson = """
            {
              "total_count": 9,
              "failed_count": 1,
              "finished_count": 6,
              "downloading_count": 2
            }
            """;

        CloudDriveOfflineQuotaResponse quota = JsonSerializer.Deserialize<CloudDriveOfflineQuotaResponse>(
            quotaJson,
            CloudDrive115JsonSerializerOptions.Defaults)!;
        CloudDriveOfflineStatsResponse stats = JsonSerializer.Deserialize<CloudDriveOfflineStatsResponse>(
            statsJson,
            CloudDrive115JsonSerializerOptions.Defaults)!;

        var dto = quota.ToDto(stats);

        Assert.Equal(9, dto.Total);
        Assert.Equal(1, dto.Failed);
        Assert.Equal(6, dto.Completed);
        Assert.Equal(2, dto.Active);
        Assert.NotNull(dto.Package);
    }

    [Fact]
    public void VideoPlay_ToDto_MapsUserDefDefinitionNewAndAudioTracks()
    {
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

        var dto = response.ToDto();

        Assert.True(dto.IsTemporary);
        Assert.Equal(4, dto.UserDef);
        Assert.Equal(2, dto.SelectedDefinition);
        Assert.Equal(90, dto.UserRotate);
        Assert.Equal(1, dto.UserTurn);
        Assert.Equal("vip_required", dto.RestrictionReason);
        Assert.NotNull(dto.UsageRestrictionNotice);
        Assert.Equal(1080, dto.QualityOptions.Single().DefinitionNew);
        Assert.Equal(1080, dto.QualityOptions.Single().Height);
        Assert.Equal(1920, dto.QualityOptions.Single().Width);
        Assert.Equal(2, dto.AudioTracks.Count);
        Assert.True(dto.AudioTracks.First().IsSelected);
    }
}
