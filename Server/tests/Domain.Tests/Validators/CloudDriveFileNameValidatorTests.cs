using Domain.Exceptions;
using Domain.Validators;

namespace Domain.Tests.Validators;

/// <summary>
/// 115 网盘文件名验证器测试。
/// </summary>
public sealed class CloudDriveFileNameValidatorTests
{
    /// <summary>
    /// 验证 255 个字符的新建文件夹名称不会抛出异常。
    /// </summary>
    [Theory]
    [InlineData(255)]
    public void ValidateNewFolderName_With255Characters_DoesNotThrow(int length)
    {
        // Arrange
        string folderName = new('a', length);

        // Act
        Exception? exception = Record.Exception(() => CloudDriveFileNameValidator.ValidateNewFolderName(folderName));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// 验证 256 个字符的新建文件夹名称会抛出无效名称异常。
    /// </summary>
    [Fact]
    public void ValidateNewFolderName_With256Characters_ThrowsCloudDriveInvalidNameException()
    {
        // Arrange
        string folderName = new('a', 256);

        // Act & Assert
        Assert.Throws<CloudDriveInvalidNameException>(() => CloudDriveFileNameValidator.ValidateNewFolderName(folderName));
    }

    /// <summary>
    /// 验证 UTF-8 字节数不超过 255 的重命名文件名不会抛出异常。
    /// </summary>
    [Theory]
    [InlineData(255)]
    [InlineData(252)]
    public void ValidateRenameFileName_WithNameWithin255Utf8Bytes_DoesNotThrow(int byteLength)
    {
        // Arrange
        string fileName = new('中', byteLength / 3);

        // Act
        Exception? exception = Record.Exception(() => CloudDriveFileNameValidator.ValidateRenameFileName(fileName));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// 验证 UTF-8 字节数超过 255 的重命名文件名会抛出无效名称异常。
    /// </summary>
    [Fact]
    public void ValidateRenameFileName_WithNameOver255Utf8Bytes_ThrowsCloudDriveInvalidNameException()
    {
        // Arrange（准备）
        string fileName = new('中', 86);

        // Act（执行）
        Assert.Throws<CloudDriveInvalidNameException>(() => CloudDriveFileNameValidator.ValidateRenameFileName(fileName));

        // Assert（断言）
    }

    /// <summary>
    /// 验证空白重命名文件名会抛出无效名称异常。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRenameFileName_WithEmptyName_ThrowsCloudDriveInvalidNameException(string? fileName)
    {
        // Arrange（准备）

        // Act（执行）
        Assert.Throws<CloudDriveInvalidNameException>(() => CloudDriveFileNameValidator.ValidateRenameFileName(fileName));

        // Assert（断言）
    }

    /// <summary>
    /// 验证包含非法字符的新建文件夹名称会抛出无效名称异常。
    /// </summary>
    [Theory]
    [InlineData("bad/name")]
    [InlineData("bad\\name")]
    [InlineData("bad:name")]
    [InlineData("bad*name")]
    [InlineData("bad?name")]
    [InlineData("bad\"name")]
    [InlineData("bad<name")]
    [InlineData("bad>name")]
    [InlineData("bad|name")]
    public void ValidateNewFolderName_WithInvalidCharacter_ThrowsCloudDriveInvalidNameException(string folderName)
    {
        // Arrange

        // Act & Assert
        Assert.Throws<CloudDriveInvalidNameException>(() => CloudDriveFileNameValidator.ValidateNewFolderName(folderName));
    }
}
