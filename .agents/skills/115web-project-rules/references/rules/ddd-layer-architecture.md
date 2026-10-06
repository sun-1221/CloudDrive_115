<!-- Generated from .windsurf/rules/ddd-layer-architecture.md by scripts/sync-windsurf-rules.ps1. -->

# DDD 四层架构边界规范

本项目采用 DDD 四层架构：`Domain` → `Application` → `Infrastructure` + `WebApi`。

## 层次依赖规则

```
Domain          （无外部依赖）
    ↑
Application     （仅依赖 Domain）
    ↑
Infrastructure  （实现 Domain 定义的接口）
    ↑
WebApi          （依赖 Application + Infrastructure）
```

---

## 各层职责与禁止引用

### Domain 层（纯净领域，零外部依赖）

**包含**：实体、值对象、聚合根、领域事件、仓储接口、领域服务、领域异常

**严禁引用**：
- `SqlSugar` / `ISqlSugarClient`
- `Microsoft.AspNetCore.*`
- `FluentValidation`
- `Quartz`
- `StackExchange.Redis` / `IConnectionMultiplexer`
- `System.Net.Http.HttpClient` / `IHttpClientFactory`
- `IDistributedCache`

---

### Application 层（用例编排，依赖 Domain 接口）

**包含**：Command、Query、Handler、Pipeline Behavior、Validator、DTO、应用服务接口

**严禁直接引用**（必须通过 Domain 接口或抽象）：
- `ISqlSugarClient`（ORM 直连）
- `IDistributedCache`（必须通过封装接口）
- `IHttpClientFactory` / `HttpClient`
- `IConnectionMultiplexer`

---

### Infrastructure 层（基础设施实现）

**包含**：仓储实现（SqlSugar）、缓存封装实现、HTTP 客户端、Quartz Job 注册、外部服务集成、依赖注入、持久化

- `SqlSugar` 注册为 `Scoped`
- `Quartz.NET` 通过 `AddQuartz()` + `AddQuartzHostedService()` 注册
- `IDistributedCache` 需封装为 `ICloudDriveMetadataCache` 等抽象后，再提供给 Application 层使用

---

### WebApi 层（接口层）

**包含**：Controller、Middleware、扩展方法、启动配置

- Controller 只能通过 `ISender` / `IMediator.Send()` 进入应用层
- **严禁** Controller 直接注入仓储、ORM 客户端或领域服务
- 全局异常中间件统一输出 `ProblemDetails`，必须兜底捕获所有未处理异常
- 映射使用 `ToDto()` / `ToEntity()` 扩展方法，**严禁**使用 AutoMapper

---

## 自动化约束对照（CD 规则）

| 规则 | 约束内容 |
|---|---|
| `CD0001` | Domain 层禁止引用基础设施类型 |
| `CD0002` | Application 层禁止直接依赖 SqlSugar、缓存和 HTTP 客户端实现 |
| `CD0003` | Controller 构造函数禁止注入仓储或 ORM 客户端 |
| `CD0004` | 全项目禁止引用 AutoMapper（`using AutoMapper;`、`IMapper`、包引用） |
| `CD0005` | Command/Query/Handler 必须遵循命名规范 |
| `CD0006` | 返回 `Task`/`ValueTask` 的方法必须带 `Async` 后缀 |
| `CD0007` | `*QueryHandler` 中禁止 Insert/Update/Delete 调用和 `IMediator.Publish()` |
| `CD0008` | 映射扩展方法统一命名为 `ToDto()` / `ToEntity()` |
| `CD0009` | `IServiceCollection` 扩展方法必须以 `Add` 开头 |

---

## 项目目录结构

```
src/
├── Domain/
│   ├── Entities/
│   ├── Enums/
│   ├── Events/
│   ├── Exceptions/
│   ├── Repositories/
│   ├── Results/
│   └── Validators/
├── Application/
│   ├── Abstractions/
│   ├── Behaviors/
│   ├── CloudDrive/
│   ├── Commands/
│   ├── Mappings/
│   └── Queries/
├── Infrastructure/
│   ├── Caching/
│   ├── CloudDrive/
│   ├── DependencyInjection/
│   ├── Http/
│   ├── Jobs/
│   └── Persistence/
└── WebApi/
    ├── Controllers/
    ├── Extensions/
    └── Middleware/

tests/
├── Application.Tests/
├── Domain.Tests/
├── Infrastructure.Tests/
└── WebApi.Tests/
```
