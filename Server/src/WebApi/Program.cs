using Application.CloudDrive;

using Infrastructure.DependencyInjection;

using WebApi.Extensions;
using WebApi.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// CloudDrive 模块：Application + Infrastructure + Quartz + Data Protection
builder.Services.AddCloudDrive(builder.Configuration);

// JWT 认证：缺失配置时启动失败，避免受保护 API 退化为匿名访问
builder.Services.AddJwtAuthentication(builder.Configuration);

// CORS：仅允许显式配置的前端域名访问
CloudDriveAppConfig cloudDriveConfig = new();
builder.Configuration.GetSection(CloudDriveAppConfig.SectionName).Bind(cloudDriveConfig);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (cloudDriveConfig.CorsAllowedOrigins is { Length: > 0 })
        {
            policy.WithOrigins(cloudDriveConfig.CorsAllowedOrigins);
        }

        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// WebApi 层：Controllers + OpenAPI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

// 中间件管道：异常处理最先，然后 CORS、认证/授权，最后业务路由
app.UseExceptionHandling();
app.UseCors();

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
