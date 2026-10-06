<!-- Generated from .windsurf/rules/ddd-domain-patterns.md by scripts/sync-windsurf-rules.ps1. -->

# DDD 领域层编码规范

## 实体（Entity）

- 继承 `Entity<TId>` 基类
- 通过**私有/受保护构造函数** + 静态工厂方法 `Create()` 控制实例创建
- 属性使用 `private set` 或 `init`，防止外部直接修改状态
- **状态变更必须通过领域方法**（如 `order.Confirm()`），不暴露 setter
- 状态变更后通过 `RaiseDomainEvent()` 发布领域事件

```csharp
public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = [];

    private Order() { }

    public static Order Create(Guid customerId, IEnumerable<OrderItemData> items)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };
        order.RaiseDomainEvent(new OrderCreatedDomainEvent(order.Id));
        return order;
    }

    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    public Result Confirm()
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure(OrderErrors.InvalidStatusTransition(Status, OrderStatus.Confirmed));

        Status = OrderStatus.Confirmed;
        RaiseDomainEvent(new OrderConfirmedDomainEvent(Id));
        return Result.Success();
    }
}
```

---

## 值对象（Value Object）

- 使用 `sealed record` 实现（天然不可变 + 结构相等）
- 所有属性不可变（`init` 或只读）
- 在构造函数中执行不变量验证
- 无唯一标识，相等性基于属性值

```csharp
public sealed record Money(decimal Amount, string Currency)
{
    public Money
    {
        if (Amount < 0)
            throw new DomainException("金额不能为负数。");
        if (string.IsNullOrWhiteSpace(Currency))
            throw new DomainException("货币代码不能为空。");
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"无法将 {Currency} 与 {other.Currency} 相加。");
        return this with { Amount = Amount + other.Amount };
    }
}
```

---

## 聚合根（Aggregate Root）

```csharp
public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents()
        => _domainEvents.Clear();
}
```

**聚合根规范**：
- 外部只能持有聚合根的引用，不能直接持有聚合内部实体的引用
- 聚合根是**事务边界**，一个事务只修改一个聚合
- 聚合内部引用其他聚合时，只存储**外键 ID**（不存储对象引用）
- 领域事件通过 `RaiseDomainEvent()` 收集，由统一的 `DomainEventDispatchBehavior` 分发

---

## 领域事件（Domain Event）

- 使用 `sealed record` 定义，命名为 `{发生的事情}DomainEvent`（过去式）
- 只在**聚合根方法内**通过 `RaiseDomainEvent()` 发布
- Handler 位于 `Application` 层（不在 `Domain` 层）
- 领域事件 Handler 处理**领域内**副作用；跨上下文副作用通过集成事件

```csharp
// Domain/Orders/Events/OrderCreatedDomainEvent.cs
public sealed record OrderCreatedDomainEvent(Guid OrderId) : IDomainEvent;

// Application/Events/OrderCreatedDomainEventHandler.cs
internal sealed class OrderCreatedDomainEventHandler(
    INotificationService notificationService) : INotificationHandler<OrderCreatedDomainEvent>
{
    public async Task Handle(OrderCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        await notificationService.SendOrderConfirmationAsync(notification.OrderId, cancellationToken);
    }
}
```

---

## 仓储接口（Repository）

- 接口定义在 `Domain` 层，实现在 `Infrastructure` 层
- 每个**聚合根**对应一个仓储接口
- 仓储只负责聚合的持久化，不包含业务逻辑
- 查询方法返回完整聚合（包含必要子实体），避免懒加载
- 复杂查询（报表/列表）不通过仓储，直接由 Query Handler 处理

```csharp
// Domain/Orders/IOrderRepository.cs
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    void Add(Order order);
    void Remove(Order order);
}
```

---

## 领域服务（Domain Service）

只在跨聚合协调时使用，单聚合逻辑留在聚合内：

- 领域服务**无状态**，不持有仓储引用（仓储操作在 Application 层）
- 命名为 `{领域概念}Service`（非 `{动词}Service`）
- 仅处理跨聚合的业务规则计算，不做持久化操作

---

## 限界上下文

- 每个限界上下文是独立的业务边界，有自己的统一语言
- 上下文间通过**防腐层（ACL）** 或**共享内核**集成
- 跨上下文通信通过**集成事件**而非直接调用

---

## 统一语言

- 代码中的类名、方法名、变量名必须与业务术语一致
- 不使用技术术语替代业务概念（如 `OrderRecord` → 应命名为 `Order`）
