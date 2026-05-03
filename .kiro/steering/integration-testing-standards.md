---
inclusion: fileMatch
fileMatchPattern: "**/IntegrationTests/**/*.cs"
---

# 集成测试规范

## 测试工具栈

| 工具 | 用途 |
|---|---|
| **xUnit** | 测试框架 |
| **Microsoft.AspNetCore.Mvc.Testing** | 托管测试 API 服务 |
| **Testcontainers** | Docker 容器化真实依赖（数据库、Redis 等） |
| **FluentAssertions** | 语义化断言库 |
| **Respawn** | 数据库状态重置（每次测试后清理，比重建容器快数十倍） |

---

## 测试项目结构

```
tests/
└── IntegrationTests/
    ├── Infrastructure/
    │   ├── IntegrationTestWebAppFactory.cs   # WebApplicationFactory + Testcontainers
    │   ├── IntegrationTestCollection.cs      # xUnit Collection 定义
    │   └── BaseIntegrationTest.cs            # 测试基类
    ├── Orders/
    │   ├── CreateOrderTests.cs
    │   └── GetOrderTests.cs
    └── Common/
        └── Extensions/                       # HttpClient 扩展方法
```

---

## WebApplicationFactory 配置

```csharp
public sealed class IntegrationTestWebAppFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17")          // ✅ 固定镜像版本
        .WithDatabase("myapp_test")
        .Build();

    private Respawner _respawner = null!;
    private IDbConnection _dbConnection = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // 替换数据库连接字符串指向测试容器
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgresContainer.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        _dbConnection = new NpgsqlConnection(_postgresContainer.GetConnectionString());
        await _dbConnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
        });
    }

    public async Task ResetDatabaseAsync() => await _respawner.ResetAsync(_dbConnection);

    public new async Task DisposeAsync() => await _postgresContainer.StopAsync();
}
```

---

## Collection Fixture（共享容器实例）

```csharp
// 多个测试类共享同一个容器实例（避免每次都拉起新容器）
[CollectionDefinition(nameof(IntegrationTestCollection))]
public sealed class IntegrationTestCollection
    : ICollectionFixture<IntegrationTestWebAppFactory> { }
```

---

## 测试基类

```csharp
[Collection(nameof(IntegrationTestCollection))]
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly IntegrationTestWebAppFactory Factory;
    protected readonly HttpClient Client;
    protected readonly ISender Sender;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();

        // MediatR Sender 用于 Arrange 阶段快速准备数据
        var scope = factory.Services.CreateScope();
        Sender = scope.ServiceProvider.GetRequiredService<ISender>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    // 每次测试后重置数据库，确保测试隔离
    public async Task DisposeAsync() => await Factory.ResetDatabaseAsync();
}
```

---

## 集成测试编写规范

```csharp
public sealed class CreateOrderTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateOrder_WithValidRequest_ReturnsCreatedWithOrderId()
    {
        // Arrange
        var customerId = await CreateTestCustomerAsync();
        var request = new CreateOrderRequest { CustomerId = customerId, Items = [...] };

        // Act
        var response = await Client.PostAsJsonAsync("/api/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderId = await response.Content.ReadFromJsonAsync<Guid>();
        orderId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateOrder_WithValidRequest_PersistsOrderToDatabase()
    {
        // Arrange + Act
        var response = await Client.PostAsJsonAsync("/api/orders", validRequest);
        var orderId = await response.Content.ReadFromJsonAsync<Guid>();

        // Assert（直接查询数据库验证持久化）
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var order = await dbContext.Orders.FindAsync(orderId);
        order.Should().NotBeNull();
        order!.Status.Should().Be(OrderStatus.Pending);
    }

    private async Task<Guid> CreateTestCustomerAsync()
    {
        var result = await Sender.Send(new CreateCustomerCommand("测试客户", "test@example.com"));
        return result.Value;
    }
}
```

---

## HTTP 测试 vs 直接调用

| 方式 | 适用场景 |
|---|---|
| **HttpClient（HTTP 调用）** | 验证 API 契约（状态码、响应格式、Header） |
| **MediatR Sender（直接调用）** | 准备测试数据；验证应用层逻辑 |
| **DbContext（直接查数据库）** | 验证数据是否正确持久化 |

---

## 认证处理

```csharp
public static class AuthenticatedClientExtensions
{
    public static HttpClient WithAuthenticatedUser(
        this HttpClient client, Guid userId, string role = "User")
    {
        var token = JwtTokenFactory.CreateForUser(userId, role);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
```

---

## 规范要点

### 容器管理

- **固定镜像版本**（如 `postgres:17`），避免上游更新导致测试随机失败
- 使用 `IAsyncLifetime` 管理容器生命周期
- 多个测试类通过 `ICollectionFixture` **共享容器**（避免每次都拉起新容器）

### 数据隔离

- 使用 **Respawn** 在每次测试后清理数据库（比重建容器快数十倍）
- 不依赖测试执行顺序，每个测试自给自足
- 通过 **MediatR Sender** 直接发送命令来准备测试数据

---

## 禁止事项

- ❌ **不使用 InMemory 数据库替代真实数据库**（行为不一致，如不支持事务）
- ❌ **不在测试间共享可变状态**（每次测试后必须重置）
- ❌ **不依赖外部真实服务**（使用 Testcontainers 或 Mock Server）
- ❌ **不在集成测试中 Mock 仓储**（集成测试的价值就是测真实数据库交互）
- ❌ **不在测试类构造函数中执行异步操作**（使用 `IAsyncLifetime.InitializeAsync`）
