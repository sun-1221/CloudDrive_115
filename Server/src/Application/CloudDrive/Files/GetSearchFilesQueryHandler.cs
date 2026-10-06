using Application.CloudDrive.Dtos;
using Application.Queries;

using Domain.Results;

namespace Application.CloudDrive.Files;

internal sealed class GetSearchFilesQueryHandler
    : IQueryHandler<GetSearchFilesQuery, CloudDriveSearchListDto>
{
    public Task<Result<CloudDriveSearchListDto>> Handle(
        GetSearchFilesQuery request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure<CloudDriveSearchListDto>(
            CloudDriveFileHandlerSupport.ApiNotCollected("文件搜索")));
    }
}
