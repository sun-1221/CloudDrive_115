using System.Text;

using Domain.Exceptions;

namespace Domain.Validators;

/// <summary>
/// 115 网盘文件名与文件夹名领域验证器。
/// </summary>
public static class CloudDriveFileNameValidator
{
    private const int MaxNewFolderNameCharacters = 255;
    private const int MaxRenameFileNameBytes = 255;
    private static readonly char[] InvalidFileNameCharacters = ['/', '\\', ':', '*', '?', '"', '<', '>', '|'];

    /// <summary>
    /// 验证新建文件夹名称。
    /// </summary>
    /// <param name="folderName">文件夹名称。</param>
    /// <exception cref="CloudDriveInvalidNameException">名称为空、包含非法字符或超过 255 字符时抛出。</exception>
    public static void ValidateNewFolderName(string? folderName)
    {
        string validatedFolderName = ValidateCommon(folderName);

        if (validatedFolderName.Length > MaxNewFolderNameCharacters)
        {
            throw new CloudDriveInvalidNameException("文件夹名称不能超过 255 个字符");
        }
    }

    /// <summary>
    /// 验证重命名后的文件名或文件夹名。
    /// </summary>
    /// <param name="fileName">文件名或文件夹名。</param>
    /// <exception cref="CloudDriveInvalidNameException">名称为空、包含非法字符或 UTF-8 编码后超过 255 字节时抛出。</exception>
    public static void ValidateRenameFileName(string? fileName)
    {
        string validatedFileName = ValidateCommon(fileName);

        int byteCount = Encoding.UTF8.GetByteCount(validatedFileName);
        if (byteCount > MaxRenameFileNameBytes)
        {
            throw new CloudDriveInvalidNameException("文件名 UTF-8 编码后不能超过 255 字节");
        }
    }

    private static string ValidateCommon(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new CloudDriveInvalidNameException("文件名不能为空");
        }

        if (name.IndexOfAny(InvalidFileNameCharacters) >= 0)
        {
            throw new CloudDriveInvalidNameException("文件名不能包含非法字符 / \\ : * ? \" < > |");
        }

        return name;
    }
}
