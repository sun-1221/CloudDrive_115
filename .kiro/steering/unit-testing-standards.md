---
inclusion: fileMatch
fileMatchPattern: "**/UnitTests/**/*.cs"
---

# 单元测试规范

## 测试工具栈

| 工具 | 用途 |
|---|---|
| **xUnit** | 测试框架（.NET 原生支持） |
| **Moq** 或 **NSubstitute** | Mock/Stub 框架 |
| **FluentAssertions** | 语义化断言库 |
| **Bogus** | 测试数据生成 |

---

## 测试项目结构

```
tests/
└── UnitTests/
    ├── Domain/
    │   ├── Orders/
    │   │   └── OrderTests.cs
    │   └── ValueObjects/
    │       └── MoneyTests.cs
    ├── Application/
    │   └── Orders/
    │       ├── CreateOrderCommandHandlerTests.cs
    │       └── GetOrderByIdQueryHandlerTests.cs
    └── Common/
        └── Fakes/           # 通用 Factory 对象
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
    result.IsSuccess.Should().BeTrue();
    order.Status.Should().Be(OrderStatus.Confirmed);
}
```

---

## 测试规范要点

### 测试隔离

- 每个测试**完全独立**，不依赖其他测试的执行顺序
- 使用 Mock/Stub 隔离所有外部依赖（数据库、HTTP、文件系统）
- 不使用静态状态或共享可变状态

### 测试 Handler

```csharp
public sealed class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();
    private readonly Mock<ICustomerRepository> _customerRepositoryMock = new();

    private CreateOrderCommandHandler CreateHandler() =>
        new(_orderRepositoryMock.Object, _customerRepositoryMock.Object);

    [Fact]
    public async Task Handle_WhenCustomerExists_CreatesOrderAndReturnsId()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        _customerRepositoryMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CustomerFactory.Create(customerId));

        var command = new CreateOrderCommand(customerId, [new OrderItemDto(Guid.NewGuid(), 1, 100m)]);

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        _orderRepositoryMock.Verify(r => r.Add(It.Is<Order>(o => o.CustomerId == customerId)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCustomerNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        _customerRepositoryMock
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        var result = await CreateHandler().Handle(new CreateOrderCommand(customerId, []), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CustomerErrors.NotFound(customerId));
        _orderRepositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }
}
```

### 测试工厂（Factory）

使用 Factory 方法准备测试数据，避免在测试中重复构造：

```csharp
// tests/UnitTests/Common/Fakes/OrderFactory.cs
internal static class OrderFactory
{
    public static Order CreatePendingOrder(Guid? customerId = null) =>
        Order.Create(
            customerId ?? Guid.NewGuid(),
            [new OrderItemData(Guid.NewGuid(), 1, new Money(100m, "CNY"))]);

    public static Order CreateConfirmedOrder()
    {
        var order = CreatePendingOrder();
        order.Confirm();
        return order;
    }
}
```

---

## 禁止事项

- ❌ **不测试私有方法**（通过公开接口间接测试）
- ❌ **不在单元测试中访问真实数据库或外部服务**
- ❌ **不在 `Act` 阶段做多个操作**（一个测试只验证一件事）
- ❌ **不使用魔法字符串/数字**（使用常量或具名变量）
- ❌ **不让测试相互依赖**（不共享可变状态）

```csharp
// ❌ 错误：一个测试验证了多件事
[Fact]
public void OrderLifecycle_Test()
{
    var order = Order.Create(...);
    order.Confirm();
    order.Ship();
    order.Complete();
    order.Status.Should().Be(OrderStatus.Completed);
}

// ✅ 正确：每个测试只做一件事
[Fact]
public void Complete_WhenOrderIsShipped_SetsStatusToCompleted()
{
    var order = OrderFactory.CreateShippedOrder();
    var result = order.Complete();
    result.IsSuccess.Should().BeTrue();
    order.Status.Should().Be(OrderStatus.Completed);
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
        result.Amount.Should().Be(expected);
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ThrowsDomainException()
    {
        var act = () => new Money(-1m, "CNY");
        act.Should().Throw<DomainException>().WithMessage("*金额不能为负数*");
    }
}
```
