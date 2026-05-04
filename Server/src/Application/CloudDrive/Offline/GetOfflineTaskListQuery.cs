using Application.CloudDrive.Dtos;
using Application.Queries;

namespace Application.CloudDrive.Offline;

public sealed record GetOfflineTaskListQuery(Guid UserId, GetOfflineTaskListInput Input)
    : IQuery<PagedResultDto<CloudDriveOfflineTaskDto>>;
