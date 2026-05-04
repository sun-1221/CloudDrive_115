using Application.CloudDrive;
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
    public async Task GetMySharesQueryHandler_WhenListReturned_MapsItems()
    {
        // Arrange
        var gateway = new RecordingCloudDriveOpenApiGateway();
        gateway.RespondWeb("share/slist", """
            {"state":true,"count":1,"list":[{"share_code":"s1","share_title":"Demo","file_id":"file-1","file_name":"demo.txt","pick_code":"pick-1","file_size":"42","create_time":1,"expire_time":2,"url":"https://share.test/s1"}]}
            """);
        var handler = new GetMySharesQueryHandler(gateway, CreateEnabledConfig());

        // Act
        Result<PagedResultDto<CloudDriveShareItemDto>> result = await handler.Handle(
            new GetMySharesQuery(
                TestUserId,
                new GetShareListInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    Offset = 0,
                    Limit = 30,
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("s1", result.Value.Items.Single().ShareCode);
        CloudDriveGatewayRequest request = Assert.Single(gateway.WebApiRequests);
        Assert.Equal("share/slist", request.Endpoint);
        Assert.Equal("json", request.Context.QueryParameters!["format"]);
    }

    [Fact]
    public async Task GetShareSendsQueryHandler_WhenSampleShapeMissing_ReturnsSampleUnavailable()
    {
        // Arrange
        var gateway = new RecordingCloudDriveOpenApiGateway();
        gateway.RespondWeb("share/send", """{"state":true,"data":{}}""");
        var handler = new GetShareSendsQueryHandler(gateway, CreateEnabledConfig());

        // Act
        Result<PagedResultDto<CloudDriveShareSendItemDto>> result = await handler.Handle(
            new GetShareSendsQuery(
                TestUserId,
                new GetShareListInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    Offset = 0,
                    Limit = 30,
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("share_sample_unavailable", result.Error.Code);
    }

    private static IOptions<CloudDriveAppConfig> CreateEnabledConfig()
    {
        return Options.Create(new CloudDriveAppConfig
        {
            CloudDriveFeatureEnabled = true,
        });
    }
}
