using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.CloudDrive.Mappings;
using Application.Commands;

using Domain.Repositories;
using Domain.Results;
using Domain.ValueObjects;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Auth;

internal sealed class UpsertCookieSessionCommandHandler(
    CookieSessionManager cookieSessionManager,
    ICookieSessionRepository cookieSessionRepository,
    ICloudDriveMetadataCache metadataCache,
    ICloudDriveRateLimiter rateLimiter,
    IOptions<CloudDriveAppConfig> configOptions)
    : ICommandHandler<UpsertCookieSessionCommand, CloudDriveCookieSessionDto>
{
    public async Task<Result<CloudDriveCookieSessionDto>> Handle(
        UpsertCookieSessionCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        CookieTriplet cookieTriplet = request.Input.ToEntity();
        await cookieSessionManager.UpsertSessionAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            cookieTriplet,
            request.Input.ExtraFields,
            cancellationToken);

        await metadataCache.InvalidateDirectoryAsync(request.UserId, request.Input.CloudAccountUid, directoryId: "0", cancellationToken: cancellationToken);

        rateLimiter.Reset(request.UserId, request.Input.CloudAccountUid);

        Domain.Entities.CookieSession session = await cookieSessionRepository.GetByAccountAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            cancellationToken)
            ?? throw new InvalidOperationException("Cookie session was not persisted.");

        return Result.Success(session.ToDto());
    }
}
