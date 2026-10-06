using Domain.Exceptions;
using Domain.Validators;

using FsCheck;
using FsCheck.Xunit;

namespace Application.Tests.CloudDrive;

/// <summary>
/// 属性 P5：文件名验证。
/// 使用 FsCheck 随机生成文件名验证边界。
/// </summary>
public sealed class FileNameValidationPropertyTests
{
    private static readonly char[] IllegalChars = ['/', '\\', ':', '*', '?', '"', '<', '>', '|'];

    /// <summary>
    /// 验证长度不超过限制的新建文件夹名称不会抛出异常。
    /// </summary>
    [Property]
    public bool ValidateNewFolderName_NameWithinLimit_DoesNotThrow(PositiveInt length)
    {
        // Arrange（准备）
        int len = Math.Min(length.Get, 255);
        string name = new('a', len);

        // Act（执行）
        Exception? ex = Record.Exception(() => CloudDriveFileNameValidator.ValidateNewFolderName(name));

        // Assert（断言）
        return ex is null;
    }

    /// <summary>
    /// 验证长度超过限制的新建文件夹名称会抛出异常。
    /// </summary>
    [Property]
    public bool ValidateNewFolderName_NameExceedingLimit_Throws(PositiveInt extra)
    {
        // Arrange（准备）
        int len = 256 + (extra.Get % 1000);
        string name = new('a', len);

        // Act（执行）
        CloudDriveInvalidNameException exception = Assert.Throws<CloudDriveInvalidNameException>(
            () => CloudDriveFileNameValidator.ValidateNewFolderName(name));

        // Assert（断言）
        return exception is not null;
    }

    /// <summary>
    /// 验证包含非法字符的新建文件夹名称始终抛出异常。
    /// </summary>
    [Property]
    public bool ValidateNewFolderName_ContainingIllegalChar_AlwaysThrows(NonEmptyString prefix, NonEmptyString suffix)
    {
        // Arrange（准备）
        foreach (char illegal in IllegalChars)
        {
            string name = prefix.Get[..Math.Min(prefix.Get.Length, 50)] + illegal + suffix.Get[..Math.Min(suffix.Get.Length, 50)];
            try
            {
                // Act（执行）
                CloudDriveFileNameValidator.ValidateNewFolderName(name);

                // Assert（断言）
                return false; // 应该抛出异常
            }
            catch (CloudDriveInvalidNameException)
            {
                // 预期行为
            }
        }

        return true;
    }

    /// <summary>
    /// 验证重命名文件名遵守 UTF-8 字节数边界。
    /// </summary>
    [Property]
    public bool ValidateRenameFileName_Utf8BytesBoundary_RespectsLimit(PositiveInt charCount)
    {
        // Arrange（准备）
        // 中文字符每个占 3 字节
        int count = Math.Min(charCount.Get, 85); // 85 * 3 = 255 bytes
        string name = new('中', count);
        int byteLen = System.Text.Encoding.UTF8.GetByteCount(name);

        // Act（执行）
        Exception? ex = Record.Exception(() => CloudDriveFileNameValidator.ValidateRenameFileName(name));

        // Assert（断言）
        if (byteLen <= 255)
        {
            return ex is null;
        }
        else
        {
            return ex is CloudDriveInvalidNameException;
        }
    }
}
