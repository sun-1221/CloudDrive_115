using Application.Abstractions.CloudDrive;
using Application.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Offline;
using Application.Tests.Support;

using Domain.Exceptions;
using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Offline;

public sealed class OfflineHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private const string TestCloudAccountUid = "115001";

    [Fact]
    public async Task GetOfflineTaskListQueryHandler_WhenTasksReturned_MapsPagedTasks()
    {
        // Arrange
        var gateway = new RecordingCloudDriveOpenApiGateway();
        gateway.RespondOffline("task_lists", """
            {"state":true,"tasks":[{"info_hash":"hash-1","name":"demo","size":"100","status":2,"percentDone":"100","file_id":"file-1","wp_path_id":"0","def2":"HD","play_long":60}],"count":"1","page_size":"10"}
            """);
        var accessor = CreateAccessor();
        var handler = new GetOfflineTaskListQueryHandler(gateway, accessor, CreateEnabledConfig());

        // Act
        Result<CloudDriveOfflineTaskListDto> result = await handler.Handle(
            new GetOfflineTaskListQuery(
                TestUserId,
                new GetOfflineTaskListInput
                {
                    Page = 1,
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal("not_collected", result.Value.ItemSchemaStatus);
        Assert.Equal(1, result.Value.Count);
        CloudDriveGatewayRequest request = Assert.Single(gateway.OfflineRequests);
        Assert.Equal("task_lists", request.Endpoint);
        Assert.Equal("0", request.Context.FormFields!["stat"]);
    }

    [Fact]
    public async Task GetOfflineQuotaQueryHandler_WhenQuotaReturned_DoesNotCallStats()
    {
        // Arrange
        var gateway = new RecordingCloudDriveOpenApiGateway();
        gateway.RespondOffline("get_quota_package_info", """{"state":true,"package":{"name":"basic"},"count":10,"surplus":8,"max_size":1024,"used":2}""");
        gateway.RespondOffline("task_count", """{"state":true,"total_count":5,"failed_count":1,"finished_count":3,"downloading_count":1}""");
        var accessor = CreateAccessor();
        var handler = new GetOfflineQuotaQueryHandler(gateway, accessor, CreateEnabledConfig());

        // Act
        Result<CloudDriveOfflineQuotaDto> result = await handler.Handle(
            new GetOfflineQuotaQuery(TestUserId),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.Count);
        Assert.Equal(["get_quota_package_info"], gateway.OfflineRequests.Select(request => request.Endpoint));
    }

    [Fact]
    public async Task GetOfflineStatsQueryHandler_WhenStatsReturned_MapsStatistics()
    {
        // Arrange
        var gateway = new RecordingCloudDriveOpenApiGateway();
        gateway.RespondOffline("task_count", """{"state":true,"total_count":5,"failed_count":1,"finished_count":3,"downloading_count":1}""");
        var accessor = CreateAccessor();
        var handler = new GetOfflineStatsQueryHandler(gateway, accessor, CreateEnabledConfig());

        // Act
        Result<CloudDriveOfflineStatsDto> result = await handler.Handle(
            new GetOfflineStatsQuery(TestUserId),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Total);
        Assert.Equal(3, result.Value.Completed);
        Assert.Equal("task_count", gateway.OfflineRequests.Single().Endpoint);
    }

    [Fact]
    public async Task AddOfflineTaskCommandHandler_WhenApiNotCollected_ReturnsBusinessFailureWithoutRequest()
    {
        // Arrange
        var gateway = new RecordingCloudDriveOpenApiGateway();
        var handler = new AddOfflineTaskCommandHandler(CreateEnabledConfig());

        // Act
        Result<CloudDriveOperationResultDto> result = await handler.Handle(
            new AddOfflineTaskCommand(
                TestUserId,
                new AddOfflineTaskInput
                {
                    Urls = ["https://example.test/file.iso"],
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_api_not_collected", result.Error.Code);
        Assert.Empty(gateway.OfflineRequests);
    }

    [Fact]
    public async Task CancelOfflineTaskCommandHandler_WhenApiNotCollected_ReturnsBusinessFailureWithoutRequest()
    {
        // Arrange
        var gateway = new RecordingCloudDriveOpenApiGateway();
        var handler = new CancelOfflineTaskCommandHandler(CreateEnabledConfig());

        // Act
        Result<CloudDriveOperationResultDto> result = await handler.Handle(
            new CancelOfflineTaskCommand(
                TestUserId,
                new CancelOfflineTaskInput
                {
                    InfoHash = "hash-1",
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_api_not_collected", result.Error.Code);
        Assert.Empty(gateway.OfflineRequests);
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
