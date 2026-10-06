using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

namespace WebApi.Tests.Integration;

/// <summary>
/// 验证未采集能力从真实 HTTP 入口直接 fail-closed。
/// </summary>
public sealed class CloudDriveCapabilityIntegrationTests(CloudDriveWebApplicationFactory factory)
    : IClassFixture<CloudDriveWebApplicationFactory>
{
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly HttpClient client = factory.CreateClient();
    private readonly CloudDriveSideEffectProbe probe = factory.Services.GetRequiredService<CloudDriveSideEffectProbe>();

    [Theory]
    [InlineData("GET", "/api/files/search?searchValue=demo", null)]
    [InlineData("POST", "/api/files/move", "{\"fileIds\":[\"file-1\"],\"targetDirectoryId\":\"dir-1\"}")]
    [InlineData("DELETE", "/api/files", "{\"fileIds\":[\"file-1\"]}")]
    [InlineData("POST", "/api/recycle/restore", "{\"recycleBinItemIds\":[\"item-1\"]}")]
    [InlineData("DELETE", "/api/recycle/item-1", null)]
    [InlineData("DELETE", "/api/recycle", null)]
    [InlineData("POST", "/api/offline/tasks/urls", "{\"urls\":[\"https://example.test/file\"]}")]
    [InlineData("POST", "/api/offline/tasks/bt", "{\"infoHash\":\"hash-1\"}")]
    [InlineData("POST", "/api/offline/parse-torrent", null)]
    [InlineData("DELETE", "/api/offline/tasks/hash-1", null)]
    [InlineData("POST", "/api/offline/tasks:clear", "{}")]
    [InlineData("GET", "/api/shares?tab=send", null)]
    [InlineData("GET", "/api/shares?tab=receive", null)]
    [InlineData("POST", "/api/videos/pick-1/transcode", null)]
    [InlineData("POST", "/api/videos/pick-1/history", "{\"time\":30,\"watchEnd\":true}")]
    public async Task DisabledEndpoint_WithValidJwt_Returns501ProblemDetails(
        string method,
        string url,
        string? json)
    {
        using HttpRequestMessage request = new(new HttpMethod(method), url);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenHelper.GenerateToken(TestUserId));
        if (json is not null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        int gatewayCallsBefore = probe.GatewayCalls;
        int jobWritesBefore = probe.JobWrites;
        int cacheInvalidationsBefore = probe.CacheInvalidations;

        using HttpResponseMessage response = await client.SendAsync(request);
        string responseBody = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode == HttpStatusCode.NotImplemented,
            $"Expected 501 but received {(int)response.StatusCode}: {responseBody}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using JsonDocument document = JsonDocument.Parse(responseBody);
        JsonElement root = document.RootElement;
        Assert.Equal(501, root.GetProperty("status").GetInt32());
        Assert.Equal("cloud_drive_api_not_collected", root.GetProperty("code").GetString());
        Assert.Equal(gatewayCallsBefore, probe.GatewayCalls);
        Assert.Equal(jobWritesBefore, probe.JobWrites);
        Assert.Equal(cacheInvalidationsBefore, probe.CacheInvalidations);
    }

    [Fact]
    public async Task UploadEndpoint_WithValidJwt_Returns501WithoutBackendSideEffects()
    {
        using MultipartFormDataContent content = new();
        using ByteArrayContent file = new("dummy"u8.ToArray());
        content.Add(file, "file", "dummy.txt");
        content.Add(new StringContent("0"), "directoryId");
        content.Add(new StringContent("false"), "forceAsync");

        int gatewayCallsBefore = probe.GatewayCalls;
        int jobWritesBefore = probe.JobWrites;
        int cacheInvalidationsBefore = probe.CacheInvalidations;

        using HttpRequestMessage request = new(HttpMethod.Post, "/api/files/upload")
        {
            Content = content,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokenHelper.GenerateToken(TestUserId));

        using HttpResponseMessage response = await client.SendAsync(request);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        using JsonDocument document = JsonDocument.Parse(body);
        Assert.Equal("cloud_drive_api_not_collected", document.RootElement.GetProperty("code").GetString());
        Assert.Equal(gatewayCallsBefore, probe.GatewayCalls);
        Assert.Equal(jobWritesBefore, probe.JobWrites);
        Assert.Equal(cacheInvalidationsBefore, probe.CacheInvalidations);
    }
}
