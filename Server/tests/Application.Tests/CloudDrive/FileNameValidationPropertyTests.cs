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

    [Property]
    public bool ValidateNewFolderName_NameWithinLimit_DoesNotThrow(PositiveInt length)
    {
        int len = Math.Min(length.Get, 255);
        string name = new('a', len);
        Exception? ex = Record.Exception(() => CloudDriveFileNameValidator.ValidateNewFolderName(name));
        return ex is null;
    }

    [Property]
    public bool ValidateNewFolderName_NameExceedingLimit_Throws(PositiveInt extra)
    {
        int len = 256 + extra.Get % 1000;
        string name = new('a', len);
        return Assert.Throws<CloudDriveInvalidNameException>(
            () => CloudDriveFileNameValidator.ValidateNewFolderName(name)) is not null;
    }

    [Property]
    public bool ValidateNewFolderName_ContainingIllegalChar_AlwaysThrows(NonEmptyString prefix, NonEmptyString suffix)
    {
        foreach (char illegal in IllegalChars)
        {
            string name = prefix.Get[..Math.Min(prefix.Get.Length, 50)] + illegal + suffix.Get[..Math.Min(suffix.Get.Length, 50)];
            try
            {
                CloudDriveFileNameValidator.ValidateNewFolderName(name);
                return false; // 应该抛出异常
            }
            catch (CloudDriveInvalidNameException)
            {
                // 预期行为
            }
        }
        return true;
    }

    [Property]
    public bool ValidateRenameFileName_Utf8BytesBoundary_RespectsLimit(PositiveInt charCount)
    {
        // 中文字符每个占 3 字节
        int count = Math.Min(charCount.Get, 85); // 85 * 3 = 255 bytes
        string name = new('中', count);
        int byteLen = System.Text.Encoding.UTF8.GetByteCount(name);

        Exception? ex = Record.Exception(() => CloudDriveFileNameValidator.ValidateRenameFileName(name));

        if (byteLen <= 255)
            return ex is null;
        else
            return ex is CloudDriveInvalidNameException;
    }
}
