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
    ICloudDriveOpenApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetLoginStatusQuery, CloudDriveLoginStatusDto>
{
    public async Task<Result<CloudDriveLoginStatusDto>> Handle(
        GetLoginStatusQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        DateTime checkedAt = DateTime.UtcNow;
        CookieSession? session = await cookieSessionRepository.GetByAccountAsync(
            request.UserId,
            request.Input.CloudAccountUid,
            cancellationToken);

        if (session is { Status: CookieSessionStatus.Invalidated }
            && session.IsInCooldown(checkedAt))
        {
            return Result.Success(new CloudDriveLoginStatusDto
            {
                CloudAccountUid = request.Input.CloudAccountUid,
                IsLoggedIn = false,
                LastCheckedAt = session.LastProbedAt ?? checkedAt,
                CooldownUntil = session.CooldownUntil,
                FailureReason = session.InvalidationReason,
            });
        }

        CloudDriveApiContext context = CreateProbeContext();

        try
        {
            JsonElement _ = await openApiGateway.SendWebApiRequestAsync<JsonElement>(
                request.UserId,
                request.Input.CloudAccountUid,
                HttpMethod.Get,
                "files",
                context,
                cancellationToken);

            return Result.Success(new CloudDriveLoginStatusDto
            {
                CloudAccountUid = request.Input.CloudAccountUid,
                IsLoggedIn = true,
                LastCheckedAt = checkedAt,
            });
        }
        catch (CloudDriveCookieInvalidatedException ex)
        {
            // CloudDriveApiClient 在检测到 401/403 或业务登录失效时已通过
            // CookieSessionManager.MarkInvalidatedAsync 持久化失效状态和冷却期，
            // QueryHandler 不再发出写命令，保持无副作用（CD0007）。
            return Result.Success(new CloudDriveLoginStatusDto
            {
                CloudAccountUid = request.Input.CloudAccountUid,
                IsLoggedIn = false,
                LastCheckedAt = checkedAt,
                FailureReason = ex.ErrorCode ?? "cookie_invalid",
            });
        }
        catch (CloudDriveNetworkException ex)
        {
            return Result.Success(CreateUnknownStatus(request.Input.CloudAccountUid, checkedAt, ex.ErrorCode));
        }
        catch (CloudDriveApiException ex)
        {
            return Result.Success(CreateUnknownStatus(request.Input.CloudAccountUid, checkedAt, ex.ErrorCode));
        }
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

    private static CloudDriveLoginStatusDto CreateUnknownStatus(
        string cloudAccountUid,
        DateTime checkedAt,
        string? failureReason)
    {
        return new CloudDriveLoginStatusDto
        {
            CloudAccountUid = cloudAccountUid,
            IsLoggedIn = null,
            LastCheckedAt = checkedAt,
            FailureReason = failureReason,
        };
    }
}
