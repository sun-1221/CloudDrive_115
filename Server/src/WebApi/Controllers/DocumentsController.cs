using Application.CloudDrive.Dtos;
using Application.CloudDrive.Preview;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using WebApi.Extensions;

namespace WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// 获取文档在线预览地址。
    /// </summary>
    /// <param name="pickCode">115 文件提取码。</param>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>表示异步操作结果的 <see cref="Task{TResult}"/>。</returns>
    [HttpGet("{pickCode}/preview")]
    public async Task<IResult> GetDocumentPreviewAsync(string pickCode, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetDocPreviewQuery(
                this.GetAuthenticatedUserId(),
                new GetPreviewInput { PickCode = pickCode }),
            cancellationToken);

        return result.ToHttpResult();
    }
}
