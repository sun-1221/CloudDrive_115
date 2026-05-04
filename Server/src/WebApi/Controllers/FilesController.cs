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
        };

        var result = await sender.Send(
            new GetFileListQuery(this.GetAuthenticatedUserId(), input),
            cancellationToken);

        return result.ToHttpResult();
    }

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
            new SearchFilesQuery(this.GetAuthenticatedUserId(), input),
            cancellationToken);

        return result.ToHttpResult();
    }

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

    [HttpPost("copy")]
    public async Task<IResult> CopyAsync(
        [FromBody] MoveFilesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CopyFilesCommand(
                this.GetAuthenticatedUserId(),
                new CopyFilesInput
                {
                    FileIds = request.FileIds,
                    TargetDirectoryId = request.TargetDirectoryId,
                }),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.Accepted);
    }

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
