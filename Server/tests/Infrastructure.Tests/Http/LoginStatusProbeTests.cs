using System.Text.Json;

using Application.Abstractions.CloudDrive;

using Domain.Exceptions;

using Infrastructure.Http;

namespace Infrastructure.Tests.Http;

public sealed class LoginStatusProbeTests
{
    [Fact]
    public async Task ProbeAsync_WhenGatewaySucceeds_ReturnsLoggedInTrueAsync()
    {
        // 准备
        var gateway = new StubGateway(JsonDocument.Parse("""{"state":true}""").RootElement.Clone());
        var probe = new LoginStatusProbe(gateway);

        // 执行
        LoginStatusProbeResult result = await probe.ProbeAsync(
            Guid.NewGuid(),
            "115001",
            CancellationToken.None);

        // 断言
        Assert.True(result.IsLoggedIn);
        Assert.Null(result.FailureReason);
        Assert.Equal("files", gateway.LastEndpoint);
        Assert.Equal("auth", gateway.LastContext?.Category);
        Assert.Equal("1", gateway.LastContext?.QueryParameters?["limit"]);
    }

    [Fact]
    public async Task ProbeAsync_WhenCookieInvalidated_ReturnsLoggedInFalseAsync()
    {
        // 准备
        var gateway = new StubGateway(new CloudDriveCookieInvalidatedException(errorCode: "cloud_drive_cookie_invalidated"));
        var probe = new LoginStatusProbe(gateway);

        // 执行
        LoginStatusProbeResult result = await probe.ProbeAsync(
            Guid.NewGuid(),
            "115001",
            CancellationToken.None);

        // 断言
        Assert.False(result.IsLoggedIn);
        Assert.Equal("cloud_drive_cookie_invalidated", result.FailureReason);
    }

    [Fact]
    public async Task ProbeAsync_WhenNetworkUnknown_ReturnsLoggedInNullAsync()
    {
        // 准备
        var gateway = new StubGateway(new CloudDriveNetworkException(errorCode: "cloud_drive_network_error"));
        var probe = new LoginStatusProbe(gateway);

        // 执行
        LoginStatusProbeResult result = await probe.ProbeAsync(
            Guid.NewGuid(),
            "115001",
            CancellationToken.None);

        // 断言
        Assert.Null(result.IsLoggedIn);
        Assert.Equal("cloud_drive_network_error", result.FailureReason);
    }

    private sealed class StubGateway(object responseOrException) : ICloudDriveOpenApiGateway
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
