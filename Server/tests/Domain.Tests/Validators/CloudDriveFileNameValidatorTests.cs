using Domain.Exceptions;
using Domain.Validators;

namespace Domain.Tests.Validators;

/// <summary>
/// 115 网盘文件名验证器测试。
/// </summary>
public sealed class CloudDriveFileNameValidatorTests
{
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

    [Fact]
    public void ValidateNewFolderName_With256Characters_ThrowsCloudDriveInvalidNameException()
    {
        // Arrange
        string folderName = new('a', 256);

        // Act & Assert
        Assert.Throws<CloudDriveInvalidNameException>(() => CloudDriveFileNameValidator.ValidateNewFolderName(folderName));
    }

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

    [Fact]
    public void ValidateRenameFileName_WithNameOver255Utf8Bytes_ThrowsCloudDriveInvalidNameException()
    {
        // Arrange
        string fileName = new('中', 86);

        // Act & Assert
        Assert.Throws<CloudDriveInvalidNameException>(() => CloudDriveFileNameValidator.ValidateRenameFileName(fileName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateRenameFileName_WithEmptyName_ThrowsCloudDriveInvalidNameException(string? fileName)
    {
        // Arrange

        // Act & Assert
        Assert.Throws<CloudDriveInvalidNameException>(() => CloudDriveFileNameValidator.ValidateRenameFileName(fileName));
    }

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
