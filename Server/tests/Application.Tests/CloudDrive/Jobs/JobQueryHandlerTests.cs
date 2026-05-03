using Application.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Jobs;
using Application.Queries;
using Application.Tests.Support;

using Domain.Entities;
using Domain.Enums;
using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.Tests.CloudDrive.Jobs;

public sealed class JobQueryHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    private static readonly Guid OtherUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    [Fact]
    public async Task GetJobStatusQueryHandler_WhenJobBelongsToUserAndAccount_ReturnsJobDto()
    {
        // Arrange
        CloudDriveJob job = CloudDriveJob.Create(
            TestUserId,
            "115001",
            CloudDriveJobType.Upload,
            resourceId: "dir-001",
            payloadJson: "{}");
        var repository = new FakeCloudDriveJobRepository([job]);
        IQueryHandler<GetJobStatusQuery, CloudDriveJobDto> handler =
            new GetJobStatusQueryHandler(repository, CreateEnabledConfig());

        // Act
        Result<CloudDriveJobDto> result = await handler.Handle(
            new GetJobStatusQuery(
                TestUserId,
                new GetJobStatusInput
                {
                    CloudAccountUid = "115001",
                    JobId = job.Id
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(job.Id, result.Value.Id);
        Assert.Equal(CloudDriveJobType.Upload, result.Value.JobType);
        Assert.Equal(CloudDriveJobStatus.Pending, result.Value.Status);
        Assert.Equal("dir-001", result.Value.ResourceId);
    }

    [Fact]
    public async Task GetJobStatusQueryHandler_WhenAccountDoesNotMatch_ReturnsJobNotFound()
    {
        // Arrange
        CloudDriveJob job = CloudDriveJob.Create(TestUserId, "115001", CloudDriveJobType.Upload, payloadJson: "{}");
        var repository = new FakeCloudDriveJobRepository([job]);
        IQueryHandler<GetJobStatusQuery, CloudDriveJobDto> handler =
            new GetJobStatusQueryHandler(repository, CreateEnabledConfig());

        // Act
        Result<CloudDriveJobDto> result = await handler.Handle(
            new GetJobStatusQuery(
                TestUserId,
                new GetJobStatusInput
                {
                    CloudAccountUid = "115002",
                    JobId = job.Id
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("cloud_drive_job_not_found", result.Error.Code);
    }

    [Fact]
    public async Task GetJobListQueryHandler_WhenCloudAccountMissing_ReturnsAllCurrentUserAccounts()
    {
        // Arrange
        CloudDriveJob first = CloudDriveJob.Create(TestUserId, "115001", CloudDriveJobType.Upload, payloadJson: "{}");
        CloudDriveJob second = CloudDriveJob.Create(TestUserId, "115002", CloudDriveJobType.Transcode, payloadJson: "{}");
        CloudDriveJob otherUser = CloudDriveJob.Create(OtherUserId, "115001", CloudDriveJobType.Upload, payloadJson: "{}");
        var repository = new FakeCloudDriveJobRepository([first, second, otherUser]);
        IQueryHandler<GetJobListQuery, PagedResultDto<CloudDriveJobDto>> handler =
            new GetJobListQueryHandler(repository, CreateEnabledConfig());

        // Act
        Result<PagedResultDto<CloudDriveJobDto>> result = await handler.Handle(
            new GetJobListQuery(
                TestUserId,
                new GetJobListInput { MaxResultCount = 20 }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.DoesNotContain(result.Value.Items, item => item.Id == otherUser.Id);
    }

    [Fact]
    public async Task GetJobListQueryHandler_WhenAccountAndStatusProvided_FiltersAndPages()
    {
        // Arrange
        CloudDriveJob running = CloudDriveJob.Create(TestUserId, "115001", CloudDriveJobType.Upload, payloadJson: "{}");
        running.Start();
        CloudDriveJob pending = CloudDriveJob.Create(TestUserId, "115001", CloudDriveJobType.BatchCopy, payloadJson: "{}");
        CloudDriveJob otherAccount = CloudDriveJob.Create(TestUserId, "115002", CloudDriveJobType.Upload, payloadJson: "{}");
        var repository = new FakeCloudDriveJobRepository([running, pending, otherAccount]);
        IQueryHandler<GetJobListQuery, PagedResultDto<CloudDriveJobDto>> handler =
            new GetJobListQueryHandler(repository, CreateEnabledConfig());

        // Act
        Result<PagedResultDto<CloudDriveJobDto>> result = await handler.Handle(
            new GetJobListQuery(
                TestUserId,
                new GetJobListInput
                {
                    CloudAccountUid = "115001",
                    Status = CloudDriveJobStatus.Pending,
                    SkipCount = 0,
                    MaxResultCount = 1
                }),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        CloudDriveJobDto item = Assert.Single(result.Value.Items);
        Assert.Equal(pending.Id, item.Id);
        Assert.Equal(CloudDriveJobStatus.Pending, item.Status);
    }

    private static IOptions<CloudDriveAppConfig> CreateEnabledConfig()
    {
        return Options.Create(new CloudDriveAppConfig
        {
            CloudDriveFeatureEnabled = true,
        });
    }
}
