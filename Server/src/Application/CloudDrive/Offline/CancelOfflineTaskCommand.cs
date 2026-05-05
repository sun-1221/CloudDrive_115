using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Commands;

namespace Application.CloudDrive.Offline;

public sealed record CancelOfflineTaskCommand(Guid UserId, CancelOfflineTaskInput Input)
    : ICommand<CloudDriveOperationResultDto>, ICloudDriveAccountScopedRequest;
