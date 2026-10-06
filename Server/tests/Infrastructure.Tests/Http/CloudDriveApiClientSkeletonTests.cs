using Application.CloudDrive;

using Infrastructure.Http;

using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Http;

public sealed class CloudDriveApiClientSkeletonTests
{
    [Theory]
    [InlineData("https://my.115.com/proapi/3.0/index.php", CloudDriveHttpClientNames.My)]
    [InlineData("vip.115.com", CloudDriveHttpClientNames.Vip)]
    [InlineData("pay.115.com", CloudDriveHttpClientNames.Pay)]
    [InlineData("webapi.115.com", CloudDriveHttpClientNames.Web)]
    [InlineData(null, CloudDriveHttpClientNames.My)]
    public void ResolveUserCenterClientName_WhenDomainKnown_RoutesKnownDomains(string? targetDomain, string expectedClientName)
    {
        // Arrange（准备）

        // Act（执行）
        string clientName = CloudDriveApiClient.ResolveUserCenterClientName(targetDomain);

        // Assert（断言）
        Assert.Equal(expectedClientName, clientName);
    }

    [Fact]
    public void AddCloudDriveHttpClients_WhenCalled_ConfiguresNamedClientBaseAddresses()
    {
        // Arrange（准备）
        var services = new ServiceCollection();
        services.AddOptions<CloudDriveAppConfig>()
            .Configure(config =>
            {
                config.WebApiBaseUrl = "https://webapi.115.com/";
                config.MyApiBaseUrl = "https://my.115.com/";
                config.VipApiBaseUrl = "https://vip.115.com/";
                config.PayApiBaseUrl = "https://pay.115.com/";
                config.OfflineApiBaseUrl = "https://clouddownload.115.com/web/";
            });
        services.AddCloudDriveHttpClients();

        using ServiceProvider provider = services.BuildServiceProvider();

        // Act（执行）
        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();

        // Assert（断言）
        Assert.Equal(new Uri("https://webapi.115.com/"), factory.CreateClient(CloudDriveHttpClientNames.Web).BaseAddress);
        Assert.Equal(new Uri("https://my.115.com/"), factory.CreateClient(CloudDriveHttpClientNames.My).BaseAddress);
        Assert.Equal(new Uri("https://vip.115.com/"), factory.CreateClient(CloudDriveHttpClientNames.Vip).BaseAddress);
        Assert.Equal(new Uri("https://pay.115.com/"), factory.CreateClient(CloudDriveHttpClientNames.Pay).BaseAddress);
        Assert.Equal(new Uri("https://clouddownload.115.com/web/"), factory.CreateClient(CloudDriveHttpClientNames.Offline).BaseAddress);
        Assert.Equal(new Uri("https://uplb.115.com/"), factory.CreateClient(CloudDriveHttpClientNames.Upload).BaseAddress);
        Assert.Null(factory.CreateClient(CloudDriveHttpClientNames.UploadDirect).BaseAddress);
    }
}
