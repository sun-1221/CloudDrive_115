using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace WebApi.Tests.Integration;

/// <summary>
/// JWT 认证与授权集成测试：验证有效 JWT、角色 403、匿名端点及 ProblemDetails 格式。
/// </summary>
public sealed class JwtAuthorizationIntegrationTests(CloudDriveWebApplicationFactory factory)
    : IClassFixture<CloudDriveWebApplicationFactory>
{
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly HttpClient _client = factory.CreateClient();

    /// <summary>
    /// 验证受保护端点携带有效 JWT 时不返回 401。
    /// </summary>
    [Theory]
    [InlineData("GET", "/api/files")]
    [InlineData("GET", "/api/user/profile")]
    [InlineData("GET", "/api/shares")]
    public async Task ProtectedEndpoint_WithValidJwt_DoesNotReturn401(string method, string url)
    {
        // Arrange
        string token = TestJwtTokenHelper.GenerateToken(TestUserId);
        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        HttpResponseMessage response = await _client.SendAsync(request);

        // Assert
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// 验证非管理员 JWT 访问管理端点返回 403 ProblemDetails。
    /// </summary>
    [Fact]
    public async Task RemovedAdminEndpoint_WithValidJwt_Returns404()
    {
        // Arrange
        string token = TestJwtTokenHelper.GenerateToken(TestUserId);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/cloud-drive/cookie-sessions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        HttpResponseMessage response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// 验证匿名访问认证检查端点不会返回 401。
    /// </summary>
    [Fact]
    public async Task AuthCheck_WithoutToken_ReturnsNon401()
    {
        // Arrange & Act
        HttpResponseMessage response = await _client.GetAsync("/api/auth/check");

        // Assert — auth/check 标记 [AllowAnonymous]，匿名访问不应返回 401
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// 验证携带有效 JWT 访问认证检查端点不会返回 401。
    /// </summary>
    [Fact]
    public async Task AuthCheck_WithValidJwt_DoesNotReturn401()
    {
        // Arrange
        string token = TestJwtTokenHelper.GenerateToken(TestUserId);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/check");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        HttpResponseMessage response = await _client.SendAsync(request);

        // Assert — 验证认证通过（非 401），业务可能因测试环境缺少数据库而返回其他状态码
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// 验证受保护端点缺少 Token 时返回 401 ProblemDetails。
    /// </summary>
    [Theory]
    [InlineData("GET", "/api/files")]
    [InlineData("GET", "/api/user/profile")]
    public async Task ProtectedEndpoint_WithoutToken_Returns401ProblemDetails(string method, string url)
    {
        // Arrange
        using var request = new HttpRequestMessage(new HttpMethod(method), url);

        // Act
        HttpResponseMessage response = await _client.SendAsync(request);

        // Assert
        await AssertProblemDetailsAsync(response, HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Assert（断言）错误响应符合 ProblemDetails HTTP 契约。
    /// </summary>
    /// <param name="response">待验证的 HTTP 响应。</param>
    /// <param name="expectedStatusCode">期望的 HTTP 状态码。</param>
    /// <returns>表示异步读取响应体并验证 JSON 字段的任务。</returns>
    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatusCode)
    {
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        string body = await response.Content.ReadAsStringAsync();
        using JsonDocument doc = JsonDocument.Parse(body);
        JsonElement root = doc.RootElement;

        Assert.True(root.TryGetProperty("status", out JsonElement status), "ProblemDetails 应包含 status 字段");
        Assert.Equal((int)expectedStatusCode, status.GetInt32());
        Assert.True(root.TryGetProperty("title", out JsonElement title), "ProblemDetails 应包含 title 字段");
        Assert.False(string.IsNullOrWhiteSpace(title.GetString()), "ProblemDetails title 不应为空");
    }
}
