using System.Net;

namespace WebApi.Tests.Integration;

/// <summary>
/// 端到端授权测试：验证所有受保护端点在缺少 JWT 时返回 401 Unauthorized。
/// </summary>
public sealed class AuthorizationIntegrationTests(CloudDriveWebApplicationFactory factory)
    : IClassFixture<CloudDriveWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("GET", "/api/files")]
    [InlineData("GET", "/api/user/profile")]
    [InlineData("GET", "/api/jobs")]
    [InlineData("GET", "/api/offline/tasks")]
    [InlineData("GET", "/api/recycle")]
    [InlineData("GET", "/api/shares")]
    [InlineData("GET", "/api/videos/test/play")]
    public async Task ProtectedEndpoint_WithoutToken_Returns401(string method, string url)
    {
        // Arrange（准备）
        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        // Act（执行）
        HttpResponseMessage response = await _client.SendAsync(request);

        // Assert（断言）
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RemovedAdminEndpoint_WithoutToken_Returns404()
    {
        // Arrange（准备）
        // 管理员入口已被个人账号 API 替换，不再存在。

        // Act（执行）
        HttpResponseMessage response = await _client.PostAsync("/api/admin/cloud-drive/cookie-sessions", null);

        // Assert（断言）
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
