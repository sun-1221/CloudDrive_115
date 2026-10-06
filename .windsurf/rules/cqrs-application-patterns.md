---
trigger: glob
globs: "**/Application/**/*.cs"
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

- 命令类名：`{动词}{名词}Command`（`DisconnectCommand`、`CreatePkceAuthorizationCommand`）
- 命令处理器：`{命令名}Handler`（`DisconnectCommandHandler`）
- 文件夹：优先按业务模块组织，如 `Application/CloudDrive/Auth/`、`Application/CloudDrive/Files/`、`Application/CloudDrive/Jobs/`

### 定义规则

- 命令对象使用 `sealed record`（不可变性）
- Handler 类使用 `internal sealed` 修饰
- Handler 内**不应包含复杂的业务验证**，验证通过 FluentValidation + Pipeline Behavior 处理
- Handler 内**不应直接调用其他 Handler**，通过领域事件或 Outbox 解耦
- 业务错误通过 `Result<T>` 返回，基础设施异常仍然抛出

```csharp
// Application/CloudDrive/Auth/DisconnectCommand.cs
public sealed record DisconnectCommand(Guid UserId, CloudDriveAccountScopedInput Input) : ICommand;

// Application/CloudDrive/Auth/DisconnectCommandHandler.cs
internal sealed class DisconnectCommandHandler(
    CloudDriveTokenManager tokenManager,
    IOptions<CloudDriveAppConfig> configOptions,
    ILogger<DisconnectCommandHandler> logger) : ICommandHandler<DisconnectCommand>
{
    public async Task<Result> Handle(
        DisconnectCommand request,
        CancellationToken cancellationToken)
    {
        CloudDriveAppConfig config = configOptions.Value;

        if (string.IsNullOrEmpty(request.Input.CloudAccountUid))
        {
            return Result.Failure(
                new Error("CloudDrive.MissingCloudAccountUid", "缺少 CloudAccountUid，请指定要断开的 115 账号。"));
        }

        await tokenManager.RemoveTokenAsync(
            request.UserId,
            config.AppId,
            request.Input.CloudAccountUid,
            cancellationToken);

        logger.LogInformation(
            "已断开 115 网盘授权 UserId={UserId}, CloudAccountUid={CloudAccountUid}",
            request.UserId, request.Input.CloudAccountUid);

        return Result.Success();
    }
}
```

---

## Query 规范

### 命名规则

- 查询类名：`Get{名词}{修饰}Query`（`GetUserInfoQuery`、`GetFileListQuery`）
- 查询处理器：`{查询名}Handler`（`GetUserInfoQueryHandler`）
- 文件夹：优先按业务模块组织，如 `Application/CloudDrive/Auth/`、`Application/CloudDrive/Files/`、`Application/CloudDrive/Preview/`

### 约束

- Query Handler **不允许副作用**：严禁 Insert/Update/Delete 操作
- Query Handler **不允许发布领域事件**（`IMediator.Publish()`/`IPublisher.Publish()`）
- Query Handler 只依赖 Domain/Application 抽象（仓储、应用层服务、`ICloudDriveOpenApiGateway`、`ICloudDriveMetadataCache` 等）
- 如需复杂投影或外部查询，由 Infrastructure 提供实现，Query Handler 仅依赖抽象接口或应用层服务

```csharp
public sealed record GetUserInfoQuery(Guid UserId, CloudDriveAccountScopedInput Input)
    : IQuery<CloudDriveUserInfoDto>;

internal sealed class GetUserInfoQueryHandler(
    ICloudDriveOpenApiGateway openApiGateway,
    IOptions<CloudDriveAppConfig> configOptions) : IQueryHandler<GetUserInfoQuery, CloudDriveUserInfoDto>
{
    public async Task<Result<CloudDriveUserInfoDto>> Handle(
        GetUserInfoQuery request,
        CancellationToken cancellationToken)
    {
        CloudDriveAppConfig config = configOptions.Value;

        UserInfoResponse userInfo = await openApiGateway.SendApiRequestAsync<UserInfoResponse>(
            request.UserId,
            config.AppId,
            request.Input.CloudAccountUid,
            HttpMethod.Get,
            "open/user/info",
            cancellationToken: cancellationToken);

        return Result.Success(new CloudDriveUserInfoDto
        {
            CloudAccountUid = request.Input.CloudAccountUid,
            Nickname = userInfo.UserName ?? string.Empty,
            AvatarUrl = userInfo.AvatarUrl,
        });
    }
}
```

---

## Pipeline Behaviors 规范

### 注册顺序（必须严格遵守）

```
Request → LoggingBehavior → [可选 PreValidationBehavior] → ValidationBehavior → TransactionBehavior → DomainEventDispatchBehavior → Handler → Response
```

| Behavior | 作用于 | 说明 |
|---|---|---|
| `LoggingBehavior` | Command + Query | 记录请求日志，使用静态日志模板（CA2254） |
| 可选 `PreValidationBehavior` | Command + Query | 业务模块显式插入的上下文解析行为，必须通过标记接口跳过不相关请求，不得执行写操作或发布领域事件 |
| `ValidationBehavior` | Command + Query | FluentValidation 校验，失败必须抛出 `ValidationException` |
| `TransactionBehavior` | **仅 Command** | 数据库事务包裹 |
| `DomainEventDispatchBehavior` | **仅 Command** | Handler 执行后、事务提交前统一分发领域事件 |

默认 Application 注册只包含 `LoggingBehavior → ValidationBehavior → TransactionBehavior → DomainEventDispatchBehavior`；
业务模块若需要账号上下文、租户上下文等验证前解析，必须在模块总入口中显式传入扩展 Behavior。

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
