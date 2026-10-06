using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace WebApi.Tests.Support;

/// <summary>
/// 测试用 <see cref="IHostEnvironment"/>，用于控制中间件判断开发环境与否。
/// </summary>
public sealed class FakeHostEnvironment(string environmentName = "Production") : IHostEnvironment
{
    /// <inheritdoc />
    public string EnvironmentName { get; set; } = environmentName;

    /// <inheritdoc />
    public string ApplicationName { get; set; } = "WebApi.Tests";

    /// <inheritdoc />
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    /// <inheritdoc />
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
