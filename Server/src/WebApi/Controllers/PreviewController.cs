using Application.CloudDrive.Dtos;
using Application.CloudDrive.Preview;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WebApi.Extensions;

namespace WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/files")]
public sealed class PreviewController(ISender sender) : ControllerBase
{
    [HttpGet("{pickCode}/preview")]
    public async Task<IResult> GetImagePreviewAsync(string pickCode, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetImagePreviewQuery(
                this.GetAuthenticatedUserId(),
                new GetPreviewInput { FileId = pickCode }),
            cancellationToken);

        return result.ToHttpResult();
    }
}
