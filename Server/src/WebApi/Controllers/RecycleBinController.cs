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
[Route("api/recycle")]
public sealed class RecycleBinController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IResult> GetListAsync(
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetRecycleBinListQuery(
                this.GetAuthenticatedUserId(),
                new GetRecycleBinListInput { Offset = offset, Limit = limit }),
            cancellationToken);

        return result.ToHttpResult();
    }

    [HttpPost("restore")]
    public async Task<IResult> RestoreAsync(
        [FromBody] RecycleBinItemsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RestoreFromRecycleBinCommand(
                this.GetAuthenticatedUserId(),
                new RecycleBinOperationInput { RecycleBinItemIds = request.RecycleBinItemIds }),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.Accepted);
    }

    [HttpDelete("{id}")]
    public async Task<IResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new DeleteFromRecycleBinCommand(
                this.GetAuthenticatedUserId(),
                new RecycleBinOperationInput { RecycleBinItemIds = [id] }),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.Accepted);
    }

    [HttpDelete]
    public async Task<IResult> ClearAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ClearRecycleBinCommand(this.GetAuthenticatedUserId(), new ClearRecycleBinInput()),
            cancellationToken);

        return result.ToOperationHttpResult(dto => dto.Accepted);
    }
}
