using Application.Commands;

using Infrastructure.DependencyInjection;
using Infrastructure.Jobs;
using Infrastructure.Persistence;
using Infrastructure.Security;

using WebApi.Extensions;
using WebApi.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Application 层：MediatR + Pipeline Behaviors（Logging → Validation → Transaction → DomainEventDispatch）+ FluentValidation + 事务/领域事件抽象
builder.Services.AddApplicationServices(typeof(ICommand).Assembly);

// Infrastructure 层
string? connectionString = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    // 只有配置数据库连接时才启用 SqlSugar 与 CodeFirst，便于无数据库环境执行轻量启动和部分测试。
    builder.Services.AddSqlSugar(
        connectionString,
        builder.Configuration["Database:Type"]);
}

builder.Services.AddCloudDriveInfrastructure(builder.Configuration);
builder.Services.AddQuartzJobs();

// Data Protection key ring：若指定路径则持久化，否则使用默认存储
string? keyStoragePath = builder.Configuration["DataProtection:KeyStoragePath"];
builder.Services.AddSecretsProtection(keyStoragePath);

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
