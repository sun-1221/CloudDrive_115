using Application.CloudDrive.Dtos;
using Application.CloudDrive.Mappings;
using Application.Queries;

using Domain.Results;

using Microsoft.Extensions.Options;

namespace Application.CloudDrive.Auth;

internal sealed class GetBoundAccountsQueryHandler(
    CookieSessionManager cookieSessionManager,
    IOptions<CloudDriveAppConfig> configOptions)
    : IQueryHandler<GetBoundAccountsQuery, List<CloudDriveAccountSummaryDto>>
{
    public async Task<Result<List<CloudDriveAccountSummaryDto>>> Handle(
        GetBoundAccountsQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveHandlerGuard.EnsureEnabled(configOptions.Value);

        IReadOnlyList<CookieSessionSummary> summaries = await cookieSessionManager.GetBoundAccountsAsync(
            request.UserId,
            cancellationToken);

        List<CloudDriveAccountSummaryDto> result = summaries
            .Select(summary => summary.ToDto(includeOnlyAccountSummary: true))
            .ToList();

        return Result.Success(result);
    }
}
