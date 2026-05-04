using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Offline;

public sealed record AddOfflineTaskCommand(Guid UserId, AddOfflineTaskInput Input)
    : ICommand<CloudDriveOperationResultDto>;
