using Infrastructure.DependencyInjection;

using WebApi.Extensions;
using WebApi.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// CloudDrive 模块：Application + Infrastructure + Quartz + Data Protection
builder.Services.AddCloudDrive(builder.Configuration);

// JWT 认证：仅当配置存在时启用
if (builder.Configuration.GetSection(JwtAuthenticationExtensions.DefaultConfigSectionName).Exists())
{
    builder.Services.AddJwtAuthentication(builder.Configuration);
}

// WebApi 层：Controllers + OpenAPI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

// 中间件管道：异常处理最先，然后认证/授权，最后业务路由
app.UseExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>
/// 提供给集成测试使用的入口类型（<c>WebApplicationFactory&lt;Program&gt;</c>）。
/// </summary>
public partial class Program;
