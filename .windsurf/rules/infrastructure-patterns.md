---
trigger: glob
globs: "**/Infrastructure/**/*.cs"
---

# Infrastructure 层编码规范

Infrastructure 层负责实现 Domain 和 Application 层定义的接口，提供持久化、缓存、HTTP 通信、定时任务等基础设施能力。

---

## 层次定位

- 依赖 Domain 层（实现仓储接口、领域抽象）和 Application 层（注册 Pipeline Behavior、Validator）
- **严禁**被 Domain 层或 Application 层反向引用
- 所有实现类使用 `internal sealed` 修饰（仅通过接口暴露给上层）
- 对外仅暴露 `IServiceCollection` 扩展方法用于 DI 注册

---

## 持久化（SqlSugar）

### 注册规范

- `ISqlSugarClient` 注册为 **Scoped** 生命周期（每次请求一个实例）
- 连接字符串从 `IConfiguration.GetConnectionString("Default")` 读取
- 启用 `IsAutoCloseConnection = true`

### 仓储实现

- 泛型仓储 `Repository<TEntity, TId>` 实现 `IRepository<TEntity, TId>`
- 仓储注册为 **Scoped**
- 仓储内通过构造函数注入 `ISqlSugarClient`
- 仓储只负责 CRUD 操作，**不包含业务逻辑**
- 复杂查询（报表/列表）如需绕过聚合仓储，应由 Infrastructure 提供查询实现或应用服务依赖的抽象接口，Application 层不得直接依赖 `ISqlSugarClient`

### 事务管理

- `ITransactionManager` 接口定义在 Application 层，实现 `SqlSugarTransactionManager` 在 Infrastructure 层
- 事务由 `TransactionBehavior` Pipeline 统一管理，**仓储内不自行开启事务**
- 注册为 **Scoped**

### 领域事件追踪

- `IDomainEventSource` 接口定义在 Application 层，实现 `SqlSugarDomainEventSource` 在 Infrastructure 层
- 追踪包含领域事件的实体，由 `DomainEventDispatchBehavior` 在事务提交前统一分发
- 注册为 **Scoped**

---

## 缓存

- Infrastructure 层使用 `IDistributedCache` 抽象，开发环境可注册 `AddDistributedMemoryCache()`
- 将缓存能力封装为 `ICloudDriveMetadataCache` 等 Application 抽象，再由 Infrastructure 提供实现
- `ICloudDriveMetadataCache` 默认注册为 `Singleton`，统一复用跨请求缓存键索引并与 `IDistributedCache` 交互
- 单实例部署可使用进程内缓存键索引；多实例部署时需补充分布式失效广播、集中式索引，或明确接受本地索引最终一致性边界
- Application 层**不直接依赖** `IDistributedCache`、`StackExchange.Redis` 或 `IConnectionMultiplexer`

---

## 频控与互斥

- `ICloudDriveRateLimiter` 作为跨请求共享状态的限流器，默认注册为 `Singleton`
- `ICloudDriveTokenRefreshLockProvider` 默认注册为 `Singleton`；单实例部署可使用进程内 `SemaphoreSlim` 实现
- 多实例部署时，限流器与刷新锁应替换为分布式实现，或在部署约束中明确声明仅单实例有效

---

## HTTP 客户端

- 通过 `IHttpClientFactory` + Named Client 模式注册
- 读请求客户端（`ReadClient`）：重试（指数退避）+ 超时 + 熔断
- 写请求客户端（`WriteClient`）：超时 + 熔断，**不配置重试**（防止重复写入）
- 韧性策略使用 Polly 配置
- Application 层通过 `ICloudDriveOpenApiGateway` 等抽象访问外部 API，`IHttpClientFactory` 与 `HttpClient` 仅在 Infrastructure 层使用

---

## 定时任务（Quartz.NET）

- 通过 `AddQuartz()` + `AddQuartzHostedService()` 注册
- 使用 DI JobFactory（`UseMicrosoftDependencyInjectionJobFactory()`）
- `WaitForJobsToComplete = true`（优雅关闭）
- Job 类定义在 Infrastructure 层，实现 `IJob` 接口
- Job 内通过构造函数注入依赖，**不使用服务定位器**

---

## DI 注册规范

- 所有注册通过 `IServiceCollection` 扩展方法，方法名**必须以 `Add` 开头**（CD0009）
- 每个子模块独立扩展方法：`AddSqlSugar`、`AddCaching`、`AddHttpClients`、`AddQuartzJobs`、`AddFluentValidation`
- 总入口方法 `AddInfrastructure` 调用所有子模块
- Pipeline Behavior 注册顺序**必须严格遵守**：

```
LoggingBehavior → [可选业务前置 Behavior] → ValidationBehavior → TransactionBehavior → DomainEventDispatchBehavior
```

- 默认 Application 注册只包含四段通用 Pipeline Behavior
- 业务前置 Behavior 必须由业务模块总入口显式传入，不得硬编码进通用 `AddApplicationServices`
- 业务前置 Behavior 必须通过标记接口跳过不相关请求，不得执行写操作或发布领域事件

---

## 外部服务集成

- 外部服务客户端定义接口在 Application 层（如 `ICloudDriveOpenApiGateway`）
- 实现类在 Infrastructure 层，使用 `internal sealed` 修饰
- 通过 Named HttpClient + Polly 策略调用外部 API
- 外部服务的 DTO/响应模型定义在 Infrastructure 层（不污染 Domain/Application）

---

## 禁止事项

- ❌ Infrastructure 层的实现类不能被 Domain 或 Application 层直接引用
- ❌ 仓储内不能包含业务逻辑或发布领域事件
- ❌ 不能在仓储内自行管理事务（由 Pipeline Behavior 统一管理）
- ❌ 不能使用服务定位器模式（`IServiceProvider.GetService<T>()`）
- ❌ 不能在 Infrastructure 层定义 Command/Query/Handler
