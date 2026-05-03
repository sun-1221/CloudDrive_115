using System.Reflection;

using Application.CloudDrive.Dtos;

namespace Application.Tests.CloudDrive;

/// <summary>
/// P9：所有返回临时资源地址的 DTO 默认标记为临时资源。
/// </summary>
public sealed class TemporaryResourceMarkerPropertyTests
{
    /// <summary>
    /// 对任意 Application 层 CloudDrive DTO，只要包含 <c>IsTemporary</c> 属性，默认构造后均应为 true。
    /// </summary>
    [Fact]
    public void CloudDriveDtos_WithIsTemporary_DefaultToTrue()
    {
        Type[] temporaryDtoTypes = typeof(CloudDriveDownloadDto).Assembly
            .GetTypes()
            .Where(type => type.Namespace == typeof(CloudDriveDownloadDto).Namespace)
            .Where(type => type is { IsAbstract: false, IsClass: true })
            .Where(type => type.GetProperty("IsTemporary", BindingFlags.Public | BindingFlags.Instance) is not null)
            .ToArray();

        Assert.Contains(typeof(CloudDriveDownloadDto), temporaryDtoTypes);
        Assert.Contains(typeof(CloudDriveImagePreviewDto), temporaryDtoTypes);
        Assert.Contains(typeof(CloudDriveDocPreviewDto), temporaryDtoTypes);
        Assert.Contains(typeof(CloudDriveVideoPlayDto), temporaryDtoTypes);

        foreach (Type dtoType in temporaryDtoTypes)
        {
            object dto = Activator.CreateInstance(dtoType)!;
            PropertyInfo isTemporaryProperty = dtoType.GetProperty("IsTemporary", BindingFlags.Public | BindingFlags.Instance)!;

            Assert.True((bool)isTemporaryProperty.GetValue(dto)!);
        }
    }
}
