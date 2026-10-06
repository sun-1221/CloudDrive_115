using System.Text.Json;

using Infrastructure.Http.Json;
using Infrastructure.Http.Responses;

namespace Infrastructure.Tests.Http.Responses;

/// <summary>
/// 115 开放平台响应模型反序列化测试。
/// </summary>
public sealed class CloudDrive115ResponseModelTests
{
    /// <summary>
    /// 验证通用 Web 响应可反序列化混合类型 state 字段。
    /// </summary>
    [Theory]
    [InlineData("""{"state":true,"code":0,"message":"ok"}""", true)]
    [InlineData("""{"state":1,"code":"0","msg":"ok"}""", true)]
    [InlineData("""{"state":0,"errNo":401,"error":"login required"}""", false)]
    public void CloudDrive115WebResponse_WhenStateHasMixedTypes_DeserializesBoolAndNumberState(string json, bool expectedState)
    {
        // Arrange（准备）

        // Act（执行）
        CloudDrive115WebResponse<JsonElement>? response = JsonSerializer.Deserialize<CloudDrive115WebResponse<JsonElement>>(
            json,
            CloudDrive115JsonSerializerOptions.Defaults);

        // Assert（断言）
        Assert.NotNull(response);
        Assert.Equal(expectedState, response.State);
    }

    /// <summary>
    /// 验证文件列表响应会规范化 Web 文件字段。
    /// </summary>
    [Fact]
    public void CloudDriveFileListResponse_WhenWebFieldsReturned_NormalizesWebFileFields()
    {
        // Arrange（准备）
        const string json = """
            {
              "count": "2",
              "page_size": 2,
              "cid": "0",
              "offset": 0,
              "limit": 2,
              "is_asc": 0,
              "path": [{"cid":"0","name":"root"}],
              "data": [
                {"fid":"folder-1","n":"Photos","s":"0","fc":0,"pc":"folder-pick","m":1,"iv":0},
                {"fid":"file-1","n":"clip.mp4","s":42,"fc":1,"pc":"file-pick","iv":1,"sha":"ABC"}
              ]
            }
            """;

        // Act（执行）
        CloudDriveFileListResponse? response = JsonSerializer.Deserialize<CloudDriveFileListResponse>(
            json,
            CloudDrive115JsonSerializerOptions.Defaults);

        // Assert（断言）
        Assert.NotNull(response);
        Assert.Equal(2, response.Count);
        Assert.Equal("0", response.Path.Single().DirectoryId);

        CloudDrive115FileItem folder = response.Items[0];
        Assert.Equal("folder-1", folder.FileId);
        Assert.Equal("Photos", folder.Name);
        Assert.Null(folder.IsDirectory);
        Assert.True(folder.IsStarred);

        CloudDrive115FileItem file = response.Items[1];
        Assert.Equal("file-1", file.FileId);
        Assert.Equal(42, file.Size);
        Assert.Null(file.IsDirectory);
        Assert.True(file.IsVideo);
        Assert.Equal("ABC", file.Sha1);
    }

    /// <summary>
    /// 验证回收站响应会反序列化顶层字段与非空条目。
    /// </summary>
    [Fact]
    public void CloudDriveRecycleBinResponse_WhenTopLevelFieldsReturned_DeserializesTopLevelFieldsOnly()
    {
        // Arrange（准备）
        const string json = """
            {
              "count": "1",
              "rb_pass": 0,
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

        // Act（执行）
        CloudDriveRecycleBinResponse? response = JsonSerializer.Deserialize<CloudDriveRecycleBinResponse>(
            json,
            CloudDrive115JsonSerializerOptions.Defaults);

        // Assert（断言）
        Assert.NotNull(response);
        Assert.Equal(1, response.Count);

        Assert.Equal(0, response.RecycleBinPasswordEnabled);
        Assert.Equal(30, response.PageSize);
        Assert.Equal(0, response.IsAscending);
        CloudDriveRecycleBinItemResponse item = Assert.Single(response.Items);
        Assert.Equal("recycled-1", item.Id);
        Assert.Equal("Deleted Folder", item.FileName);
    }

    /// <summary>
    /// 验证上传初始化响应可区分秒传和 OSS 凭据。
    /// </summary>
    [Fact]
    public void CloudDriveUploadInitResponse_WhenDifferentUploadModesReturned_DistinguishesInstantUploadAndOssCredentials()
    {
        // Arrange（准备）
        const string instantJson = """{"file_id":"3409","file_name":"a.txt","pick_code":"abc","sha1":"SHA","cid":"0"}""";
        const string ossJson = """
            {
              "object":"obj",
              "accessid":"access",
              "host":"https://oss.example.com",
              "policy":"policy",
              "signature":"signature",
              "expire":"1776518659",
              "callback":"callback"
            }
            """;

        // Act（执行）
        CloudDriveUploadInitResponse? instant = JsonSerializer.Deserialize<CloudDriveUploadInitResponse>(
            instantJson,
            CloudDrive115JsonSerializerOptions.Defaults);
        CloudDriveUploadInitResponse? oss = JsonSerializer.Deserialize<CloudDriveUploadInitResponse>(
            ossJson,
            CloudDrive115JsonSerializerOptions.Defaults);

        // Assert（断言）
        Assert.NotNull(instant);
        Assert.True(instant.IsInstantUpload);
        Assert.False(instant.HasOssCredential);

        Assert.NotNull(oss);
        Assert.False(oss.IsInstantUpload);
        Assert.True(oss.HasOssCredential);
        Assert.Equal(1776518659L, oss.Expire);
    }

    /// <summary>
    /// 验证 VIP 卡响应可映射数字开头的余额字段。
    /// </summary>
    [Fact]
    public void CloudDriveVipCardResponse_WhenBalanceFieldStartsWithNumber_MapsNumericLeadingBalanceField()
    {
        // Arrange（准备）
        const string json = """
            {
              "num": 1,
              "vip_count": 2,
              "space_count": 3,
              "forever_count": 4,
              "sqcode_count": "5",
              "sqcode_cancel_count": "6",
              "00_balance": 700
            }
            """;

        // Act（执行）
        CloudDriveVipCardResponse? response = JsonSerializer.Deserialize<CloudDriveVipCardResponse>(
            json,
            CloudDrive115JsonSerializerOptions.Defaults);

        // Assert（断言）
        Assert.NotNull(response);
        Assert.Equal(700, response.Balance);
        Assert.Equal(5, response.SqCodeCount);
        Assert.Equal(6, response.SqCodeCancelCount);
    }

    /// <summary>
    /// 验证离线任务列表响应会规范化任务对象字段。
    /// </summary>
    [Fact]
    public void CloudDriveOfflineTaskListResponse_WhenTaskObjectsReturned_NormalizesTaskObjectFields()
    {
        // Arrange（准备）
        const string json = """
            {
              "page": 1,
              "page_count": 1,
              "page_row": 30,
              "page_size": 30,
              "count": 1,
              "quota": 10,
              "total": 1,
              "real_state": 1,
              "tasks": {
                "hash-1": {
                  "info_hash": "hash-1",
                  "name": "ubuntu.iso",
                  "size": "1024",
                  "add_time": 1776518000,
                  "last_update": 1776518100,
                  "percentDone": "99.5",
                  "file_id": "file-1",
                  "wp_path_id": "0",
                  "def2": "HD",
                  "play_long": "120",
                  "status": 1
                }
              }
            }
            """;

        // Act（执行）
        CloudDriveOfflineTaskListResponse? response = JsonSerializer.Deserialize<CloudDriveOfflineTaskListResponse>(
            json,
            CloudDrive115JsonSerializerOptions.Defaults);

        // Assert（断言）
        Assert.NotNull(response);
        Assert.True(response.RealState);

        CloudDriveOfflineTaskResponse task = response.Tasks.Single();
        Assert.Equal("hash-1", task.InfoHash);
        Assert.Equal(1024, task.Size);
        Assert.Equal(99.5m, task.PercentDone);
        Assert.Equal("0", task.ParentId);
        Assert.Equal(120, task.PlayDuration);
    }
}
