---
inclusion: fileMatch
fileMatchPattern: "**/WebApi/**/*.cs"
---

# WebApi 层编码规范

WebApi 层是系统的入口，负责接收 HTTP 请求、分发到 Application 层、返回标准化响应。

---

## 层次定位

- 依赖 Application 层（发送 Command/Query）和 Infrastructure 层（DI 注册）
- 是唯一直接面向外部客户端的层
- **不包含业务逻辑**，仅负责 HTTP 协议适配和请求分发

---

## Controller 规范

### 依赖注入

- Controller **只注入 `ISender`**（MediatR），通过 `sender.Send()` 分发请求
- **严禁**注入仓储（`IRepository`）、ORM 客户端（`ISqlSugarClient`）、领域服务或应用服务（CD0003）
- 使用 Primary Constructor 简化注入

```csharp
[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController(ISender sender) : ControllerBase
{
}
```

### 请求处理

- Controller Action 只做三件事：构造 Command/Query → 发送 → 映射 HTTP 响应
- 输入验证由 FluentValidation + `ValidationBehavior` Pipeline 处理，Controller **不做验证**
- 所有 Action **必须**接受 `CancellationToken` 参数
- 根据 `Result.IsSuccess` 判断返回状态码

```csharp
[HttpPost]
public async Task<IActionResult> CreateAsync(
    [FromBody] CreateOrderRequest request,
    CancellationToken cancellationToken)
{
    var command = new CreateOrderCommand(request.CustomerId, request.Items);
    Result<Guid> result = await sender.Send(command, cancellationToken);

    return result.IsSuccess
        ? CreatedAtAction(nameof(GetByIdAsync), new { id = result.Value }, result.Value)
        : BadRequest(result.Error);
}
```

### 命名规范

- Controller 类名：`{资源名复数}Controller`（`OrdersController`、`UsersController`）
- Action 方法名：`{动词}Async`（`CreateAsync`、`GetByIdAsync`、`DeleteAsync`）
- 路由：`api/[controller]`，使用 kebab-case 资源名

---

## 映射规范

- 请求 DTO → Command/Query：在 Controller Action 内直接构造
- 领域实体 → 响应 DTO：使用 `ToDto()` 扩展方法（CD0008）
- **严禁**使用 AutoMapper（CD0004）
- 映射扩展方法定义在 Application 层的 `Mappings/` 目录

---

## 全局异常处理

- 使用自定义 `ExceptionHandlingMiddleware` 统一捕获所有未处理异常
- 异常中间件**必须最先注册**（在所有其他中间件之前）
- 所有错误响应统一输出 `ProblemDetails`（RFC 7807 / RFC 9457）
- Content-Type 为 `application/problem+json`

### 异常映射规则

| 异常类型 | HTTP 状态码 | 说明 |
|---|---|---|
| `ValidationException` | 400 Bad Request | FluentValidation 校验失败，附带 `errors` 字典 |
| `UnauthorizedAccessException` | 401 Unauthorized | 未认证 |
| `KeyNotFoundException` | 404 Not Found | 资源不存在 |
| `FileNotFoundException` | 404 Not Found | 资源不存在 |
| `HttpRequestException` | 502/503 | 外部服务通信失败 |
| 其他未处理异常 | 500 Internal Server Error | 兜底，**不暴露内部异常信息** |

### 安全原则

- 生产环境**严禁**在响应中暴露异常堆栈、内部错误消息或数据库信息
- 异常详情只写入日志，不返回给客户端
- 日志使用静态模板（CA2254），包含请求路径和异常类型

---

## 中间件注册顺序

```csharp
app.UseExceptionHandling();    // 1. 全局异常处理（最先）
// app.UseAuthentication();    // 2. 认证
// app.UseAuthorization();     // 3. 授权
app.MapControllers();          // 4. 路由映射（最后）
```

---

## Program.cs 规范

- 使用 Minimal Hosting Model（顶级语句）
- 服务注册通过 Infrastructure 层的 `AddInfrastructure()` 总入口方法
- WebApi 层**不直接注册**仓储、ORM 或其他基础设施服务
- Swagger 仅在开发环境启用

---

## 禁止事项

- ❌ Controller 不能注入仓储、ORM 客户端或领域服务（CD0003）
- ❌ Controller 不能包含业务逻辑
- ❌ Controller 不能直接操作数据库
- ❌ 不能使用 AutoMapper（CD0004）
- ❌ 不能在生产环境响应中暴露异常堆栈或内部错误信息
- ❌ 不能在 WebApi 层定义领域实体、值对象或仓储接口
