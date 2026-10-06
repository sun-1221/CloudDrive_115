using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WebApi.Extensions;
using WebApi.Models;

namespace WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/files")]
public sealed class FilesController(ISender sender) : ControllerBase
{
    /// <summary>
    /// 获取指定目录下的文件列表。
    /// </summary>
    /// <param name="cid">115 目录编号。</param>
    /// <param name="offset">分页偏移量。</param>
    /// <param name="limit">分页返回数量上限。</param>
    /// <param name="type">业务类型。</param>
    /// <param name="suffix">文件扩展名筛选条件。</param>
    /// <param name="asc">是否按升序排序。</param>
    /// <param name="orderBy">排序字段表达式。</param>
    /// <param name="star">星标状态筛选条件。</param>
    /// <param name="showDir">是否返回目录信息。</param>
    /// <param name="natSort">参数 <paramref name="natSort"/>。</param>
    /// <param name="snap">是否返回视频截图信息。</param>
    /// <param name="countFolders">是否统计文件夹数量。</param>
    /// <param name="recordOpenTime">是否记录文件打开时间。</param>
    /// <param name="code">错误码。</param>
    /// <param name="scid">115 搜索目录编号。</param>
    /// <param name="source">请求来源标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpGet]
    public async Task<IResult> GetListAsync(
        [FromQuery(Name = "cid")] string? cid,
        [FromQuery] int offset,
        [FromQuery] int limit = 50,
        [FromQuery] int? type = null,
        [FromQuery] string? suffix = null,
        [FromQuery] int? asc = null,
        [FromQuery(Name = "o")] string? orderBy = null,
        [FromQuery] int? star = null,
        [FromQuery(Name = "showDir")] int? showDir = null,
        [FromQuery(Name = "natsort")] int? natSort = null,
        [FromQuery] int? snap = null,
        [FromQuery(Name = "count_folders")] int? countFolders = null,
        [FromQuery(Name = "record_open_time")] int? recordOpenTime = null,
        [FromQuery] string? code = null,
        [FromQuery] string? scid = null,
        [FromQuery] string? source = null,
        CancellationToken cancellationToken = default)
    {
        var input = new GetFileListInput
        {
            DirectoryId = cid,
            Offset = offset,
            Limit = limit,
            Type = type,
            Suffix = suffix,
            Asc = asc,
            OrderBy = orderBy,
            Star = star,
            ShowDir = showDir,
            NatSort = natSort,
            Snap = snap,
            CountFolders = countFolders,
            RecordOpenTime = recordOpenTime,
            Code = code,
            Scid = scid,
            Source = source,
        };

        var result = await sender.Send(
            new GetFileListQuery(this.GetAuthenticatedUserId(), input),
            cancellationToken);

        return result.ToHttpResult();
    }

    /// <summary>
    /// 获取指定文件的详细信息。
    /// </summary>
    /// <param name="fileId">115 文件唯一标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpGet("{fileId}")]
    public async Task<IResult> GetDetailAsync(string fileId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetFileDetailQuery(
                this.GetAuthenticatedUserId(),
                new GetFileInput { FileId = fileId }),
            cancellationToken);

        return result.ToHttpResult();
    }

    /// <summary>
    /// 搜索 115 云盘文件。
    /// </summary>
    /// <param name="searchValue">搜索关键字。</param>
    /// <param name="offset">分页偏移量。</param>
    /// <param name="limit">分页返回数量上限。</param>
    /// <param name="cid">115 目录编号。</param>
    /// <param name="date">日期筛选条件。</param>
    /// <param name="type">业务类型。</param>
    /// <param name="pickCode">115 文件提取码。</param>
    /// <param name="countFolders">是否统计文件夹数量。</param>
    /// <param name="source">请求来源标识。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpGet("search")]
    public async Task<IResult> SearchAsync(
        [FromQuery] string searchValue,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 50,
        [FromQuery(Name = "cid")] string? cid = null,
        [FromQuery(Name = "date")] string? date = null,
        [FromQuery] int? type = null,
        [FromQuery] string? pickCode = null,
        [FromQuery] int? countFolders = null,
        [FromQuery] string? source = null,
        CancellationToken cancellationToken = default)
    {
        var input = new SearchFileInput
        {
            Keyword = searchValue,
            Offset = offset,
            Limit = limit,
            DirectoryId = cid,
            Date = date,
            Type = type,
            PickCode = pickCode,
            CountFolders = countFolders,
            Source = source,
        };

        var result = await sender.Send(
            new GetSearchFilesQuery(this.GetAuthenticatedUserId(), input),
            cancellationToken);

        return result.ToHttpResult();
    }

    /// <summary>
    /// 获取指定文件的下载链接。
    /// </summary>
    /// <param name="pickCode">115 文件提取码。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpGet("{pickCode}/download")]
    public async Task<IResult> GetDownloadAsync(string pickCode, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetDownloadUrlQuery(
                this.GetAuthenticatedUserId(),
                new GetDownloadUrlInput { PickCode = pickCode }),
            cancellationToken);

        return result.ToHttpResult();
    }

    /// <summary>
    /// 创建文件夹。
    /// </summary>
    /// <param name="request">HTTP 请求载荷。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpPost("folders")]
    public async Task<IResult> CreateFolderAsync(
        [FromBody] CreateFolderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateFolderCommand(
                this.GetAuthenticatedUserId(),
                new CreateFolderInput
                {
                    ParentId = request.ParentId,
                    FolderName = request.FolderName,
                }),
            cancellationToken);

        return result.ToCreatedHttpResult();
    }

    /// <summary>
    /// 上传文件到指定目录。
    /// </summary>
    /// <param name="file">上传文件。</param>
    /// <param name="directoryId">目标目录编号。</param>
    /// <param name="forceAsync">是否强制使用异步后台任务。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IResult> UploadAsync(
        IFormFile file,
        [FromForm] string directoryId,
        [FromForm] bool forceAsync,
        CancellationToken cancellationToken)
    {
        await using Stream fileStream = file.OpenReadStream();
        var input = new UploadFileInput
        {
            DirectoryId = directoryId,
            FileName = file.FileName,
            FileSize = file.Length,
            ForceAsync = forceAsync,
        };

        var result = await sender.Send(
            new UploadFileCommand(this.GetAuthenticatedUserId(), input, fileStream),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.Accepted);
    }

    /// <summary>
    /// 移动文件或文件夹。
    /// </summary>
    /// <param name="request">HTTP 请求载荷。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpPost("move")]
    public async Task<IResult> MoveAsync(
        [FromBody] MoveFilesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new MoveFilesCommand(
                this.GetAuthenticatedUserId(),
                new MoveFilesInput
                {
                    FileIds = request.FileIds,
                    TargetDirectoryId = request.TargetDirectoryId,
                }),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.Accepted);
    }

    /// <summary>
    /// 复制文件或文件夹。
    /// </summary>
    /// <param name="request">HTTP 请求载荷。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpPost("copy")]
    public async Task<IResult> CopyAsync(
        [FromBody] CopyFileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CopyFilesCommand(
                this.GetAuthenticatedUserId(),
                new CopyFilesInput
                {
                    FileIds = [request.FileId],
                    TargetDirectoryId = request.TargetDirectoryId,
                }),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.Accepted);
    }

    /// <summary>
    /// 删除文件或文件夹（移至回收站）。
    /// </summary>
    /// <param name="request">HTTP 请求载荷。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpDelete]
    public async Task<IResult> DeleteAsync(
        [FromBody] FileIdsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new DeleteFilesCommand(
                this.GetAuthenticatedUserId(),
                new DeleteFilesInput { FileIds = request.FileIds }),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.Accepted);
    }

    /// <summary>
    /// 重命名文件或文件夹。
    /// </summary>
    /// <param name="fileId">115 文件唯一标识。</param>
    /// <param name="request">HTTP 请求载荷。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpPatch("{fileId}")]
    public async Task<IResult> RenameAsync(
        string fileId,
        [FromBody] RenameFileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RenameFileCommand(
                this.GetAuthenticatedUserId(),
                new RenameFileInput
                {
                    FileId = fileId,
                    NewFileName = request.NewFileName,
                }),
            cancellationToken);

        return result.ToHttpResult();
    }

    /// <summary>
    /// 切换文件收藏状态。
    /// </summary>
    /// <param name="fileId">115 文件唯一标识。</param>
    /// <param name="request">HTTP 请求载荷。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpPost("{fileId}/star")]
    public async Task<IResult> ToggleStarAsync(
        string fileId,
        [FromBody] ToggleStarRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ToggleStarCommand(
                this.GetAuthenticatedUserId(),
                new ToggleStarInput
                {
                    FileId = fileId,
                    Star = request.Starred,
                }),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.Accepted);
    }
}
