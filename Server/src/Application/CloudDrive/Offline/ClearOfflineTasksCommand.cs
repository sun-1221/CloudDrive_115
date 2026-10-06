using Application.Abstractions;
using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Offline;

public sealed record ClearOfflineTasksCommand(Guid UserId, ClearOfflineTasksInput Input)
    : ICommand<CloudDriveOperationResultDto>, IHasInput<ClearOfflineTasksInput>;
