using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Auth;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Files;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Video;

internal sealed class GetSubtitlesQueryHandler(
    CloudDriveFileManager fileManager,
    ICloudDriveWebApiGateway openApiGateway,
    ICloudDriveAccountContextAccessor accountContextAccessor,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetSubtitlesQuery, CloudDriveSubtitleListDto>
{
    public async Task<Result<CloudDriveSubtitleListDto>> Handle(
        GetSubtitlesQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        string cloudAccountUid = accountContextAccessor.CloudAccountUid;

        Result<string> pickCodeResult = await ResolvePickCodeAsync(
            request.UserId,
            cloudAccountUid,
            request.Input.FileId,
            request.Input.PickCode,
            cancellationToken);

        if (pickCodeResult.IsFailure)
        {
            return Result.Failure<CloudDriveSubtitleListDto>(pickCodeResult.Error);
        }

        JsonElement response = await openApiGateway.SendWebApiRequestAsync<JsonElement>(
            request.UserId,
            cloudAccountUid,
            HttpMethod.Get,
            "movies/subtitle",
            CloudDriveVideoSupport.CreateSubtitleContext(pickCodeResult.Value),
            cancellationToken);

        return Result.Success(CloudDriveVideoSupport.MapSubtitles(response));
    }

    private async Task<Result<string>> ResolvePickCodeAsync(
        Guid userId,
        string cloudAccountUid,
        string? fileId,
        string? pickCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return string.IsNullOrWhiteSpace(pickCode)
                ? Result.Failure<string>(CloudDriveVideoSupport.PickCodeRequired)
                : Result.Success(pickCode);
        }

        CloudDriveFileDetailDto? file = await fileManager.GetFileDetailAsync<CloudDriveFileDetailDto?>(
            userId,
            accountContextAccessor.CloudAccountUid,
            fileId,
            CloudDriveFileHandlerSupport.CreateFileDetailContext(new GetFileInput
            {
                FileId = fileId,
            }),
            cancellationToken);

        if (file is null)
        {
            return Result.Failure<string>(CloudDriveFileHandlerSupport.FileNotFound(fileId));
        }

        return string.IsNullOrWhiteSpace(file.PickCode)
            ? Result.Failure<string>(CloudDriveVideoSupport.PickCodeRequired)
            : Result.Success(file.PickCode);
    }
}
