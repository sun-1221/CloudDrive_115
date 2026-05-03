# 需求文档：.NET 10 原生框架搭建（替代 ABP）

## 简介

本需求文档基于已批准的设计文档，定义 .NET 10 原生技术栈替代 ABP 框架的基础架构搭建需求。涵盖 DDD 分层架构、CQRS 分发、MediatR Pipeline、领域事件、SqlSugar 仓储、Quartz.NET 调度、缓存封装、输入验证、HTTP 韧性、全局异常处理、手动映射及依赖注入等模块。

本文档不涉及任何具体业务逻辑，仅定义框架层面的技术需求与验收标准。

## 术语表

- **Domain_Layer**：领域层，包含实体、值对象、领域事件、仓储接口和领域服务
- **Application_Layer**：应用层，包含 Command/Query Handler、Pipeline Behavior、DTO、验证器和事件处理器
- **Infrastructure_Layer**：基础设施层，包含仓储实现、缓存封装、HTTP 客户端配置、Quartz 调度和服务注册
- **WebApi_Layer**：表现层，包含 API 控制器、中间件和应用入口
- **CQRS**：Command Query Responsibility Segregation，命令查询职责分离模式
- **DI**：Dependency Injection，依赖注入
- **DTO**：Data Transfer Object，应用层与 WebApi 层之间传递数据的对象
- **ORM**：Object Relational Mapping，对象关系映射工具，本 spec 使用 SqlSugar
- **Result**：`Result` / `Result<T>` 业务结果模型，表示成功或失败；失败时携带 `Error`
- **MediatR_Mediator**：MediatR 中介者，负责 Command/Query 分发和领域事件发布
- **Pipeline_Behavior**：MediatR 管道行为，在 Handler 执行前后拦截请求
- **ValidationBehavior**：验证管道行为，自动执行 FluentValidation 校验
- **TransactionBehavior**：事务管道行为，为 Command 开启 SqlSugar 事务
- **LoggingBehavior**：日志管道行为，记录请求/响应日志
- **Repository**：仓储基类，封装 SqlSugar CRUD 操作
- **Domain_Event**：领域事件，表示领域内已发生的事实；Domain_Layer 中保持纯领域抽象，由 Application_Layer 适配为 MediatR 通知后发布
- **Exception_Middleware**：全局异常处理中间件，将异常映射为 ProblemDetails
- **Cache_Extensions**：IDistributedCache 的 JSON 序列化/反序列化封装
- **Polly_Policy**：Polly 韧性策略，包含重试、超时和熔断
- **Quartz_Job**：Quartz.NET 后台任务，实现 `IJob` 接口，由 `IScheduler` 根据 Cron 或一次性触发器调度

## 需求

### 需求 1：DDD 四层架构分层与依赖规则

**用户故事：** 作为开发者，我希望项目按照 DDD 四层架构组织，以便职责清晰分离、各层独立演进。

#### 验收标准

1. THE Domain_Layer SHALL 仅包含实体、值对象、领域事件、仓储接口和领域服务，不依赖任何外层项目
2. THE Application_Layer SHALL 仅依赖 Domain_Layer，包含 Command/Query Handler、Pipeline Behavior、DTO、验证器和事件处理器
3. THE Infrastructure_Layer SHALL 依赖 Domain_Layer 和 Application_Layer，实现 Domain_Layer 定义的仓储接口以及 Application_Layer 定义的基础设施抽象接口（如 ITransactionManager、IDomainEventSource），包含 SqlSugar 配置、缓存封装、HTTP 客户端配置、Quartz 调度和服务注册
4. THE WebApi_Layer SHALL 依赖 Application_Layer 和 Infrastructure_Layer，包含 API 控制器、中间件和应用入口
5. THE Domain_Layer SHALL 不包含对 Application_Layer、Infrastructure_Layer 或 WebApi_Layer 的项目引用

### 需求 2：CQRS Command/Query 分发

**用户故事：** 作为开发者，我希望通过 MediatR 实现 Command/Query 分离，以便读写操作职责明确、可独立扩展。

#### 验收标准

1. WHEN 定义写操作时，THE Application_Layer SHALL 提供 `ICommand` / `ICommand<TResponse>` 基接口或等效约定，底层实现 `IRequest<Result>` / `IRequest<Result<TResponse>>`
2. WHEN 定义读操作时，THE Application_Layer SHALL 提供 `IQuery<TResponse>` 基接口或等效约定，底层实现 `IRequest<Result<TResponse>>`
3. WHEN Controller 收到 HTTP 请求时，THE WebApi_Layer SHALL 构造对应的 Command 或 Query 对象并通过 `ISender.Send()` 或 `IMediator.Send()` 分发
4. WHEN Command Handler 执行时，THE Application_Layer SHALL 允许调用领域服务、仓储并触发领域事件
5. WHEN Query Handler 执行时，THE Application_Layer SHALL 仅执行数据查询，不触发任何副作用

### 需求 3：MediatR Pipeline Behaviors

**用户故事：** 作为开发者，我希望通过 MediatR Pipeline Behavior 实现横切关注点，以便验证、事务和日志逻辑与业务 Handler 解耦。

#### 验收标准

1. THE Application_Layer SHALL 提供 LoggingBehavior，记录每个请求的类型、参数和响应耗时
2. THE Application_Layer SHALL 提供 ValidationBehavior，在 Handler 执行前自动执行所有已注册的 FluentValidation 验证器
3. WHEN FluentValidation 校验失败时，THE ValidationBehavior SHALL 抛出 `ValidationException`，阻止 Handler 执行
4. THE Application_Layer SHALL 提供 TransactionBehavior，为 Command 请求开启 SqlSugar 事务
5. WHEN 请求为 Query 类型时，THE TransactionBehavior SHALL 跳过事务管理，直接执行后续管道
6. WHEN Command Handler 执行成功时，THE TransactionBehavior SHALL 提交事务
7. IF Command Handler 执行过程中抛出异常，THEN THE TransactionBehavior SHALL 回滚事务并重新抛出异常
8. THE Infrastructure_Layer SHALL 按 LoggingBehavior → ValidationBehavior → TransactionBehavior → DomainEventDispatchBehavior 的顺序注册 Pipeline Behavior
9. THE Application_Layer SHALL 提供 `IDomainEventSource` 或等效抽象，用于收集本次请求中被持久化的聚合，以供 `DomainEventDispatchBehavior` 在事务边界内统一分发领域事件

### 需求 4：领域事件机制

**用户故事：** 作为开发者，我希望通过领域事件实现聚合间解耦通信，以便跨聚合副作用（如缓存失效、通知）不侵入核心业务逻辑。

#### 验收标准

1. THE Domain_Layer SHALL 提供不依赖 MediatR 或其他外部框架的领域事件基类
2. THE Domain_Layer SHALL 在实体基类中提供 `AddDomainEvent()` 方法用于收集领域事件
3. WHEN Command Handler 完成持久化后，THE DomainEventDispatchBehavior SHALL 自动遍历被追踪实体收集的领域事件，将其包装为 MediatR 通知后通过 `IMediator.Publish()` 或 `IPublisher.Publish()` 发布，并清空事件集合
4. THE Application_Layer SHALL 提供 MediatR 通知包装类型和 `INotificationHandler<TNotification>` 实现来处理领域事件
5. THE Domain_Event SHALL 在同一事务内同步处理
6. WHEN 普通 Command 通过 MediatR Pipeline 执行时，THE Handler SHALL NOT 手动调用 `IMediator.Publish()` 发布实体领域事件，而应由 `DomainEventDispatchBehavior` 统一处理

### 需求 5：SqlSugar 仓储基础设施

**用户故事：** 作为开发者，我希望使用 SqlSugar CodeFirst 作为 ORM 并提供仓储基类，以便快速实现数据持久化且自动管理数据库表结构。

#### 验收标准

1. THE Infrastructure_Layer SHALL 将 `ISqlSugarClient` 注册为 Scoped 生命周期，每个请求一个实例
2. THE Infrastructure_Layer SHALL 配置 SqlSugar 的 `IsAutoCloseConnection = true` 以自动管理连接池
3. THE Infrastructure_Layer SHALL 配置 SqlSugar 的 `InitKeyType = Attribute` 以通过实体特性标注主键与字段
4. WHEN 应用启动时，THE Infrastructure_Layer SHALL 调用 `db.CodeFirst.InitTables<T>()` 自动创建或更新数据库表
5. THE Infrastructure_Layer SHALL 提供泛型仓储基类 `Repository<TEntity, TId>`，封装 GetById、GetList、Insert、Update、Delete 等常用 CRUD 操作
6. THE Domain_Layer SHALL 定义仓储接口 `IRepository<TEntity, TId>`，由 Infrastructure_Layer 的 `Repository<TEntity, TId>` 实现
7. WHEN Repository 执行 Insert/Update 等持久化操作后，THE Infrastructure_Layer SHALL 调用 `IDomainEventSource.Track(entity)` 或等效方法追踪聚合，以便后续 `DomainEventDispatchBehavior` 分发其领域事件

### 需求 6：Quartz.NET 后台任务调度

**用户故事：** 作为开发者，我希望使用 Quartz.NET 提供后台任务调度基础，以便业务模块可以方便地注册和管理定时任务。

#### 验收标准

1. THE Infrastructure_Layer SHALL 通过 `AddQuartz()` 和 `AddQuartzHostedService()` 注册 Quartz.NET 服务
2. THE Infrastructure_Layer SHALL 使用 Quartz.Extensions.DependencyInjection 提供的 Microsoft DI JobFactory 或等效机制，确保每个 Job 可通过 DI 在独立 Scope 中解析依赖
3. WHEN Job 执行时，THE Infrastructure_Layer SHALL 通过 `JobDataMap` 传递 `JobId`，由 Job 从数据库加载业务载荷
4. THE Infrastructure_Layer SHALL 首期使用内存 JobStore 存储调度信息
5. THE Infrastructure_Layer SHALL 约定 `JobDataMap` 仅携带最小调度标识（如 `JobId`），具体业务载荷与恢复检查点应由 Job 通过持久化存储加载
6. WHEN Quartz Job 执行时，THE Job SHALL NOT 假设 MediatR Pipeline Behavior 已自动生效；若需要事务、领域事件发布或恢复检查点处理，THEN THE Job SHALL 显式处理

### 需求 7：IDistributedCache 缓存封装

**用户故事：** 作为开发者，我希望使用 IDistributedCache 的 JSON 封装，以便以强类型方式存取缓存数据。

#### 验收标准

1. THE Infrastructure_Layer SHALL 提供 `DistributedCacheExtensions` 静态扩展类，封装 `GetAsync<T>()` 和 `SetAsync<T>()` 方法进行 JSON 序列化/反序列化
2. THE Infrastructure_Layer SHALL 提供 `GetOrSetAsync<T>()` 方法，缓存命中时直接返回，未命中时调用 factory 委托获取数据并写入缓存
3. THE Infrastructure_Layer SHALL 支持配置默认缓存 TTL
4. WHEN 开发环境时，THE Infrastructure_Layer SHALL 使用 `AddDistributedMemoryCache()` 注册内存缓存
5. THE Cache_Extensions SHALL 使用 `System.Text.Json` 进行 JSON 序列化和反序列化

### 需求 8：FluentValidation 输入验证

**用户故事：** 作为开发者，我希望使用 FluentValidation 进行输入验证并与 MediatR Pipeline 集成，以便验证逻辑与业务逻辑解耦且自动执行。

#### 验收标准

1. THE Infrastructure_Layer SHALL 通过 `AddValidatorsFromAssembly()` 自动扫描并注册 Application_Layer 中的所有验证器
2. WHEN MediatR 请求存在对应的 FluentValidation 验证器时，THE ValidationBehavior SHALL 在 Handler 执行前自动执行校验
3. WHEN 校验失败时，THE ValidationBehavior SHALL 抛出包含所有验证错误的 `ValidationException`
4. WHEN 请求没有对应的验证器时，THE ValidationBehavior SHALL 直接放行，执行后续管道

### 需求 9：IHttpClientFactory + Polly HTTP 韧性

**用户故事：** 作为开发者，我希望使用 IHttpClientFactory 和 Polly 提供 HTTP 客户端韧性基础，以便外部 API 调用具备重试、超时和熔断能力。

#### 验收标准

1. THE Infrastructure_Layer SHALL 通过 Named HttpClient 注册不同用途的 HTTP 客户端
2. WHEN 配置读请求（GET）的 Polly 策略时，THE Infrastructure_Layer SHALL 包含重试策略（瞬时错误重试 3 次 + 指数退避 1s/2s/4s）、超时策略（默认 10 秒，可通过配置调整）与熔断策略（连续失败 5 次后熔断 30 秒）
3. WHEN 配置写请求（POST/PUT/DELETE）的 Polly 策略时，THE Infrastructure_Layer SHALL 仅包含超时策略（默认 10 秒，可通过配置调整）与熔断策略（连续失败 5 次后熔断 30 秒），不包含自动重试
4. THE Infrastructure_Layer SHALL 通过 `IHttpClientFactory` 管理 HTTP 连接池，避免 Socket 耗尽
5. THE Infrastructure_Layer SHALL 支持业务模块按端点契约复用 query string、`application/x-www-form-urlencoded`、`multipart/form-data` 与 JSON 四类请求构造能力

### 需求 10：全局异常处理中间件 + ProblemDetails

**用户故事：** 作为开发者，我希望通过全局异常处理中间件统一错误响应格式，以便所有 API 错误返回标准的 ProblemDetails JSON。

#### 验收标准

1. THE Exception_Middleware SHALL 捕获所有未处理异常并返回 `application/problem+json` 格式的响应
2. WHEN 捕获到 `ValidationException` 时，THE Exception_Middleware SHALL 映射为 HTTP 400 并在 ProblemDetails 的 errors 字段中包含字段级验证错误详情
3. WHEN 捕获到业务异常时，THE Exception_Middleware SHALL 根据异常类型映射为对应的 HTTP 状态码（401/404/429/502/503 等）
4. WHEN 捕获到未知异常时，THE Exception_Middleware SHALL 映射为 HTTP 500 并隐藏内部异常详情
5. THE Exception_Middleware SHALL 对所有异常记录日志，包含请求路径、异常类型和异常消息
6. WHEN Application 返回 `Result.Failure(...)` 或等效失败结果时，THE WebApi_Layer SHALL 将其映射为非 2xx 的 `ProblemDetails` 响应，且 SHALL NOT 以 HTTP 200 表示失败

### 需求 11：手动对象映射

**用户故事：** 作为开发者，我希望使用手动映射扩展方法替代 AutoMapper，以便对象映射在编译期可检查且无反射开销。

#### 验收标准

1. THE Application_Layer SHALL 使用静态扩展方法 `ToDto()` 和 `ToEntity()` 进行 Entity 与 DTO 之间的映射
2. THE Application_Layer SHALL 确保映射方法在编译期可检查类型安全，不使用反射
3. WHEN 业务模块需要对象映射时，THE Application_Layer SHALL 在对应模块中定义各自的映射扩展方法

### 需求 12：依赖注入服务注册

**用户故事：** 作为开发者，我希望通过 `IServiceCollection` 扩展方法集中管理服务注册，以便替代 ABP 模块系统并保持注册逻辑清晰有序。

#### 验收标准

1. THE Infrastructure_Layer SHALL 提供 `IServiceCollection` 扩展方法，按模块组织服务注册（如 `AddSqlSugar()`、`AddQuartzJobs()`、`AddCaching()` 等）
2. THE WebApi_Layer SHALL 在 `Program.cs` 中通过调用扩展方法完成所有服务注册
3. THE Infrastructure_Layer SHALL 将仓储实现注册为 Scoped 生命周期
4. THE Infrastructure_Layer SHALL 将 MediatR 及其 Pipeline Behavior 注册到 DI 容器
5. WHEN 新增业务模块时，THE Infrastructure_Layer SHALL 支持通过新增扩展方法的方式注册该模块的服务，无需修改现有注册代码
6. WHEN API 需要基于当前用户执行受保护操作时，THE WebApi_Layer SHALL 支持通过 `AddJwtAuthentication()` 等扩展方法注册认证，并在请求管道中启用 `UseAuthentication()` 与 `UseAuthorization()`
7. THE Infrastructure_Layer SHALL 支持业务模块注册机密保护能力（如基于 ASP.NET Core Data Protection 的 Protect/Unprotect），并确保 key ring 可持久化以支持服务重启和多实例部署
