using Infrastructure.DependencyInjection;

using WebApi.Extensions;
using WebApi.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// CloudDrive 模块：Application + Infrastructure + Quartz + Data Protection
builder.Services.AddCloudDrive(builder.Configuration);

// JWT 认证：缺失配置时启动失败，避免受保护 API 退化为匿名访问
builder.Services.AddJwtAuthentication(builder.Configuration);

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

app.MapControllers().RequireAuthorization();

app.Run();

/// <summary>
/// 提供给集成测试使用的入口类型（<c>WebApplicationFactory&lt;Program&gt;</c>）。
/// </summary>
public partial class Program;
