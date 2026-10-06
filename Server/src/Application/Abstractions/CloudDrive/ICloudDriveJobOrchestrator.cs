using Application.CloudDrive.Dtos;

using Domain.Enums;

namespace Application.Abstractions.CloudDrive;

/// <summary>
/// 115 网盘后台任务编排器。统一创建任务记录并委托调度器启动后台执行。
/// </summary>
public interface ICloudDriveJobOrchestrator
{
    /// <summary>
    /// 创建大文件异步上传任务。
    /// </summary>
    /// <returns>返回已创建的后台上传任务 ID。</returns>
    Task<Guid> EnqueueLargeUploadAsync(
        Guid userId,
        string cloudAccountUid,
        UploadFileInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建批量文件或回收站操作任务。
    /// </summary>
    /// <returns>返回已创建的批量后台任务 ID。</returns>
    Task<Guid> EnqueueBatchOperationAsync(
        Guid userId,
        string cloudAccountUid,
        CloudDriveJobType jobType,
        object payload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建清空回收站任务。
    /// </summary>
    /// <returns>返回已创建的清空回收站后台任务 ID。</returns>
    Task<Guid> EnqueueClearRecycleBinAsync(
        Guid userId,
        string cloudAccountUid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建视频转码任务。
    /// </summary>
    /// <returns>返回已创建的视频转码后台任务 ID。</returns>
    Task<Guid> EnqueueVideoTranscodeAsync(
        Guid userId,
        string cloudAccountUid,
        SubmitTranscodeInput input,
        CancellationToken cancellationToken = default);
}
