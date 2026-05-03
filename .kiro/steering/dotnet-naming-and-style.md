---
inclusion: fileMatch
fileMatchPattern: "**/*.cs"
---

# .NET 命名与代码风格规范

## 命名约定

| 类型 | 规则 | 示例 |
|---|---|---|
| 类、接口、枚举、结构体 | PascalCase | `OrderService`, `IRepository` |
| 方法、属性、事件 | PascalCase | `GetByIdAsync`, `IsActive` |
| 本地变量、参数 | camelCase | `orderId`, `cancellationToken` |
| 私有字段 | `_camelCase`（下划线前缀） | `_repository`, `_logger` |
| 常量 | PascalCase | `MaxRetryCount`, `DefaultTimeout` |
| 泛型类型参数 | `T` 前缀 + PascalCase | `TEntity`, `TResult` |
| 接口 | `I` 前缀 + PascalCase | `IOrderRepository`, `IUnitOfWork` |
| 异步方法 | `Async` 后缀 | `GetOrderAsync`, `SaveChangesAsync` |

### 命名原则

- 使用完整英文单词，不使用缩写（`Id`、`Dto`、`Url`、`Http` 等广泛接受的缩写除外）
- 布尔类型使用 `Is`、`Has`、`Can`、`Should` 前缀（`IsActive`、`HasPermission`）
- 集合类型使用复数形式（`orders`、`Items`）
- 不使用匈牙利命名法（~~`strName`~~、~~`intCount`~~）

```csharp
// ❌ 错误
public class usr_svc { }
private string _strName;
public async Task GetData() { }

// ✅ 正确
public class UserService { }
private string _name;
public async Task<UserDto> GetUserAsync(Guid id, CancellationToken cancellationToken) { }
```

---

## 代码风格

### 文件结构

- 每个文件只定义一个顶级类型（极少数情况下允许嵌套私有类）
- 文件名与主要类型名一致（`OrderService.cs`）
- 使用**文件范围命名空间**（File-scoped namespace）

```csharp
// ✅ 推荐
namespace MyApp.Domain.Orders;

public class Order { }
```

### using 指令

- `using` 放在命名空间**外部**（文件顶部）
- 按顺序排列并以空行分隔：System → 第三方库 → 项目内部

```csharp
using System;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;
using MediatR;

using MyApp.Domain.Orders;
```

### 格式规范

- 缩进：4 个空格，不使用 Tab
- 大括号：Allman 风格（左右大括号各占一行）
- 每行不超过 120 个字符
- `if`/`for`/`while` 等语句**必须**使用大括号，即使只有一行

```csharp
// ✅ 正确
if (id == Guid.Empty)
{
    throw new ArgumentException("Id cannot be empty.", nameof(id));
}

// ❌ 错误
if (id == Guid.Empty) throw new ArgumentException("...");
```

### var 的使用

- 右侧类型明确时（`new`、显式转换）才使用 `var`
- 类型不明确时使用显式类型

```csharp
// ✅ 允许
var order = new Order();

// ❌ 不允许（类型不明确）
var result = GetSomething();

// ✅ 正确
OrderDto result = GetSomething();
```

### 语言关键字

使用语言关键字而非 CLR 类型名称：

```csharp
// ✅ 正确
string name;
int count;
bool isActive;

// ❌ 错误
String name;
Int32 count;
Boolean isActive;
```

---

## 异步编程

- I/O 密集型操作必须使用 `async/await`
- 所有异步方法**必须**带 `Async` 后缀
- 所有异步方法**必须**传递 `CancellationToken`
- 当前项目为 ASP.NET Core 应用，**不强制** `ConfigureAwait(false)`

```csharp
// ✅ 正确
public async Task<Order> GetOrderAsync(
    Guid id,
    CancellationToken cancellationToken = default)
{
    return await _repository.GetByIdAsync(id, cancellationToken);
}

// ❌ 错误：同步阻塞
public Order GetOrder(Guid id)
{
    return _repository.GetByIdAsync(id).Result;
}
```

---

## 异常处理

- 只捕获能处理的异常，不捕获 `Exception` 基类（全局异常中间件除外）
- 不使用空 `catch` 块吞掉异常
- 使用具体异常类型，附带有意义的消息

```csharp
// ✅ 正确
catch (DbException ex)
{
    _logger.LogError(ex, "Database error while getting order {OrderId}", id);
    throw new InfrastructureException("Failed to retrieve order.", ex);
}

// ❌ 错误
catch { }
catch (Exception ex) { _logger.LogError(ex, "error"); }
```

---

## 集合与 LINQ

- 返回集合时使用 `IReadOnlyList<T>` 或 `IReadOnlyCollection<T>`，而非具体实现
- 接收集合参数时使用 `IEnumerable<T>`

---

## 依赖注入

- 所有依赖通过**构造函数注入**，不使用属性注入或服务定位器
- 推荐使用 C# 12 Primary Constructor 简化注入代码
- 服务注册通过 `IServiceCollection` 扩展方法集中管理，扩展方法名以 `Add` 开头

```csharp
// ✅ 推荐：Primary Constructor
public sealed class OrderService(
    IOrderRepository repository,
    ILogger<OrderService> logger)
{ }

// ✅ 扩展方法注册
public static IServiceCollection AddOrderModule(this IServiceCollection services)
{
    services.AddScoped<IOrderRepository, OrderRepository>();
    return services;
}
```

### 生命周期原则

- `Singleton`：无状态服务、缓存、配置；`SqlSugar` 使用 `Scoped`
- `Scoped`：数据库上下文、仓储、单次请求内的工作单元
- `Transient`：轻量无状态工具类
