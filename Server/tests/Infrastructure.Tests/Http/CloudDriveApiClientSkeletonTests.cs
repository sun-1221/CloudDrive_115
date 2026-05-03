using Application.CloudDrive;

using Infrastructure.Http;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Tests.Http;

public sealed class CloudDriveApiClientSkeletonTests
{
    [Theory]
    [InlineData("https://my.115.com/proapi/3.0/index.php", CloudDriveHttpClientNames.My)]
    [InlineData("vip.115.com", CloudDriveHttpClientNames.Vip)]
    [InlineData("pay.115.com", CloudDriveHttpClientNames.Pay)]
    [InlineData("webapi.115.com", CloudDriveHttpClientNames.Web)]
    [InlineData(null, CloudDriveHttpClientNames.My)]
    public void ResolveUserCenterClientName_RoutesKnownDomains(string? targetDomain, string expectedClientName)
    {
        string clientName = CloudDriveApiClient.ResolveUserCenterClientName(targetDomain);

        Assert.Equal(expectedClientName, clientName);
    }

    [Fact]
    public void AddCloudDriveHttpClients_ConfiguresNamedClientBaseAddresses()
    {
        var services = new ServiceCollection();
        services.AddOptions<CloudDriveAppConfig>()
            .Configure(config =>
            {
                config.WebApiBaseUrl = "https://webapi.115.com/";
                config.MyApiBaseUrl = "https://my.115.com/";
                config.VipApiBaseUrl = "https://vip.115.com/";
                config.PayApiBaseUrl = "https://pay.115.com/";
                config.OfflineApiBaseUrl = "https://115.com/web/lixian/";
            });
        services.AddCloudDriveHttpClients();

        using ServiceProvider provider = services.BuildServiceProvider();
        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();

        Assert.Equal(new Uri("https://webapi.115.com/"), factory.CreateClient(CloudDriveHttpClientNames.Web).BaseAddress);
        Assert.Equal(new Uri("https://my.115.com/"), factory.CreateClient(CloudDriveHttpClientNames.My).BaseAddress);
        Assert.Equal(new Uri("https://vip.115.com/"), factory.CreateClient(CloudDriveHttpClientNames.Vip).BaseAddress);
        Assert.Equal(new Uri("https://pay.115.com/"), factory.CreateClient(CloudDriveHttpClientNames.Pay).BaseAddress);
        Assert.Equal(new Uri("https://115.com/web/lixian/"), factory.CreateClient(CloudDriveHttpClientNames.Offline).BaseAddress);
    }
}
