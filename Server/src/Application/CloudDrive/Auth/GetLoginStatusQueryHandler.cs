using System.Text.Json;

using Application.Abstractions.CloudDrive;
using Application.CloudDrive.Dtos;
using Application.Queries;

using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Repositories;
using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Auth;

internal sealed class GetLoginStatusQueryHandler(
    ICookieSessionRepository cookieSessionRepository,
    ICloudDriveWebApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetLoginStatusQuery, CloudDriveAuthCheckDto>
{
    public async Task<Result<CloudDriveAuthCheckDto>> Handle(
        GetLoginStatusQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        DateTime checkedAt = DateTime.UtcNow;
        IReadOnlyList<CookieSession> activeSessions = await ResolveActiveSessionsAsync(request, cancellationToken);

        if (activeSessions.Count == 0)
        {
            return Result.Success(new CloudDriveAuthCheckDto
            {
                IsAuthenticated = false,
                IsLoggedIn = false,
                LastCheckedAt = checkedAt,
                FailureReason = "cookie_invalid",
            });
        }

        if (activeSessions.Count > 1)
        {
            return Result.Success(new CloudDriveAuthCheckDto
            {
                IsAuthenticated = false,
                IsLoggedIn = false,
                LastCheckedAt = checkedAt,
                FailureReason = "cookie_invalid",
            });
        }

        CookieSession session = activeSessions[0];
        if (session.Status != CookieSessionStatus.Active || session.IsInCooldown(checkedAt))
        {
            return Result.Success(new CloudDriveAuthCheckDto
            {
                IsAuthenticated = false,
                IsLoggedIn = false,
                LastCheckedAt = checkedAt,
                CooldownUntil = session.CooldownUntil,
                FailureReason = "cookie_not_active",
            });
        }

        CloudDriveApiContext context = CreateProbeContext();

        try
        {
            JsonElement _ = await openApiGateway.SendWebApiRequestAsync<JsonElement>(
                session.UserId,
                session.CloudAccountUid,
                HttpMethod.Get,
                "files",
                context,
                cancellationToken);

            return Result.Success(new CloudDriveAuthCheckDto
            {
                IsAuthenticated = true,
                IsLoggedIn = true,
                LastCheckedAt = checkedAt,
            });
        }
        catch (CloudDriveCookieInvalidatedException ex)
        {
            // CloudDriveApiClient 在检测到 401/403 或业务登录失效时已通过
            // CookieSessionManager.MarkInvalidatedAsync 持久化失效状态和冷却期，
            // QueryHandler 不再发出写命令，保持无副作用（CD0007）。
            return Result.Success(new CloudDriveAuthCheckDto
            {
                IsAuthenticated = false,
                IsLoggedIn = false,
                LastCheckedAt = checkedAt,
                FailureReason = NormalizeFailureReason(ex.ErrorCode, "cookie_invalid"),
            });
        }
        catch (CloudDriveNetworkException ex)
        {
            return Result.Success(CreateFailedStatus(checkedAt, ex.ErrorCode, "network"));
        }
        catch (CloudDriveApiException ex)
        {
            return Result.Success(CreateFailedStatus(checkedAt, ex.ErrorCode, "network"));
        }
    }

    private async Task<IReadOnlyList<CookieSession>> ResolveActiveSessionsAsync(
        GetLoginStatusQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CookieSession> sessions = request.UserId.HasValue
            ? await cookieSessionRepository.GetByUserAsync(request.UserId.Value, cancellationToken)
            : await cookieSessionRepository.GetAvailableSessionsAsync(cancellationToken);

        return sessions
            .Where(session => session.Status is CookieSessionStatus.Active)
            .ToList();
    }

    private static CloudDriveApiContext CreateProbeContext()
    {
        return new CloudDriveApiContext
        {
            Category = "auth",
            QueryParameters = new Dictionary<string, string>
            {
                ["cid"] = "0",
                ["limit"] = "1",
                ["format"] = "json",
            },
        };
    }

    private static CloudDriveAuthCheckDto CreateFailedStatus(
        DateTime checkedAt,
        string? failureReason,
        string fallback)
    {
        return new CloudDriveAuthCheckDto
        {
            IsAuthenticated = false,
            IsLoggedIn = null,
            LastCheckedAt = checkedAt,
            FailureReason = NormalizeFailureReason(failureReason, fallback),
        };
    }

    private static string NormalizeFailureReason(string? failureReason, string fallback)
    {
        if (string.IsNullOrWhiteSpace(failureReason))
        {
            return fallback;
        }

        return failureReason.Contains("network", StringComparison.OrdinalIgnoreCase)
            || failureReason.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            ? "network"
            : fallback;
    }
}
