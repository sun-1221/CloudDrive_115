using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.User;
using Application.Queries;
using Application.Tests.Support;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.User;

public sealed class UserAggregationHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private const string TestCloudAccountUid = "115001";

    [Fact]
    public async Task GetUserSpaceQueryHandler_WhenAggregationHasSpace_ReturnsSpaceInfo()
    {
        // Arrange
        RecordingCloudDriveOpenApiGateway gateway = CreateUserGateway();
        UserAggregationService service = CreateService(gateway);
        var accessor = CreateAccessor();
        IQueryHandler<GetUserSpaceQuery, CloudDriveSpaceInfoDto> handler =
            new GetUserSpaceQueryHandler(service, accessor, CreateEnabledConfig());

        // Act
        Result<CloudDriveSpaceInfoDto> result = await handler.Handle(
            new GetUserSpaceQuery(TestUserId),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("available", result.Value.SchemaStatus);
        Assert.Contains(gateway.WebApiRequests, request => request.Endpoint == "files/index_info");
    }

    [Fact]
    public async Task GetUserVipQueryHandler_WhenAggregationHasVip_ReturnsVipInfo()
    {
        // Arrange
        RecordingCloudDriveOpenApiGateway gateway = CreateUserGateway();
        UserAggregationService service = CreateService(gateway);
        var accessor = CreateAccessor();
        IQueryHandler<GetUserVipQuery, CloudDriveVipInfoDto> handler =
            new GetUserVipQueryHandler(service, accessor, CreateEnabledConfig());

        // Act
        Result<CloudDriveVipInfoDto> result = await handler.Handle(
            new GetUserVipQuery(TestUserId),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("VIP", result.Value.LevelName);
        Assert.Contains(gateway.UserCenterRequests, request =>
            request.Endpoint == "."
            && request.Context.QueryParameters!["ct"] == "order"
            && request.Context.QueryParameters["ac"] == "get_yun_card");
        Assert.Contains(gateway.UserCenterRequests, request =>
            request.Endpoint == "."
            && request.Context.QueryParameters!["ct"] == "balance"
            && request.Context.QueryParameters["ac"] == "getBalance");
    }

    private static RecordingCloudDriveOpenApiGateway CreateUserGateway()
    {
        var gateway = new RecordingCloudDriveOpenApiGateway();
        gateway.RespondWeb("files/index_info", """{"state":true,"data":{"all_total":"10 GB","all_use":"2 GB","all_remain":"8 GB","all_total_size":10,"all_use_size":2,"all_remain_size":8}}""");
        gateway.RespondWeb("user/vip_limit", """{"state":true,"data":{"level_name":"VIP","expire":"1893456000"}}""");
        gateway.RespondUserCenter(".", """{"state":true,"data":{"vip_count":2,"00_balance":88}}""");
        return gateway;
    }

    private static UserAggregationService CreateService(RecordingCloudDriveOpenApiGateway gateway)
    {
        return new UserAggregationService(
            gateway,
            CreateEnabledConfig(),
            new FakeLogger<UserAggregationService>());
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
            MyApiBaseUrl = "https://my.115.com/",
            VipApiBaseUrl = "https://vip.115.com/",
            PayApiBaseUrl = "https://pay.115.com/",
        });
    }
}
