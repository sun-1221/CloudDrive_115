<!-- Generated from .windsurf/rules/integration-testing-standards.md by scripts/sync-windsurf-rules.ps1. -->

# 集成测试规范

当前仓库尚未建立独立的 HTTP/宿主级集成测试基础设施；本规则用于新增真实持久化、真实 DI 或真实 HTTP 宿主测试时手动启用。

## 测试工具栈

| 工具 | 用途 |
|---|---|
| **xUnit** | 测试框架 |
| **SqlSugar + SQL Server** | 当前仓库真实持久化集成测试组合（单元测试可用 SQLite in-memory） |
| **Microsoft.AspNetCore.Mvc.Testing** | 需要端到端验证 Controller / Middleware / 认证管线时使用 |
| **Testcontainers / Respawn** | 引入 PostgreSQL、Redis 等真实依赖后再使用 |
| **xUnit Assert / FsCheck.Xunit** | 当前仓库常见断言与性质验证方式 |

---

## 测试项目结构

```
tests/
├── Infrastructure.Tests/
│   └── Persistence/
│       ├── RepositoryCrudPropertyTests.cs
│       └── CloudDriveTokenRepositoryIntegrationTests.cs
└── WebApi.Tests/
    └── Integration/
        └── CloudDriveRecycleBinControllerIntegrationTests.cs
```

其中前者贴合当前仓库现状；后者表示未来引入 HTTP 宿主级集成测试时的推荐放置位置。

---

## 真实 SqlSugar 仓储集成示例

```csharp
public sealed class CloudDriveTokenRepositoryIntegrationTests : IAsyncLifetime
{
    private ISqlSugarClient _db = null!;
    private CloudDriveTokenRepository _repository = null!;

    public Task InitializeAsync()
    {
        // 集成测试使用真实 SQL Server（本地实例或 Testcontainers）
        _db = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = "Server=.;Database=CloudDrive_Test;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;",
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute,
            ConfigureExternalServices = EntityMappingConfiguration.CreateExternalServices(),
        });

        SqlSugarSetup.InitCloudDriveTables(_db);
        _repository = new CloudDriveTokenRepository(_db, new SqlSugarDomainEventSource());

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task InsertAndGetByUserAndAccountAsync_PersistsToken()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        CloudDriveToken token = CloudDriveToken.Create(
            userId,
            "app-id",
            "uid-001",
            "enc-access",
            "enc-refresh",
            DateTime.UtcNow.AddHours(1));

        // Act
        await _repository.InsertAsync(token, CancellationToken.None);
        CloudDriveToken? loaded = await _repository.GetByUserAndAccountAsync(
            userId,
            "app-id",
            "uid-001",
            CancellationToken.None);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(token.Id, loaded.Id);
    }
}
```

---

## 可选：WebApplicationFactory 场景

- 若需要验证 `Controller`、`Middleware`、认证与 `ProblemDetails` 输出，可在 `WebApi.Tests` 中补充 HTTP 宿主级集成测试
- 使用 `WebApplicationFactory<Program>` 前，先为 `Program` 暴露可测试入口（如在 `Program.cs` 末尾声明 `public partial class Program { }`）
- 通过 `ConfigureAppConfiguration` 覆盖 `ConnectionStrings:Default`、`Jwt:*` 等配置，避免测试污染开发环境
- 当前项目使用 `SqlSugar + SQL Server`；HTTP 集成测试也应优先复用真实 `ISqlSugarClient` 配置，而不是沿用 EF Core 风格的 `DbContext` 样板

---

## HTTP 测试 vs 直接调用

| 方式 | 适用场景 |
|---|---|
| **HttpClient（HTTP 调用）** | 验证 API 契约（状态码、响应格式、Header） |
| **MediatR Sender（直接调用）** | 准备测试数据；验证应用层逻辑 |
| **仓储 / `ISqlSugarClient`（直接查数据库）** | 验证数据是否正确持久化 |

---

## 认证处理

- HTTP 集成测试应提供与 `AddJwtAuthentication()` 一致的测试 `Jwt` 配置，或在测试宿主中注册专用测试认证方案
- 测试数据中的 `NameIdentifier` claim 必须能被 `UserContextExtensions.GetAuthenticatedUserId()` 解析为合法 `Guid`
- 不要直接套用旧的 EF Core / 自定义 JWT 样板代码，测试辅助实现需与当前仓库认证和持久化方式一致

---

## 规范要点

### 数据库隔离

- 当前项目使用 SQL Server，集成测试可使用本地 SQL Server 实例或 Testcontainers (`testcontainers/mssql`)，并按需配合 Respawn 做状态重置
- 纯 CRUD 属性测试可保留 SQLite in-memory（如 `RepositoryCrudPropertyTests`），但不用于验证 SQL Server 特有行为
- 使用 `IAsyncLifetime` 管理数据库文件、容器或测试宿主生命周期

### 场景选择

- Repository / Persistence 集成测试优先落在 `Infrastructure.Tests`
- Middleware / Controller / Auth 管线测试优先落在 `WebApi.Tests`
- 不依赖测试执行顺序，每个测试自给自足

---

## 禁止事项

- ❌ **不使用内存集合、Fake Repository 或仅模拟行为的假数据库替代真实持久化**
- ❌ **不在测试间共享可变状态**（每次测试后必须重置）
- ❌ **不依赖外部真实服务**（使用 Testcontainers 或 Mock Server）
- ❌ **不在集成测试中 Mock 仓储**（集成测试的价值就是测真实数据库交互）
- ❌ **不在测试类构造函数中执行异步操作**（使用 `IAsyncLifetime.InitializeAsync`）
