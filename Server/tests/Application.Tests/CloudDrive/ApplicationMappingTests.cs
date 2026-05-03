using Application.CloudDrive.Dtos;
using Application.CloudDrive.Mappings;

using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Tests.CloudDrive;

public sealed class ApplicationMappingTests
{
    [Fact]
    public void UpsertCookieSessionInput_ToEntity_MapsCookieTriplet()
    {
        var input = new UpsertCookieSessionInput
        {
            Uid = "uid",
            Cid = "cid",
            Ussid = "ussid",
        };

        CookieTriplet entity = input.ToEntity();

        Assert.Equal("uid", entity.Uid);
        Assert.Equal("cid", entity.Cid);
        Assert.Equal("ussid", entity.Ussid);
    }

    [Fact]
    public void CloudDriveJob_ToDto_MapsStatusAndProgress()
    {
        CloudDriveJob job = CloudDriveJob.Create(
            Guid.NewGuid(),
            "cloud-account",
            CloudDriveJobType.BatchCopy,
            "resource-1",
            "{}");
        job.Start();
        job.UpdateProgress(37);

        CloudDriveJobDto dto = job.ToDto();

        Assert.Equal(job.Id, dto.Id);
        Assert.Equal(CloudDriveJobType.BatchCopy, dto.JobType);
        Assert.Equal(CloudDriveJobStatus.Running, dto.Status);
        Assert.Equal(37, dto.Progress);
        Assert.Equal("resource-1", dto.ResourceId);
    }
}
