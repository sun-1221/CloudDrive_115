using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Files;

/// <summary>
/// 执行或排队指定 115 账号的文件上传。
/// </summary>
/// <param name="UserId">系统用户 ID。</param>
/// <param name="Input">文件上传输入。</param>
/// <param name="FileStream">待上传文件流；异步排队场景可为空。</param>
public sealed record UploadFileCommand(Guid UserId, UploadFileInput Input, Stream? FileStream)
    : ICommand<CloudDriveUploadExecutionDto>;
