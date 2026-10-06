using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Share;
using Application.Tests.Support;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Share;

public sealed class ShareHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private const string TestCloudAccountUid = "115001";

    [Fact]
    public async Task GetMySharesQueryHandler_WhenListReturned_MapsShareItems()
    {
        // Arrange
        var gateway = new RecordingCloudDriveOpenApiGateway();
        gateway.RespondWeb("share/slist", """
            {"state":true,"count":1,"list":[{"share_code":"s1","share_title":"Demo","file_id":"file-1","file_name":"demo.txt","pick_code":"pick-1","file_size":"42","create_time":1,"expire_time":2,"url":"https://share.test/s1"}]}
            """);
        var accessor = CreateAccessor();
        var handler = new GetMySharesQueryHandler(gateway, accessor, CreateEnabledConfig());

        // Act
        Result<CloudDriveShareListDto> result = await handler.Handle(
            new GetMySharesQuery(
                TestUserId,
                new GetShareListInput
                {
                    Offset = 0,
                    Limit = 30,
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("available", result.Value.ItemSchemaStatus);
        Assert.Equal(1, result.Value.Count);
        CloudDriveShareItemDto item = Assert.Single(result.Value.Items);
        Assert.Equal("s1", item.ShareCode);
        Assert.Equal("Demo", item.ShareTitle);
        Assert.Equal("file-1", item.FileId);
        Assert.Equal("demo.txt", item.FileName);
        Assert.Equal("pick-1", item.PickCode);
        Assert.Equal(42, item.FileSize);
        Assert.NotNull(item.CreateTime);
        Assert.NotNull(item.ExpireTime);
        Assert.Equal("https://share.test/s1", item.Url);
        Assert.Equal("available", item.SchemaStatus);
        CloudDriveGatewayRequest request = Assert.Single(gateway.WebApiRequests);
        Assert.Equal("share/slist", request.Endpoint);
        Assert.Equal("json", request.Context.QueryParameters!["format"]);
    }

    [Fact]
    public async Task GetShareSendsQueryHandler_WhenCalled_ReturnsNotCollected()
    {
        // Arrange
        var gateway = new RecordingCloudDriveOpenApiGateway();
        var handler = new GetShareSendsQueryHandler();

        // Act
        Result<CloudDriveShareListDto> result = await handler.Handle(
            new GetShareSendsQuery(
                TestUserId,
                new GetShareListInput
                {
                    Offset = 0,
                    Limit = 30,
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_api_not_collected", result.Error.Code);
        Assert.Empty(gateway.WebApiRequests);
    }

    private static CloudDriveAccountContextAccessor CreateAccessor()
    {
        var accessor = new CloudDriveAccountContextAccessor();
        accessor.SetContext(new CloudDriveAccountContext(TestUserId, TestCloudAccountUid, TestCloudAccountUid));
        return accessor;
    }

    private static IOptions<CloudDriveAppConfig> CreateEnabledConfig()
    {
        return Options.Create(new CloudDriveAppConfig
        {
            CloudDriveFeatureEnabled = true,
        });
    }
}
