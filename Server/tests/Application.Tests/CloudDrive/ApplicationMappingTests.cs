using Application.CloudDrive.Dtos;
using Application.CloudDrive.Mappings;

using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Tests.CloudDrive;

/// <summary>
/// Application 层 115 网盘映射扩展测试。
/// </summary>
public sealed class ApplicationMappingTests
{
    /// <summary>
    /// 验证 Cookie 会话输入能够映射为 Cookie 三元组值对象。
    /// </summary>
    [Fact]
    public void UpsertCookieSessionInput_ToEntity_MapsCookieTriplet()
    {
        // Arrange（准备）
        var input = new UpsertCookieSessionInput
        {
            Uid = "uid",
            Cid = "cid",
            Ussid = "ussid",
        };

        // Act（执行）
        CookieTriplet entity = input.ToEntity();

        // Assert（断言）
        Assert.Equal("uid", entity.Uid);
        Assert.Equal("cid", entity.Cid);
        Assert.Equal("ussid", entity.Ussid);
    }

    /// <summary>
    /// 验证云盘任务实体能够映射任务状态与进度到 DTO。
    /// </summary>
    [Fact]
    public void CloudDriveJob_ToDto_MapsStatusAndProgress()
    {
        // Arrange（准备）
        CloudDriveJob job = CloudDriveJob.Create(
            Guid.NewGuid(),
            "cloud-account",
            CloudDriveJobType.BatchCopy,
            "resource-1",
            "{}");
        job.Start();
        job.UpdateProgress(37);

        // Act（执行）
        CloudDriveJobDto dto = job.ToDto();

        // Assert（断言）
        Assert.Equal(job.Id, dto.Id);
        Assert.Equal(CloudDriveJobType.BatchCopy, dto.JobType);
        Assert.Equal(CloudDriveJobStatus.Running, dto.Status);
        Assert.Equal(37, dto.Progress);
        Assert.Equal("resource-1", dto.ResourceId);
    }
}
