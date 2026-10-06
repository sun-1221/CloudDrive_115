using System.Text.Json;

using Application.Abstractions.CloudDrive;

using Domain.Exceptions;

namespace Infrastructure.Http;

/// <summary>
/// 115 Cookie 登录态探测器。
/// </summary>
internal sealed class LoginStatusProbe(ICloudDriveWebApiGateway gateway)
{
    /// <summary>
    /// 探测指定 115 账号的 Cookie 登录状态。
    /// </summary>
    public async Task<LoginStatusProbeResult> ProbeAsync(
        Guid userId,
        string cloudAccountUid,
        CancellationToken cancellationToken = default)
    {
        var context = new CloudDriveApiContext
        {
            Category = "auth",
            QueryParameters = new Dictionary<string, string>
            {
                ["cid"] = "0",
                ["limit"] = "1",
                ["format"] = "json",
            },
        };

        try
        {
            // 用最小 files 列表请求探测登录态，避免探测动作触发写操作或明显增加上游负载。
            JsonElement _ = await gateway.SendWebApiRequestAsync<JsonElement>(
                userId,
                cloudAccountUid,
                HttpMethod.Get,
                "files",
                context,
                cancellationToken);

            return new LoginStatusProbeResult(true, null);
        }
        catch (CloudDriveCookieInvalidatedException ex)
        {
            return new LoginStatusProbeResult(false, ex.ErrorCode);
        }
        catch (CloudDriveNetworkException ex)
        {
            return new LoginStatusProbeResult(null, ex.ErrorCode);
        }
        catch (CloudDriveApiException ex)
        {
            return new LoginStatusProbeResult(null, ex.ErrorCode);
        }
    }
}
