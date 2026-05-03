---
inclusion: fileMatch
fileMatchPattern: "**/Application/**/*.cs"
---

# CQRS 与 Application 层规范

本项目使用 MediatR 实现 CQRS，所有 Command/Query 通过 `ISender.Send()` 分发。

---

## 基础接口定义

Application 层定义基础接口，Handler 实现这些接口（不直接继承 MediatR 原始接口）：

```csharp
// 命令接口
public interface ICommand : IRequest<Result> { }
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }

// 查询接口
public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }

// Handler 接口
public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand { }
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse> { }
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse> { }
```

---

## Command 规范

### 命名规则

- 命令类名：`{动词}{名词}Command`（`CreateOrderCommand`、`CancelBookingCommand`）
- 命令处理器：`{命令名}Handler`（`CreateOrderCommandHandler`）
- 文件夹：`Application/Commands/{功能模块}/`

### 定义规则

- 命令对象使用 `sealed record`（不可变性）
- Handler 类使用 `internal sealed` 修饰
- Handler 内**不应包含复杂的业务验证**，验证通过 FluentValidation + Pipeline Behavior 处理
- Handler 内**不应直接调用其他 Handler**，通过领域事件或 Outbox 解耦
- 业务错误通过 `Result<T>` 返回，基础设施异常仍然抛出

```csharp
// Application/Commands/Orders/CreateOrderCommand.cs
public sealed record CreateOrderCommand(
    Guid CustomerId,
    IReadOnlyList<OrderItemDto> Items) : ICommand<Guid>;

// Application/Commands/Orders/CreateOrderCommandHandler.cs
internal sealed class CreateOrderCommandHandler(
    IOrderRepository orderRepository,
    ICustomerRepository customerRepository) : ICommandHandler<CreateOrderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            return Result.Failure<Guid>(CustomerErrors.NotFound(request.CustomerId));

        var order = Order.Create(customer.Id, request.Items);
        orderRepository.Add(order);

        return Result.Success(order.Id);
    }
}
```

---

## Query 规范

### 命名规则

- 查询类名：`Get{名词}{修饰}Query`（`GetOrderByIdQuery`、`GetActiveOrdersQuery`）
- 查询处理器：`{查询名}Handler`（`GetOrderByIdQueryHandler`）
- 文件夹：`Application/Queries/{功能模块}/`

### 约束

- Query Handler **不允许副作用**：严禁 Insert/Update/Delete 操作
- Query Handler **不允许发布领域事件**（`IMediator.Publish()`/`IPublisher.Publish()`）
- 可跳过领域模型直接读取数据库（SqlSugar 投影）

```csharp
public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDetailDto>;

internal sealed class GetOrderByIdQueryHandler(
    ISqlSugarClient db) : IQueryHandler<GetOrderByIdQuery, OrderDetailDto>
{
    public async Task<Result<OrderDetailDto>> Handle(
        GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var order = await db.Queryable<Order>()
            .Where(o => o.Id == request.OrderId)
            .Select<OrderDetailDto>()
            .FirstAsync();

        if (order is null)
            return Result.Failure<OrderDetailDto>(OrderErrors.NotFound(request.OrderId));

        return Result.Success(order);
    }
}
```

---

## Pipeline Behaviors 规范

### 注册顺序（必须严格遵守）

```
Request → LoggingBehavior → ValidationBehavior → TransactionBehavior → DomainEventDispatchBehavior → Handler → Response
```

| Behavior | 作用于 | 说明 |
|---|---|---|
| `LoggingBehavior` | Command + Query | 记录请求日志，使用静态日志模板（CA2254） |
| `ValidationBehavior` | Command + Query | FluentValidation 校验，失败必须抛出 `ValidationException` |
| `TransactionBehavior` | **仅 Command** | 数据库事务包裹 |
| `DomainEventDispatchBehavior` | **仅 Command** | Handler 执行后、事务提交前统一分发领域事件 |

### 验证器规范（FluentValidation）

```csharp
public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("客户 ID 不能为空。");
        RuleFor(x => x.Items).NotEmpty().WithMessage("订单至少包含一个商品。");
    }
}
```

---

## Result 模式

- **所有 Handler 返回 `Result<T>`** 而非直接抛出业务异常
- 业务错误通过 `Result.Failure(error)` 传达
- 基础设施异常（网络、DB）仍然抛出，由全局异常中间件处理

```csharp
// Domain/Errors/OrderErrors.cs
public static class OrderErrors
{
    public static Error NotFound(Guid id) =>
        new("Order.NotFound", $"订单 {id} 不存在。");

    public static readonly Error AlreadyCancelled =
        new("Order.AlreadyCancelled", "订单已取消，无法重复操作。");
}
```

---

## Controller 接入规范

- Controller 只注入 `ISender`（不注入仓储、应用服务、领域服务）
- 通过 `sender.Send(command/query, cancellationToken)` 分发请求
- 根据 `Result.IsSuccess` 判断返回状态码

```csharp
[ApiController]
[Route("api/orders")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(request.CustomerId, request.Items);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetOrder), new { id = result.Value }, result.Value)
            : BadRequest(result.Error);
    }
}
```
