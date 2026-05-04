using Application.CloudDrive;
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
        var handler = new GetOfflineTaskListQueryHandler(gateway, CreateEnabledConfig());

        // Act
        Result<PagedResultDto<CloudDriveOfflineTaskDto>> result = await handler.Handle(
            new GetOfflineTaskListQuery(
                TestUserId,
                new GetOfflineTaskListInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    Page = 1,
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("success", result.Value.Items.Single().Status);
        CloudDriveGatewayRequest request = Assert.Single(gateway.OfflineRequests);
        Assert.Equal("task_lists", request.Endpoint);
        Assert.Equal("0", request.Context.FormFields!["stat"]);
    }

    [Fact]
    public async Task GetOfflineQuotaQueryHandler_WhenQuotaAndStatsReturned_MergesStatistics()
    {
        // Arrange
        var gateway = new RecordingCloudDriveOpenApiGateway();
        gateway.RespondOffline("get_quota_package_info", """{"state":true,"package":{"name":"basic"},"count":10,"surplus":8,"max_size":1024,"used":2}""");
        gateway.RespondOffline("task_count", """{"state":true,"total_count":5,"failed_count":1,"finished_count":3,"downloading_count":1}""");
        var handler = new GetOfflineQuotaQueryHandler(gateway, CreateEnabledConfig());

        // Act
        Result<CloudDriveOfflineQuotaDto> result = await handler.Handle(
            new GetOfflineQuotaQuery(
                TestUserId,
                new CloudDriveAccountScopedInput { CloudAccountUid = TestCloudAccountUid }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.Count);
        Assert.Equal(5, result.Value.Total);
        Assert.Equal(3, result.Value.Completed);
        Assert.Equal(["get_quota_package_info", "task_count"], gateway.OfflineRequests.Select(request => request.Endpoint));
    }

    [Fact]
    public async Task AddOfflineTaskCommandHandler_WhenSignatureUnavailable_ReturnsBusinessFailure()
    {
        // Arrange
        var gateway = new ThrowingCloudDriveOpenApiGateway(new CloudDriveConfigurationException(
            "signature unavailable",
            "offline_signature_not_available"));
        var handler = new AddOfflineTaskCommandHandler(gateway, CreateEnabledConfig());

        // Act
        Result<CloudDriveOperationResultDto> result = await handler.Handle(
            new AddOfflineTaskCommand(
                TestUserId,
                new AddOfflineTaskInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    Urls = ["https://example.test/file.iso"],
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("offline_signature_not_available", result.Error.Code);
    }

    [Fact]
    public async Task CancelOfflineTaskCommandHandler_WhenSuccessful_PostsDelTask()
    {
        // Arrange
        var gateway = new RecordingCloudDriveOpenApiGateway();
        var handler = new CancelOfflineTaskCommandHandler(gateway, CreateEnabledConfig());

        // Act
        Result<CloudDriveOperationResultDto> result = await handler.Handle(
            new CancelOfflineTaskCommand(
                TestUserId,
                new CancelOfflineTaskInput
                {
                    CloudAccountUid = TestCloudAccountUid,
                    InfoHash = "hash-1",
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        CloudDriveGatewayRequest request = Assert.Single(gateway.OfflineRequests);
        Assert.Equal("del_task", request.Endpoint);
        Assert.Equal("hash-1", request.Context.FormFields!["hash[0]"]);
    }

    private static IOptions<CloudDriveAppConfig> CreateEnabledConfig()
    {
        return Options.Create(new CloudDriveAppConfig
        {
            CloudDriveFeatureEnabled = true,
        });
    }
}
