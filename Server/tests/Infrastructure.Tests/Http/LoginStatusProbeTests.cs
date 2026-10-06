using System.Text.Json;

using Application.Abstractions.CloudDrive;

using Domain.Exceptions;

using Infrastructure.Http;

namespace Infrastructure.Tests.Http;

/// <summary>
/// 115 网盘登录状态探测器测试。
/// </summary>
public sealed class LoginStatusProbeTests
{
    /// <summary>
    /// 验证网关探测成功时返回已登录。
    /// </summary>
    [Fact]
    public async Task ProbeAsync_WhenGatewaySucceeds_ReturnsLoggedInTrueAsync()
    {
        // Arrange（准备）
        var gateway = new StubGateway(JsonDocument.Parse("""{"state":true}""").RootElement.Clone());
        var probe = new LoginStatusProbe(gateway);

        // Act（执行）
        LoginStatusProbeResult result = await probe.ProbeAsync(
            Guid.NewGuid(),
            "115001",
            CancellationToken.None);

        // Assert（断言）
        Assert.True(result.IsLoggedIn);
        Assert.Null(result.FailureReason);
        Assert.Equal("files", gateway.LastEndpoint);
        Assert.Equal("auth", gateway.LastContext?.Category);
        Assert.Equal("1", gateway.LastContext?.QueryParameters?["limit"]);
    }

    /// <summary>
    /// 验证 Cookie 失效时登录状态探测返回未登录。
    /// </summary>
    [Fact]
    public async Task ProbeAsync_WhenCookieInvalidated_ReturnsLoggedInFalseAsync()
    {
        // Arrange（准备）
        var gateway = new StubGateway(new CloudDriveCookieInvalidatedException(errorCode: "cloud_drive_cookie_invalidated"));
        var probe = new LoginStatusProbe(gateway);

        // Act（执行）
        LoginStatusProbeResult result = await probe.ProbeAsync(
            Guid.NewGuid(),
            "115001",
            CancellationToken.None);

        // Assert（断言）
        Assert.False(result.IsLoggedIn);
        Assert.Equal("cloud_drive_cookie_invalidated", result.FailureReason);
    }

    /// <summary>
    /// 验证网络异常时登录状态探测返回未知状态。
    /// </summary>
    [Fact]
    public async Task ProbeAsync_WhenNetworkUnknown_ReturnsLoggedInNullAsync()
    {
        // Arrange（准备）
        var gateway = new StubGateway(new CloudDriveNetworkException(errorCode: "cloud_drive_network_error"));
        var probe = new LoginStatusProbe(gateway);

        // Act（执行）
        LoginStatusProbeResult result = await probe.ProbeAsync(
            Guid.NewGuid(),
            "115001",
            CancellationToken.None);

        // Assert（断言）
        Assert.Null(result.IsLoggedIn);
        Assert.Equal("cloud_drive_network_error", result.FailureReason);
    }

    private sealed class StubGateway(object responseOrException) : ICloudDriveWebApiGateway
    {
        public string? LastEndpoint { get; private set; }

        public CloudDriveApiContext? LastContext { get; private set; }

        public Task<T> SendWebApiRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            HttpMethod method,
            string endpoint,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default)
        {
            LastEndpoint = endpoint;
            LastContext = context;

            if (responseOrException is Exception exception)
            {
                return Task.FromException<T>(exception);
            }

            return Task.FromResult((T)responseOrException);
        }

        public Task<T> SendUserCenterRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            HttpMethod method,
            string endpoint,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<T> SendOfflineApiRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            HttpMethod method,
            string endpoint,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<T> SendUploadInitRequestAsync<T>(
            Guid userId,
            string cloudAccountUid,
            CloudDriveApiContext context,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<T> UploadFileAsync<T>(
            Guid userId,
            string cloudAccountUid,
            string uploadUrl,
            Stream fileStream,
            string fileName,
            IDictionary<string, string>? formFields = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
