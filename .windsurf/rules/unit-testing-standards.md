---
trigger: glob
globs: "tests/**/*.cs"
---

# 单元测试规范

## 测试工具栈

| 工具 | 用途 |
|---|---|
| **xUnit** | 测试框架（当前仓库主测试框架） |
| **FsCheck.Xunit** | 属性测试（`[Property]`），适合不变量、映射和中间件性质验证 |
| **手写 Fake / Stub / Spy** | 隔离仓储、HTTP、缓存等外部依赖 |
| **xUnit Assert** | 当前仓库默认断言方式 |

---

## 测试项目结构

```
tests/
├── Application.Tests/
│   └── CloudDrive/
│       └── JobQueryHandlerTests.cs
├── Domain.Tests/
│   └── EntityDomainEventPropertyTests.cs
├── Infrastructure.Tests/
│   └── CloudDrive/
│       ├── CloudDriveApiClientRateLimitTests.cs
│       └── ErrorCodeMappingPropertyTests.cs
└── WebApi.Tests/
    └── Middleware/
        └── ExceptionHandlingMiddlewarePropertyTests.cs
```

---

## 命名规范

测试方法名格式：**`{被测方法}_{场景描述}_{期望结果}`**

```csharp
// ✅ 正确
public void Confirm_WhenOrderIsPending_ReturnsSuccess()
public void Confirm_WhenOrderIsAlreadyCancelled_ReturnsFailure()
public void Create_WithNegativeAmount_ThrowsDomainException()
public async Task Handle_WhenCustomerNotFound_ReturnsCustomerNotFoundError()
public async Task Handle_WhenValidCommand_CreatesOrderAndPublishesDomainEvent()
```

---

## AAA 模式（Arrange-Act-Assert）

每个测试必须遵循 AAA 结构，三个区域用注释分隔：

```csharp
[Fact]
public void Confirm_WhenOrderIsPending_SetsStatusToConfirmed()
{
    // Arrange
    var order = Order.Create(
        customerId: Guid.NewGuid(),
        items: [new OrderItemData(Guid.NewGuid(), 2, new Money(50m, "CNY"))]);

    // Act
    var result = order.Confirm();

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal(OrderStatus.Confirmed, order.Status);
}
```

---

## 测试规范要点

### 测试隔离

- 每个测试**完全独立**，不依赖其他测试的执行顺序
- 使用手写 Fake/Stub/Spy 隔离所有外部依赖（数据库、HTTP、文件系统）
- 不使用静态状态或共享可变状态

### 属性测试（FsCheck）

- 对领域不变量、错误码映射、`ProblemDetails` 输出等稳定性质优先使用 `[Property]`
- 属性测试可直接返回 `bool`，也可配合 `Assert` / `Assert.ThrowsAsync` 验证约束
- 对路径、异常消息、枚举索引等随机输入，先做必要清洗，避免生成无效用例

### xUnit Theory（数据驱动测试）

- 当**同一断言逻辑**需要覆盖多个固定样例、边界值或等价类时，优先使用 `[Theory]`，不要把同构场景拆成多个近似 `[Fact]`
- 小规模、可直接内联的样例优先使用 `[InlineData]`；样例较多、需要复用或构造成本较高时使用 `[MemberData]` 或 `[ClassData]`
- 值对象运算、校验器、映射函数、错误码映射、参数矩阵这类“输入变、断言结构不变”的测试，优先写成 `[Theory]`
- 若测试只验证一个独立业务场景，且没有明显的数据矩阵价值，继续使用 `[Fact]`
- 若目标是验证随机输入下始终成立的不变量，而不是有限样例集合，应优先改用 `[Property]`，不要用超长 `[Theory]` 代替属性测试

### 测试 Handler

```csharp
public sealed class JobQueryHandlerTests
{
    private static readonly Guid TestUserId = Guid.NewGuid();
    private const string TestCloudAccountUid = "uid-001";

    [Fact]
    public async Task GetJobStatus_JobExists_ReturnsSuccess()
    {
        CloudDriveJob job = CloudDriveJob.Create(TestUserId, TestCloudAccountUid, CloudDriveJobType.Upload, "{}");
        var repo = new FakeJobRepository([job]);
        var handler = new GetJobStatusQueryHandler(repo, CreateEnabledConfig());

        var query = new GetJobStatusQuery(TestUserId, new GetJobStatusInput
        {
            CloudAccountUid = TestCloudAccountUid,
            JobId = job.Id,
        });

        // Act
        Result<CloudDriveJobDto> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(job.Id, result.Value.Id);
    }
}
```

### 测试辅助对象（Factory / Helper）

当前仓库更常用局部 Helper 方法准备配置与实体，再配合手写 `Fake*Repository` 放在同一测试文件中：

```csharp
private static IOptions<CloudDriveAppConfig> CreateEnabledConfig() => Options.Create(new CloudDriveAppConfig
{
    AppId = "test-app",
    AppSecret = "test-secret",
    CallbackUrl = "https://callback.test",
    CloudDriveFeatureEnabled = true,
});
```

---

## 禁止事项

- ❌ **不测试私有方法**（通过公开接口间接测试）
- ❌ **不在单元测试中访问真实数据库或外部服务**
- ❌ **不在 `Act` 阶段做多个操作**（一个测试只验证一件事）
- ❌ **不使用魔法字符串/数字**（使用常量或具名变量）
- ❌ **不让测试相互依赖**（不共享可变状态）

```csharp
[Fact]
public async Task GetJobList_ReturnsPagedResult()
{
    CloudDriveJob j1 = CloudDriveJob.Create(TestUserId, TestCloudAccountUid, CloudDriveJobType.Upload, "{}");
    CloudDriveJob j2 = CloudDriveJob.Create(TestUserId, TestCloudAccountUid, CloudDriveJobType.Transcode, "{}");
    var repo = new FakeJobRepository([j1, j2]);
    var handler = new GetJobListQueryHandler(repo, CreateEnabledConfig());

    Result<PagedResultDto<CloudDriveJobDto>> result = await handler.Handle(
        new GetJobListQuery(TestUserId, new GetJobListInput { CloudAccountUid = TestCloudAccountUid }),
        CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal(2, result.Value.Items.Count);
}
```

---

## 值对象测试

```csharp
public sealed class MoneyTests
{
    [Theory]
    [InlineData(100, 50, 150)]
    [InlineData(0, 100, 100)]
    public void Add_WithSameCurrency_ReturnsCorrectSum(decimal a, decimal b, decimal expected)
    {
        var result = new Money(a, "CNY").Add(new Money(b, "CNY"));
        Assert.Equal(expected, result.Amount);
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new Money(-1m, "CNY"));
    }
}
```
