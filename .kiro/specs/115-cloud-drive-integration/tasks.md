# 实现计划：115 网盘集成

## 概述

基于 .NET 10 原生技术栈（MediatR CQRS + SqlSugar + Quartz.NET + FluentValidation）和 DDD 四层架构，从底层基础设施到上层表现层逐步实现 115 网盘开放平台集成。每个任务构建在前序任务之上，确保增量可验证。实现语言为 C#/.NET。

> **前置依赖：** 本任务列表假设 `115-cloud-drive-net10` 定义的框架基础设施（MediatR Pipeline Behavior、SqlSugar 仓储基类、Quartz.NET 调度、IDistributedCache JSON 封装、FluentValidation 集成、全局异常中间件 + ProblemDetails）已完成搭建。

## 任务列表

- [x] 1. 领域层 - 核心实体、枚举、异常与配置定义
  - [x] 1.1 创建领域层枚举
    - 创建 `CloudDriveJobType` 枚举（Upload, BatchCopy, BatchMove, BatchDelete, BatchRecycleBinRestore, BatchRecycleBinDelete, ClearRecycleBin, Transcode）
    - 创建 `CloudDriveJobStatus` 枚举（Pending, Running, Completed, Failed, Cancelled）
    - 创建 `CookieSessionStatus` 枚举（Active, Invalidated, Revoked）
    - _需求: 1, 15, 21_ _设计: §CookieSession（聚合根 - 领域层）, §CloudDriveJob（聚合根 - 领域层）_

  - [x] 1.2 创建领域层聚合根 `CookieSession`
    - 继承 `AggregateRoot<Guid>`，使用 SqlSugar `[SugarTable]` 和 `[SugarColumn]` 特性标注表名、主键与字段
    - 包含 Id, UserId, CloudAccountUid, EncryptedCookiePayload, KeyVersion, Status, InvalidationReason, CooldownUntil, LastProbedAt, CreationTime, LastModificationTime
    - UID/CID/USSID 三元组序列化为 `{ uid, cid, ussid, extraFields }` JSON 后加密存入 `EncryptedCookiePayload`；原文不暴露为实体属性，仅在 `CookieSessionManager.GetValidCookieHeaderAsync` 的受保护解密管道中取用
    - 保留 `public` 无参构造函数供 SqlSugar ORM 反序列化使用，业务代码通过领域方法操作状态
    - 实现业务方法：`MarkInvalidated(reason, cooldownSeconds)`、`EnterCooldown(cooldownSeconds)`、`IsInCooldown(now)`、`Revoke()`、`RefreshProbe(timestamp)`
    - `MarkInvalidated` 与 `EnterCooldown` 调用时通过 `AddDomainEvent()` 追加 `CookieInvalidated` 领域事件（须继承框架 `Domain.Events.DomainEvent` 抽象基类）
    - _需求: 1, 2_ _设计: §CookieSession（聚合根 - 领域层）, §1. 认证与会话管理模块 (Auth)_

  - [x] 1.3 创建领域层聚合根 `CloudDriveJob`
    - 继承 `AggregateRoot<Guid>`，使用 SqlSugar `[SugarTable]` 和 `[SugarColumn]` 特性标注
    - 包含 Id, UserId, CloudAccountUid, JobType, Status, ResourceId, Progress, ErrorMessage, PayloadJson, CheckpointData, StartTime, EndTime, CreationTime
    - 保留 `public` 无参构造函数供 SqlSugar ORM 反序列化使用，业务代码通过领域方法操作状态
    - 实现业务方法（更新状态、更新进度、记录错误等），状态变更时通过 `AddDomainEvent()` 收集领域事件（领域事件须继承框架 `Domain.Events.DomainEvent` 抽象基类）
    - _需求: 21, 28_ _设计: §CloudDriveJob（聚合根 - 领域层）, §5. 后台任务模块 (Quartz.NET Job)_

  - [x] 1.4 创建业务异常类
    - 创建 `CloudDriveCookieInvalidatedException`（Cookie 失效 / 登录态无效 / HTTP 401 / HTTP 403 / `errNo=login_required`）
    - 创建 `CloudDriveFileNotFoundException`（文件不存在）
    - 创建 `CloudDriveRateLimitException`（频率限制）
    - 创建 `CloudDriveInvalidNameException`（名称非法）
    - 创建 `CloudDriveConfigurationException`（配置缺失 / `CloudDriveFeatureEnabled == false`）
    - 创建 `CloudDriveNetworkException`（网络超时/失败）
    - 创建 `CloudDriveUploadException`（上传失败）
    - 创建 `CloudDriveApiException`（通用 API 错误；包括 `ErrorFieldAdapter` 未命中查找规则时的 `unknown_error_shape`）
    - 在 `ExceptionHandlingMiddleware.HandleExceptionAsync` 的 `switch` 表达式中添加 115 业务异常分支：`CloudDriveCookieInvalidatedException` → 401、`CloudDriveFileNotFoundException` → 404、`CloudDriveRateLimitException` → 429、`CloudDriveInvalidNameException` → 400、`CloudDriveConfigurationException` → 503、`CloudDriveNetworkException` → 502、`CloudDriveUploadException` → 500、`CloudDriveApiException` → 502
    - _需求: 1, 25_ _设计: §错误码映射_

  - [x] 1.5 定义仓储接口、领域事件与领域层文件名验证器
    - 创建 `ICookieSessionRepository` 接口（定义在 Domain 层 `Repositories/`），继承框架 `IRepository<CookieSession, Guid>` 并扩展按 `UserId + CloudAccountUid` 查询的 `GetByAccountAsync` 方法
    - 创建 `ICloudDriveJobRepository` 接口，继承框架 `IRepository<CloudDriveJob, Guid>` 并扩展按 UserId 查询、按 Id 查询方法；为启动恢复协调支持按 Status 扫描 `Pending` / `Running` 任务
    - 创建领域事件 `CookieInvalidated`（继承 `DomainEvent`，携带 `CloudAccountUid`、`Reason`、`InvalidatedAt`、`CooldownUntil`）
    - 创建 `CloudDriveFileNameValidator` 静态类：实现 `ValidateNewFolderName`（限 **255 字符**，对应需求 7 新建文件夹）与 `ValidateRenameFileName`（限 **255 字节**，UTF-8 编码，对应需求 12 重命名）两种校验方法；验证失败时抛出 `CloudDriveInvalidNameException`
    - _需求: 1, 2, 7, 12, 21, 28_ _设计: §CookieSession（聚合根 - 领域层）, §CloudDriveJob（聚合根 - 领域层）, §CloudDriveFileManager_

  - [x] 1.6 编写实体单元测试
    - 测试 `CookieSession` 实体的业务方法：`MarkInvalidated` 正确追加 `CookieInvalidated` 事件、`IsInCooldown` 在冷却期内返回 true、`Revoke` 状态流转
    - 测试 `CloudDriveJob` 实体的状态流转逻辑（Pending → Running → Completed/Failed/Cancelled）
    - 测试 `CloudDriveFileNameValidator` 对 255 字符 / 255 字节边界、空字符串与非法字符的处理（注意中文字符 UTF-8 下占 3 字节）
    - _需求: 1, 7, 12, 21_ _设计: §CookieSession（聚合根 - 领域层）, §CloudDriveJob（聚合根 - 领域层）_

- [x] 2. 应用层 - 服务、抽象接口与属性测试
  - [x] 2.1 创建应用层抽象接口与配置类
    - 创建 `ICloudDriveOpenApiGateway` 接口（定义在 Application 层 `Abstractions/` 目录；语义为 115 API 统一网关抽象，覆盖 `webapi.115.com`、`my.115.com`、`vip.115.com`、`pay.115.com`、`115.com/web/lixian`），方法签名包括 `SendWebApiRequestAsync`、`SendUserCenterRequestAsync`、`SendOfflineApiRequestAsync`、`UploadFileAsync`；配套值对象 `CloudDriveApiContext`（`TargetDomain`、`QueryParameters`、`FormFields`、`JsonBody`、`Category`）
    - 创建 `ICloudDriveMetadataCache` 接口（仅包含 `GetOrSetAsync`、`InvalidateDirectoryAsync`、`InvalidateFileAsync`）
    - 创建 `ICloudDriveRateLimiter` 接口
    - `AcquireAsync` 方法签名：`Task<IDisposable> AcquireAsync(Guid userId, string cloudAccountUid, string? category = null, CancellationToken cancellationToken = default)`，返回 `IDisposable` 许可，调用方通过 `using` 释放
    - `RecordThrottleSignal` 方法签名：`void RecordThrottleSignal(Guid userId, string cloudAccountUid, string? category = null)`
    - 创建 `ICloudDriveSecretProtector` 接口（Cookie 负载加密 / 签名密钥保护），提供 `Protect(string keyName, string plaintext)` / `Unprotect(string keyName, string ciphertext, int? keyVersion = null)` 方法；实现类使用 ASP.NET Core Data Protection 对不同 `keyName` 维护独立 key ring，支持 `keyVersion` 回落解密
    - 创建 `ICookieInvalidationNotifier` 接口，提供 `NotifyAsync(CookieInvalidatedEvent evt, CancellationToken ct)` 方法，用于运维告警/事件总线分发（默认实现输出结构化日志）
    - 创建 `ICookieSignatureStrategy` 接口，签名为 `Task<CookieSignaturePayload> GenerateAsync(string cloudAccountUid, string endpoint, IDictionary<string, string> parameters, CancellationToken ct)`；返回 `{ sign, time, uid }`；默认注册 `NotImplementedCookieSignatureStrategy`，调用时返回明确的 `offline_signature_not_available` 错误（运维提供真实实现后再替换）
    - 创建 `IErrorFieldAdapter` 接口，签名 `CloudDriveApiError? TryParse(Uri requestUri, JsonDocument responseJson)`；配套值对象 `CloudDriveApiError` 与 `ErrorFieldDescriptor`（`UrlPattern`、`CodeFieldName`、`CodeType`、`MessageFieldName`、`FallbackCodeFieldName`、`FallbackMessageFieldName`）以及 `ErrorCodeType` 枚举（Number, String）
    - 创建 `CloudDriveAppConfig` 配置类（字段：`CookieEncryptionKeyName`、`SignatureKeyName?`、`CookieCooldownSeconds`、`WebApiBaseUrl`、`MyApiBaseUrl`、`VipApiBaseUrl`、`PayApiBaseUrl`、`OfflineApiBaseUrl`、`DefaultRateLimit`、`RateLimitOverrides?`、`RateLimitOverflowStrategy`、`RateLimitMaxQueueLength`、`ThrottleRetry*`、`CacheTtlSeconds`、`LargeFileAsyncThresholdBytes`、`OfficePreviewServiceUrl?`、`CloudDriveFeatureEnabled`），通过 `IOptions<CloudDriveAppConfig>` 注入；**不再包含 AppId/AppSecret/CallbackUrl/PkceEnabled 等 OAuth 字段**
    - 创建 `RateLimitOverflowStrategy` 枚举（Queue, Reject）
    - 确认 `IDomainEventSource` 接口已包含 `Track(IHasDomainEvents entity)` 方法签名（框架已实现，无需修改）
    - 实现类 `CloudDriveMetadataCache`、`CloudDriveRateLimiter`、`ErrorFieldAdapter`、`DataProtectionCloudDriveSecretProtector`、`LoggingCookieInvalidationNotifier`、`NotImplementedCookieSignatureStrategy` 位于 Infrastructure 层（见任务 4.9-4.13）
    - _需求: 1, 2, 17, 18, 20, 25, 26_ _设计: §1. 认证与会话管理模块 (Auth), §9. 基础设施组件, §IErrorFieldAdapter / ErrorFieldAdapter（错误响应字段适配器）_

  - [x] 2.2 实现 `CookieSessionManager` 应用层服务
    - 普通类 + 构造函数注入 `ICookieSessionRepository`（Domain 仓储接口）、`ICloudDriveSecretProtector`、`ICookieInvalidationNotifier` 与 `IOptions<CloudDriveAppConfig>`；不继承框架基类
    - 实现 `UpsertSessionAsync(userId, input)`：将 UID/CID/USSID + `ExtraFields` 序列化为 JSON 并通过 `ICloudDriveSecretProtector.Protect(CookieEncryptionKeyName, payload)` 加密写入 `EncryptedCookiePayload`，同步记录 `KeyVersion`；同一 `UserId + CloudAccountUid` 上下文只保留一条记录
    - 实现 `GetValidCookieHeaderAsync(userId, cloudAccountUid, ct)`：读取 `CookieSession` → 判断 `Status` 与 `IsInCooldown` → 通过 `Unprotect` 解密 → 组装为标准 `Cookie` HTTP 头字符串返回；失效或在冷却期内时直接抛出 `CloudDriveCookieInvalidatedException`
    - 实现 `MarkInvalidatedAsync(userId, cloudAccountUid, reason, ct)`：调用聚合根 `MarkInvalidated`（冷却时长来自 `CookieCooldownSeconds`），触发仓储 Update 以便领域事件经 `DomainEventDispatchBehavior` 自动分发；同时调用 `ICookieInvalidationNotifier.NotifyAsync` 输出脱敏日志/告警
    - 实现 `EnterCooldownAsync(userId, input)`：供 `EnterCookieCooldownCommand` 使用，可覆盖默认冷却时长
    - 实现 `RevokeAsync(userId, cloudAccountUid)`：标记 `CookieSession.Revoke()`，清理关联缓存与限流桶
    - 实现 `GetBoundAccountsAsync(userId)`：返回当前用户下所有 `CookieSession` 的摘要列表（不暴露 Cookie 原文，仅返回 `CloudAccountUid`、`Status`、`CreationTime`、`LastModificationTime`、`CooldownUntil`）
    - 解密失败（`KeyVersion` 不匹配或密钥损坏）时返回明确错误并阻止继续发起 115 API 请求
    - 本管理器不提供任何 Cookie 自动刷新逻辑 — 失效后必须由运维通过 `UpsertSessionAsync` 重新注入（与 OAuth 令牌自动刷新机制形成明确对比）
    - _需求: 1, 2, 3, 19_ _设计: §CookieSessionManager, §1. 认证与会话管理模块 (Auth)_

  - [x] 2.3 编写属性测试 - Cookie 安全性
    - **属性 P1: Cookie 安全性**
    - 验证 `UpsertSessionAsync` 存储的 `EncryptedCookiePayload` 经过加密，不包含 UID/CID/USSID 明文
    - 验证同一 key ring / 同一部署上下文下服务重启后仍可成功解密既有 Cookie 与签名密钥
    - 验证日志输出经 `CloudDriveSensitiveFieldsFilter` 脱敏后不包含原始 Cookie 字符串
    - _需求: 1, 2, 26_

  - [x] 2.4 编写属性测试 - Cookie 失效处理
    - **属性 P2: Cookie 失效处理**
    - 模拟 `errNo=login_required` 场景，验证 `CookieSession.Status` 被标记为 `Invalidated` 且 `CooldownUntil` 非空
    - 模拟 HTTP 401 / 403 场景，验证同样触发失效标记与 `CookieInvalidated` 领域事件
    - 验证冷却期内的重复探测返回缓存结果 / `CloudDriveCookieInvalidatedException`，不发起新的官方请求
    - 验证系统不尝试自动恢复失效 Cookie（对比 OAuth Refresh_Token 自动刷新机制）
    - _需求: 1, 3_

  - [x] 2.5 编写属性测试 - 缓存一致性
    - **属性 P4: 缓存一致性**
    - 验证写操作后缓存被清除，下一次读请求触发 API 调用
    - 验证用户主动刷新目录时绕过旧缓存并返回最新官方数据
    - _需求: 20, 27_

  - [x] 2.6 编写属性测试 - 用户数据隔离
    - **属性 P10: 用户数据隔离**
    - 验证不同 `userId / cloudAccountUid` 生成不同缓存键，一个用户或账号的失效操作不影响另一个用户或账号
    - _需求: 20, 27_

  - [x] 2.7 实现 `CloudDriveFileManager` 应用层服务 - 读路径缓存协作
    - 普通类 + 构造函数注入，注入 `ICloudDriveMetadataCache`、`ICloudDriveOpenApiGateway`、`ICloudDriveRateLimiter`、`IOptions<CloudDriveAppConfig>`
    - 实现 `GetFileListAsync`：缓存优先获取文件列表
    - 实现 `GetFileDetailAsync`：缓存优先获取文件详情
    - 复用 `ICloudDriveMetadataCache` 在读取路径优先命中缓存
    - 调用 `ICloudDriveOpenApiGateway` 时仅需传递 `UserId + CloudAccountUid`；网关内部从 `CookieSessionManager` 获取当前 Cookie 并注入请求头，Cookie 方案不携带 `AppId` 维度
    - _需求: 3, 4, 27_ _设计: §CloudDriveFileManager, §2. 文件管理模块 (File)_

  - [x] 2.8 实现 `CloudDriveFileManager` - 写路径缓存失效协作
    - 实现 `ExecuteWithCacheInvalidationAsync`：执行写操作并使受影响目录缓存失效
    - 统一上传、创建文件夹、移动、复制、删除、重命名、还原等写路径的缓存失效编排，并为清空回收站提供复用的缓存清理入口
    - _需求: 27_ _设计: §CloudDriveFileManager_

  - [x] 2.9 编写属性测试 - 文件名验证
    - **属性 P5: 文件名验证**
    - 验证 `CloudDriveFileNameValidator.ValidateNewFolderName` 对 255 字符边界、空字符串、非法字符抛出 `CloudDriveInvalidNameException`
    - 验证 `CloudDriveFileNameValidator.ValidateRenameFileName` 对 255 字节边界（UTF-8）、空字符串、非法字符抛出 `CloudDriveInvalidNameException`
    - _需求: 7, 12_

  - [x] 2.10 编写属性测试 - 频控限流一致性
    - **属性 P3: 频控限流一致性**
    - 验证并发请求下通过的请求数不超过按用户 + 115 账号 + 场景维度的配置上限
    - 验证 Queue / Reject 两种超限策略与最大队列长度生效
    - _需求: 17, 24_

  - [x] 2.11 编写属性测试 - 缓存数据真实性
    - **属性 P11: 缓存数据真实性**
    - 验证缓存写入仅包含官方响应字段与系统可验证派生值，官方缺失字段返回空值或缺省值而非伪造数据
    - _需求: 27_

- [x] 3. 检查点 - 领域层与应用层服务完成验证
  - 当前阶段通过条件：
    - `CookieSessionManager`、`CloudDriveFileManager` 的必做测试通过（应用层服务）
    - `CloudDriveFileNameValidator` 的文件名验证测试通过（领域层静态验证器，含 255 字符 / 255 字节双规则）
    - `ICloudDriveMetadataCache`、`ICloudDriveRateLimiter`、`ICookieInvalidationNotifier`、`ICookieSignatureStrategy`、`IErrorFieldAdapter` 接口已定义在 Application 层
    - 不依赖真实 115 API 即可验证 Cookie 加密/解密、失效冷却、名称校验、缓存命中/失效、缓存真实性、隔离维度与限流逻辑
    - 领域层仅包含实体、枚举、异常、仓储接口、领域事件和文件名验证器，无基础设施依赖

- [ ] 4. 基础设施层 - HTTP 客户端、仓储与数据库
  - [x] 4.1 创建 115 网页端 API 响应模型
    - 创建 `CloudDrive115WebResponse<T>` 通用响应包装类（`State`, `Data`, `Message`/`Msg`/`Error`, `Code`/`Errno`/`ErrCode`/`ErrNo`）；其中 `State` 属性使用 `BoolOrNumberJsonConverter` 兼容 bool(true/false) 与 number(0/1)；具体错误字段的选择由 `IErrorFieldAdapter` 按 URL 路径决定，响应模型本身仅以联合字段呈现
    - 创建各 API 端点的具体响应模型（文件列表、文件详情、下载地址、视频播放、字幕、回收站、云下载任务、分享列表、用户中心多源）
    - 创建字段归一化反序列化适配器处理网页端文件对象字段映射：`fid → FileId`、`n → Name`、`s → Size`、`fc`（0=文件夹, 1=文件）→ `IsDirectory`；回收站接口的 `type`（1=文件, 2=目录）需反向映射；搜索接口的 `fc`（1=仅文件夹, 2=仅文件）作为请求参数独立处理
    - _需求: 4, 5, 6, 14, 16, 20, 25_ _设计: §9. 基础设施组件, §DTO 定义_

  - [x] 4.2 实现 `CloudDriveApiClient` 基础设施骨架
    - 使用 `IHttpClientFactory` 创建 Named HttpClient（按域名分组：`Cloud115Web`、`Cloud115My`、`Cloud115Vip`、`Cloud115Pay`、`Cloud115Offline`）
    - 实现 `ICloudDriveOpenApiGateway` 接口（定义在 Application 层），作为访问 115 网页端 API 的基础设施实现
    - 配置 `WebApiBaseUrl`、`MyApiBaseUrl`、`VipApiBaseUrl`、`PayApiBaseUrl`、`OfflineApiBaseUrl` 与共享 `System.Text.Json` 序列化设置
    - 构造函数注入 `CookieSessionManager`、`IErrorFieldAdapter`、`ICookieSignatureStrategy`、`ICloudDriveRateLimiter`、`IOptions<CloudDriveAppConfig>`、`ILogger<CloudDriveApiClient>`
    - _需求: 2, 17, 25_ _设计: §ICloudDriveOpenApiGateway / CloudDriveApiClient_

  - [ ] 4.3 实现网页端与用户中心请求封装
    - 实现 `SendWebApiRequestAsync`：默认走 `WebApiBaseUrl`；按 `CloudDriveApiContext` 组装 query string 与 form/json body；通过 `CookieSessionManager.GetValidCookieHeaderAsync` 获取当前 Cookie 并注入请求头；调用 `ICloudDriveRateLimiter.AcquireAsync` 按场景限流
    - 实现 `SendUserCenterRequestAsync`：按 `context.TargetDomain` 路由到 `my.115.com` / `vip.115.com` / `pay.115.com`；注入 Cookie；对 VIP 响应中 `00_balance` 等数字开头字段归一化为 `Balance`
    - 实现 `LoginCheckHandler` / `LoginStatusProbe` 辅助：通过 `GET webapi.115.com/files?cid=0&limit=1` 的业务成功/失败结果判定登录态；失败结果进一步由 `IErrorFieldAdapter` 解析判断是否为 `login_required`
    - 响应处理：先检查 HTTP 状态码；若 HTTP 成功，通过 `IErrorFieldAdapter.TryParse(requestUri, responseJson)` 判定业务成功；业务失败按错误码映射为 `CloudDrive*Exception`
    - 对 `errNo=login_required` / HTTP 401 / HTTP 403 场景同步调用 `CookieSessionManager.MarkInvalidatedAsync` 进入冷却期并抛出 `CloudDriveCookieInvalidatedException`
    - _需求: 1, 3, 4, 5, 23, 25_ _设计: §ICloudDriveOpenApiGateway / CloudDriveApiClient, §IErrorFieldAdapter / ErrorFieldAdapter（错误响应字段适配器）, §CookieSessionManager_

  - [ ] 4.4 实现云下载签名与上传请求封装
    - 实现 `SendOfflineApiRequestAsync`：默认走 `OfflineApiBaseUrl`；调用 `ICookieSignatureStrategy.GenerateAsync` 生成 `{ sign, time, uid }` 三元组，合并到 form body；在请求 URL 上追加 `?ct=lixian&ac={操作符}` 必需查询参数
    - 在 `ICookieSignatureStrategy` 未提供有效实现（默认 `NotImplementedCookieSignatureStrategy`）时抛出 `CloudDriveConfigurationException`，带 `offline_signature_not_available` 错误码
    - 实现 `UploadFileAsync`：对网页端上传链路返回的 OSS / multipart 上传凭证 URL 发送 `multipart/form-data`；不直接注入 Cookie（仅依赖上传接口返回的临时凭证）
    - 为后台任务恢复保留必要的上下文字段（`TempFilePath` 和官方返回的 `pick_code`）
    - _需求: 5, 20, 21, 26_ _设计: §6. 云下载模块 (Offline), §ICookieSignatureStrategy（云下载签名预留扩展点）, §ICloudDriveOpenApiGateway / CloudDriveApiClient_

  - [ ] 4.5 实现错误映射、脱敏日志与 Polly 韧性策略
    - 复用框架层 Polly 配置基础，为各 Named HttpClient 配置幂等请求的有限重试、超时、熔断策略
    - 读请求（GET）应用重试 + 超时 + 熔断；写请求（POST/PUT/DELETE）仅超时 + 熔断，不自动重试
    - 同时处理 HTTP 错误和 HTTP 200 但业务错误码失败的场景；错误解析统一委托 `IErrorFieldAdapter.TryParse`
    - 将网络连接超时和连接失败分别映射为 `CloudDriveNetworkException`，并保留可读错误原因
    - 记录请求方法、请求路径、错误码、错误消息和重试次数；通过 `CloudDriveSensitiveFieldsFilter` 对 Cookie / UID / CID / USSID / 签名密钥 / extraFields 进行脱敏
    - 实现 115 错误码到业务异常的映射逻辑：`login_required` → `CloudDriveCookieInvalidatedException`；频控错误码 → `CloudDriveRateLimitException`（同时回调 `ICloudDriveRateLimiter.RecordThrottleSignal`）；404 → `CloudDriveFileNotFoundException`；其他业务失败 → `CloudDriveApiException`
    - _需求: 1, 17, 25, 26_ _设计: §错误码映射, §IErrorFieldAdapter / ErrorFieldAdapter（错误响应字段适配器）_

  - [ ] 4.6 实现 `ErrorFieldAdapter` 注册表与默认规则
    - 实现 `IErrorFieldAdapter`；内部维护不可变 `IReadOnlyDictionary<string, ErrorFieldDescriptor>`，注册 `requirements.md:454-470` 覆盖的全部错误字段变体
    - 必须注册的 URL 模式（至少）：`/files/search`（`errCode` number + `errMsg`）、`/movies/subtitle`（大写 `errNo` + `error`）、`/user/vip_limit`（`code` string + `msg`）、`/user/index_info`（`code` string）、`pay.115.com/`（`msg` 非 `message`）、`/share/send`（`errNo` + fallback `errno`）、`/share/receive`（`errNo` + fallback `errno`）、`/share/slist`（`errNo`）、`115.com/web/lixian/`（`errcode` + `error_msg`）、`/rb` 回收站（`errNo` + `error`）、`/files`（`errNo` + `error`）以及 `/files/add`、`/files/edit` 等写入端点
    - 默认规则（未命中任一 URL 模式时）：读 `state` + `code` / `errno` + `message` / `error`；全部字段均缺失时仅以 HTTP 状态码判定成败，且标注 `unknown_error_shape` 错误码
    - 注册为 Singleton 生命周期，保证线程安全
    - _需求: 25_ _设计: §IErrorFieldAdapter / ErrorFieldAdapter（错误响应字段适配器）_

  - [ ] 4.7 编写属性测试 - 错误码映射完整性
    - **属性 P6: 错误码映射完整性**
    - 验证 `ErrorFieldAdapter` 对 12 种错误字段变体（`errNo` / `errCode` / `errno` / `code` / `msg` / `message` / `Message` / `err` / `error` / 大小写混用等）按 URL 路径正确解析
    - 验证 HTTP 401/403/404/429/500 与业务错误码均映射为正确的 `CloudDrive*Exception`
    - 验证 `errNo=login_required` 触发 `CloudDriveCookieInvalidatedException` 且同步调用 `CookieSessionManager.MarkInvalidatedAsync`
    - 验证网络超时与连接失败分别映射为明确的网络异常
    - 验证幂等请求按配置重试，非幂等写请求不自动重试
    - 验证未命中任何 URL 模式时返回带 `unknown_error_shape` 错误码的 `CloudDriveApiException` 并保留脱敏摘要
    - _需求: 1, 17, 25_

  - [ ] 4.8 实现 SqlSugar CodeFirst 建表与实体映射
    - 在应用启动时调用 `db.CodeFirst.InitTables<CookieSession>()` 和 `db.CodeFirst.InitTables<CloudDriveJob>()` 自动创建或更新表
    - 通过 `[SugarTable]`、`[SugarColumn(IsPrimaryKey = true)]`、`[SugarColumn(Length = ...)]` 等特性配置表名、主键、字段长度与索引
    - `CookieSession` 需为 `UserId + CloudAccountUid` 建立唯一索引；`EncryptedCookiePayload` 使用足够长度的 TEXT 列以容纳加密密文
    - _需求: 1, 21_ _设计: §CookieSession（聚合根 - 领域层）, §CloudDriveJob（聚合根 - 领域层）_

  - [ ] 4.9 实现仓储类
    - 实现 `CookieSessionRepository`，独立实现 `ICookieSessionRepository` 接口（不继承框架 `Repository<,>` 基类，因其为 `internal sealed` 且未注入 `IDomainEventSource`），直接注入 `ISqlSugarClient` 和 `IDomainEventSource`，封装 CRUD 操作并扩展 `GetByAccountAsync`、`ListByUserAsync` 查询方法
    - 实现 `CloudDriveJobRepository`，独立实现 `ICloudDriveJobRepository` 接口，直接注入 `ISqlSugarClient` 和 `IDomainEventSource`，封装 CRUD 操作并扩展按 UserId 查询、按 Id 查询、按 Status 扫描方法
    - 仓储的 Insert/Update 操作后需调用 `IDomainEventSource.Track(entity)` 将实体注册到领域事件追踪集合，以便 `DomainEventDispatchBehavior` 在事务提交前自动分发领域事件
    - 注册为 Scoped 生命周期
    - _需求: 1, 21_ _设计: §CookieSession（聚合根 - 领域层）, §CloudDriveJob（聚合根 - 领域层）_

  - [ ] 4.10 注册基础设施层依赖注入
    - 创建 `IServiceCollection` 扩展方法 `AddCloudDriveInfrastructure()`
    - 注册各 Named HttpClient（`Cloud115Web`、`Cloud115My`、`Cloud115Vip`、`Cloud115Pay`、`Cloud115Offline`）与 Polly 策略
    - 注册 `ICloudDriveOpenApiGateway` → `CloudDriveApiClient`
    - 注册 `ICloudDriveSecretProtector` → `DataProtectionCloudDriveSecretProtector`（内部按 `keyName` 维护独立 key ring）
    - 注册 `ICookieInvalidationNotifier` → `LoggingCookieInvalidationNotifier`（默认；可替换为事件总线实现）
    - 注册 `ICookieSignatureStrategy` → `NotImplementedCookieSignatureStrategy`（默认；运维提供真实签名实现后替换）
    - 注册 `IErrorFieldAdapter` → `ErrorFieldAdapter`（Singleton）
    - 注册 `ICloudDriveMetadataCache` → `CloudDriveMetadataCache`（Singleton）
    - 注册 `ICloudDriveRateLimiter` → `CloudDriveRateLimiter`（Singleton）
    - 注册仓储实现（`CookieSessionRepository`、`CloudDriveJobRepository`，Scoped）
    - 注册 `CloudDriveAppConfig` 配置绑定（`services.Configure<CloudDriveAppConfig>(configuration.GetSection("CloudDrive"))`）
    - 确保 Cookie 加密密钥通过机密存储或加密配置提供，不会以明文暴露到日志或响应
    - 确保 Data Protection key ring 在同一部署内可持久化复用，以保证服务重启后仍可解密既有 Cookie 和签名密钥
    - _需求: 1, 2, 17, 20, 25, 26_ _设计: §9. 基础设施组件_

  - [ ] 4.11 实现 `CloudDriveRateLimiter` 基础设施类
    - 实现 `ICloudDriveRateLimiter` 接口，基于令牌桶算法，注册为 Singleton
    - 内部通过 `ConcurrentDictionary<string, TokenBucket>` 按 `UserId + CloudAccountUid + Category` 维度键持有令牌桶实例，确保跨请求共享限流状态
    - 按用户 + 115 账号 + 场景维度限流，支持 Queue / Reject 超限策略与最大队列长度
    - 暴露 `RecordThrottleSignal` 接收 115 官方频控信号并触发退避
    - _需求: 17, 24_ _设计: §CloudDriveRateLimiter_

  - [ ] 4.12 实现 `CloudDriveMetadataCache` 基础设施类
    - 实现 `ICloudDriveMetadataCache` 接口，内部使用 `IDistributedCache` + `DistributedCacheExtensions`
    - 实现 `GetOrSetAsync`：缓存优先获取，未命中时调用 factory 并写入缓存
    - 实现 `InvalidateDirectoryAsync`：按用户 + 115 账号 + 目录维度使缓存失效
    - 实现 `InvalidateFileAsync`：按用户 + 115 账号 + 文件维度使缓存失效
    - 缓存键按 `UserId + CloudAccountUid + DirectoryId/FileId` 组合生成，确保不同账号数据完全隔离
    - 确保缓存内容仅包含文件元数据，不得存储 Cookie / UID / CID / USSID / 签名密钥等敏感信息
    - 支持通过 `CloudDriveAppConfig.CacheTtlSeconds` 配置 TTL
    - _需求: 20, 27_ _设计: §CloudDriveMetadataCache_

  - [ ] 4.13 实现 `CloudDriveSensitiveFieldsFilter` 日志脱敏过滤器
    - 实现 `ILogFilter` 或 `ILoggerProvider` 包装器，在日志输出阶段统一替换 Cookie / UID / CID / USSID / 签名密钥 / `ExtraFields` 值为 `[REDACTED]`
    - 识别字段名（大小写不敏感）：`cookie`, `uid`, `cid`, `ussid`, `sign`, `signkey`, `password`, `encryptedcookiepayload` 以及 `CloudDriveAppConfig.CookieEncryptionKeyName` / `SignatureKeyName` 对应的密钥名
    - 对 HTTP 请求头 / 响应体 / 异常堆栈中的匹配位置均执行脱敏；保留用于定位问题的元数据（状态码、时间戳、请求 ID）
    - 对外响应与审计日志同样套用该过滤器，确保敏感字段不会泄漏到 ProblemDetails 响应
    - _需求: 26_ _设计: §P1: Cookie 安全性, §1. 认证与会话管理模块 (Auth)_

- [ ] 5. 检查点 - 基础设施层完成验证
  - 当前阶段通过条件：
    - 关键请求封装（网页端/用户中心/云下载/上传）、错误适配器、脱敏日志与 Polly 韧性策略的必做测试通过
    - SqlSugar CodeFirst 建表可正常执行（`CookieSession`、`CloudDriveJob`）
    - `ICloudDriveOpenApiGateway`、`IErrorFieldAdapter`、`ICookieInvalidationNotifier`、`ICookieSignatureStrategy`、仓储与 `CloudDriveAppConfig` 装配完成，可供应用层直接使用
    - `CloudDriveMetadataCache`、`CloudDriveRateLimiter`、`CloudDriveSensitiveFieldsFilter` 基础设施实现已完成并注册

- [ ] 6. 应用层 - DTO 定义与手动映射
  - [ ] 6.1 创建应用层 DTO
    - 创建公共 DTO：`CloudDriveOperationResultDto`、`CloudDriveAccountScopedInput`
    - 创建分页与列表包装 DTO：`PagedResultDto<T>`、`ListResultDto<T>`（若框架基础设施未提供则在此创建）
    - 创建文件相关 DTO：`CloudDriveFileDto`、`CloudDriveFileDetailDto`、`CloudDrivePathNodeDto`、`CloudDriveFileLabelDto`、`CloudDriveSearchResultDto`、`CloudDriveDownloadDto`、`CloudDriveFolderDto`、`CloudDriveUploadSessionDto`、`CloudDriveUploadedFileDto`、`CloudDriveUploadExecutionDto`、`CloudDriveRecycleBinItemDto`
    - 创建预览相关 DTO 与枚举：`CloudDriveImagePreviewDto`、`CloudDriveDocPreviewDto`、`DocRenderMode` 枚举（Direct, ThirdParty, NotAvailable）
    - 创建视频相关 DTO 与枚举：`CloudDriveVideoPlayDto`（含 `UserDef` 官方记忆清晰度）、`VideoPlayStatus` 枚举（Ready, Transcoding）、`VideoQualityOption`（含 `DefinitionNew` 新版清晰度标识）、`AudioTrackOption`、`CloudDriveVideoTranscodeDto`、`CloudDriveVideoPlayProgressDto`（含 `AddTime`）、`CloudDriveSubtitleListDto`、`CloudDriveSubtitleDto`
    - 创建认证相关 DTO：`CloudDriveCookieSessionDto`（`CloudAccountUid`、`Status`、`CreationTime`、`LastModificationTime`、`CooldownUntil`）、`CloudDriveLoginStatusDto`（`CloudAccountUid`、`IsLoggedIn?`、`LastCheckedAt`、`CooldownUntil?`、`FailureReason?`）、`CloudDriveUserInfoDto`、`CloudDriveSpaceInfoDto`、`CloudDriveVipInfoDto`、`CloudDriveAccountSummaryDto`
    - 创建云下载 DTO：`CloudDriveOfflineTaskDto`、`CloudDriveOfflineQuotaDto`（字段归一化 `total_count → Total`、`failed_count → Failed`、`finished_count → Completed`、`downloading_count → Active`）
    - 创建分享管理 DTO：`CloudDriveShareItemDto`、`CloudDriveShareSendItemDto`、`CloudDriveShareReceiveItemDto`
    - 创建任务相关 DTO：`CloudDriveJobDto`
    - 不再创建 `CloudDriveTokenDto` 与 `CloudDrivePkceAuthorizationDto`（OAuth 方案遗留 DTO，Cookie 方案不使用）
    - _需求: 1, 3, 19, 20, 21, 22, 23_ _设计: §DTO 定义_

  - [ ] 6.2 创建应用层输入 DTO 与 FluentValidation 验证器
    - 创建基础 Input：`CloudDriveAccountScopedInput`
    - 创建 Cookie 会话管理 Input：`UpsertCookieSessionInput`（`CloudAccountUid`、`Uid`、`Cid`、`Ussid`、`ExtraFields?`）、`EnterCookieCooldownInput`（继承 `CloudDriveAccountScopedInput`，含 `Reason`、`CooldownSeconds?`）、`RevokeCookieSessionInput`（继承 `CloudDriveAccountScopedInput`）
    - 创建文件 Input：`GetFileListInput`、`GetFileInput`、`CreateFolderInput`、`InitUploadInput`、`UploadFileInput`、`GetDownloadUrlInput`、`MoveFilesInput`、`CopyFilesInput`、`DeleteFilesInput`、`RenameFileInput`、`ClearRecycleBinInput`、`SearchFileInput`、`GetRecycleBinListInput`、`RecycleBinOperationInput`、`RefreshDirectoryInput`、`ToggleStarInput`
    - 创建预览与视频 Input：`GetPreviewInput`、`GetVideoPlayInput`、`SubmitTranscodeInput`、`GetSubtitlesInput`、`GetVideoPlayProgressInput`、`RememberVideoPlayProgressInput`
    - 创建云下载 Input：`GetOfflineTaskListInput`、`AddOfflineTaskInput`（含 `Urls: List<string>`、`SavePath?`）、`CancelOfflineTaskInput`、`ClearOfflineTasksInput`
    - 创建分享 Input：`GetShareListInput`
    - 创建任务 Input：`GetJobStatusInput`、`GetJobListInput`
    - 除运维端点（`UpsertCookieSessionInput`、任务列表全量查询）与登录态探测外，所有输入 DTO 继承 `CloudDriveAccountScopedInput` 并校验 `CloudAccountUid`
    - 为每个输入 DTO 创建对应的 `AbstractValidator<T>` 验证器，由框架 `ValidationBehavior` 自动拦截
    - _需求: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 23_ _设计: §DTO 定义, §关键输入 DTO（节选）_

  - [ ] 6.3 编写属性测试 - 临时资源标识
    - **属性 P9: 临时资源标识**
    - 验证所有返回临时资源的 DTO（下载地址、预览地址、播放地址）在构造与映射为对外响应后均保持 `IsTemporary = true`
    - _需求: 6, 14, 15, 16_

  - [ ] 6.4 创建手动映射扩展方法
    - 为 115 网页端 API 响应模型 → 领域对象 → DTO 创建静态扩展方法 `ToDto()` / `ToEntity()`
    - 编译期可检查类型安全，无反射开销
    - 按模块组织映射扩展方法（`CookieMapping`、`FileMapping`、`PreviewMapping`、`VideoMapping`、`OfflineMapping`、`ShareMapping`、`UserAggregationMapping`、`JobMapping`）
    - 文件映射内完成字段归一化：`fid → FileId`、`n → Name`、`s → Size`、`fc` 值域转换等；回收站 `type` 字段的反向映射
    - 映射时对网页端未返回的字段保留 null 或缺省值，不得伪造官方未提供且不可验证的数据
    - **严禁使用 AutoMapper**（CD0004）
    - _需求: 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 19, 20, 21, 22, 23_ _设计: §DTO 定义_

  - [ ] 6.5 补充视频播放进度 DTO、输入 DTO 与验证器
    - 创建 `CloudDriveVideoPlayProgressDto`（含 `AddTime` 官方 add_time 时间戳）
    - 创建 `GetVideoPlayProgressInput` 与 `RememberVideoPlayProgressInput`
    - 创建 `GetVideoPlayProgressInputValidator` 与 `RememberVideoPlayProgressInputValidator`
    - 为视频播放 DTO 补充 `UserDef`（网页端 user_def 记忆清晰度）、`RestrictionReason`、`UsageRestrictionNotice`、多音轨与细化清晰度字段的映射验证
    - 为 `VideoQualityOption` 补充 `DefinitionNew`（网页端 definition_n 新版清晰度标识）
    - 为 `CloudDriveRecycleBinItemDto` 补充 `RecycleBinItemId`（网页端 `id`，用于 `tid` 参数）和 `RestoreStatus`（网页端 `status`：-1=还原中, 0=正常），注意回收站 `type` 值域（1=文件,2=目录）与文件列表 `fc` 值域（0=文件夹,1=文件）反转
    - _需求: 11, 14, 15_ _设计: §视频相关 DTO, §DTO 定义_

- [ ] 7. 应用层 - Cookie 会话管理 Command/Query Handler
  - [ ] 7.1 实现运行时配置保护与 `UpsertCookieSessionCommandHandler`（运维端点）
    - 所有 Handler 入口处通过 `IOptions<CloudDriveAppConfig>` 检查 `CloudDriveFeatureEnabled`；为 `false` 时抛出 `CloudDriveConfigurationException`
    - 实现 `UpsertCookieSessionCommandHandler`：接收 `UpsertCookieSessionInput`（UID/CID/USSID + 可选 ExtraFields），委托 `CookieSessionManager.UpsertSessionAsync` 加密持久化
    - 成功后发布 `CookieSessionUpserted` 领域事件（由仓储 `Track(entity)` + `DomainEventDispatchBehavior` 自动分发）；Handler 内部不手动调用 `IMediator.Publish()`
    - **安全要求**：此 Command 必须仅在管理员/运维专用端点暴露；Controller 层通过策略或授权属性（如 `[Authorize(Roles = "Admin")]`）限制访问
    - 同步清理该账号上下文的元数据缓存（调用 `ICloudDriveMetadataCache.InvalidateDirectoryAsync` 清空根目录）与限流桶
    - _需求: 1, 2_ _设计: §1. 认证与会话管理模块 (Auth)_

  - [ ] 7.2 实现 `GetLoginStatusQueryHandler`（登录态探测）
    - 通过调用 `GET webapi.115.com/files?cid=0&limit=1` 探测 Cookie 是否仍然可用
    - 判定规则：(1) 业务成功响应 → `IsLoggedIn = true`；(2) `IErrorFieldAdapter` 返回 `login_required` 或 HTTP 401/403 → `IsLoggedIn = false` 并触发 `EnterCookieCooldownCommand`；(3) 网络异常或未分类错误 → `IsLoggedIn = null`（未知状态），不触发冷却
    - 若当前 `CookieSession.Status == Invalidated` 且 `IsInCooldown()`，直接返回冷却状态快照，不发起官方探测请求
    - 更新 `CookieSession.LastProbedAt` 时间戳供审计
    - 返回 `CloudDriveLoginStatusDto`（含 `IsLoggedIn`、`LastCheckedAt`、`CooldownUntil?`、`FailureReason?`）
    - Query Handler 不触发写副作用（CD0007）；但状态更新通过内部调用 `EnterCookieCooldownCommand` 实现隔离（侧面触发副作用）
    - _需求: 3_ _设计: §1. 认证与会话管理模块 (Auth), §LoginCheckHandler（需求 3 登录态检测）_

  - [ ] 7.3 实现 `EnterCookieCooldownCommandHandler`
    - 接收 `EnterCookieCooldownInput`（含 `CloudAccountUid`、`Reason`、`CooldownSeconds?`）
    - 委托 `CookieSessionManager.EnterCooldownAsync`：调用聚合根 `EnterCooldown(cooldownSeconds)`，触发仓储 Update 以便 `CookieInvalidated` 领域事件经 `DomainEventDispatchBehavior` 自动分发
    - 冷却时长优先使用输入参数 `CooldownSeconds`，否则使用 `CloudDriveAppConfig.CookieCooldownSeconds`（默认 60 秒）
    - 用于 `CloudDriveApiClient` 侧面检测到 Cookie 失效时的内部状态同步，以及运维手动标记失效的入口
    - _需求: 1, 3_ _设计: §CookieSessionManager_

  - [ ] 7.4 实现 `GetBoundAccountsQueryHandler`、`GetUserInfoQueryHandler` 与 `RevokeCookieSessionCommandHandler`
    - 实现 `GetBoundAccountsQueryHandler`：基于当前用户的 `CookieSession` 记录返回已绑定 115 账号摘要列表（仅 `CloudAccountUid`、`Status`、`CreationTime`、`LastModificationTime`、`CooldownUntil`；**绝不包含 Cookie 原文**）
    - 实现 `GetUserInfoQueryHandler`：获取指定 `CloudAccountUid` 的 115 账号信息；委托 `UserAggregationService.AggregateUserInfoAsync` 并行调用 `webapi.115.com/user/info` + `webapi.115.com/user/vip_limit` + 其他多源接口
    - 实现 `RevokeCookieSessionCommandHandler`：接收 `RevokeCookieSessionInput`；委托 `CookieSessionManager.RevokeAsync`，标记 `CookieSession.Revoke()` 并清理关联缓存与限流桶；**不调用远程登出接口**（网页端无对应接口）
    - 多账号绑定场景下，缺失 `CloudAccountUid` 时返回明确错误
    - 若当前 `CookieSession.Status != Active`，`GetUserInfoQueryHandler` 返回明确的 `cookie_not_active` 业务错误，提示由运维重新注入 Cookie
    - _需求: 1, 3, 19, 23_ _设计: §CookieSessionManager, §8. 用户中心聚合模块 (UserAggregator)_

  - [ ] 7.5 编写属性测试 - 配置完整性校验
    - **属性 P8: 配置完整性校验**
    - 验证 `CookieEncryptionKeyName`、`WebApiBaseUrl` 或（如启用云下载）`SignatureKeyName` 任一缺失时，`CloudDriveFeatureEnabled` 为 `false`
    - 验证 `CloudDriveFeatureEnabled == false` 时所有 Handler 方法抛出 `CloudDriveConfigurationException`
    - 验证正确配置下 `CloudDriveFeatureEnabled == true` 且 Handler 正常执行
    - _需求: 2_

  - [ ] 7.6 编写 Cookie 会话多账号上下文单元测试
    - 验证 `UpsertCookieSessionCommandHandler` 写入 `CookieSession` 后 `CloudAccountUid` 与 `UserId` 双维度隔离，同一用户下不同账号互不影响
    - 验证 `RevokeCookieSessionCommandHandler` 仅影响指定 `CloudAccountUid` 的记录与缓存/限流桶，不波及其他账号
    - 验证 `GetBoundAccountsQueryHandler` 仅返回当前用户的 `CookieSession` 记录，不跨用户泄漏
    - 验证 `GetUserInfoQueryHandler` 对缺失 `CloudAccountUid` 的请求返回明确错误（除运维/登录态端点外所有已授权请求必须显式传递 `CloudAccountUid`）
    - _需求: 1, 19, 23_ _设计: §CookieSessionManager, §1. 认证与会话管理模块 (Auth)_

- [x] 8. 应用层 - 后台任务编排与任务查询

  - [x] 8.1 实现 `CloudDriveJobOrchestrator`
    - 实现 `ICloudDriveJobOrchestrator` 接口（定义在 Application 层 `Abstractions/`），实现类 `CloudDriveJobOrchestrator` 位于 Application 层
    - 统一决定同步/异步执行边界并创建 `CloudDriveJob` 记录，创建时将业务输入参数序列化为 JSON 写入 `PayloadJson` 字段；上传任务持久化可恢复元数据（`TempFilePath` 和官方返回的 `pick_code`），不直接序列化文件流
    - 通过应用层抽象 `ICloudDriveJobScheduler.ScheduleAsync()` 触发调度；Infrastructure 层当前使用 Quartz.NET 创建 Trigger，并通过 `JobDataMap` 传递 `JobId`
    - 对外组装 `JobId` 与受理结果，供 `CloudDriveOperationResultDto` / `CloudDriveUploadExecutionDto` 复用
    - _需求: 5, 8, 9, 10, 11, 15, 21_ _设计: §5. 后台任务模块 (Quartz.NET Job), §CloudDriveJob（聚合根 - 领域层）_

  - [x] 8.2 实现 `CloudDriveUploadJob` (Quartz IJob - Infrastructure 层)
    - 实现 `IJob.Execute(IJobExecutionContext context)`，从 `JobDataMap` 获取 `JobId`，从数据库 `CloudDriveJob.PayloadJson` 字段加载业务载荷（上传任务使用包含 `DirectoryId`、`FileName`、`FileSize`、`FileSha1`、`TempFilePath`、`PickCode` 等可恢复元数据的内部载荷对象）。Job 类定义在 Infrastructure 层（遵循 infrastructure-patterns.md 规范）
    - 复用与 `InitUploadCommandHandler` / `UploadFileCommandHandler` 相同的 `CloudDriveUploadInitContract` 与 `CloudDriveUploadSessionDto`；统一使用 `POST uplb.115.com/3.0/sampleinitupload.php`（body: `userid={uid}&filename={name}&filesize={bytes}&target=U_1_{cid}`）；秒传时直接完成，非秒传时按网页端返回的 OSS 直传凭证（`host/object/accessid/policy/signature/callback`）执行 `POST {host}/`（PostObject）
    - 处理大文件异步上传、进度更新与失败恢复；断点续传优先通过本地恢复检查点（`TempFilePath` + `pickcode`）重新发起上传（网页端未发现专用远端分片续传接口）
    - 记录任务状态、开始/结束时间与错误摘要
    - 保存恢复检查点（持久化 `TempFilePath` 和网页端返回的 `pickcode`），支持基于本地临时文件和断点续传的安全重跑
    - 执行实际上传前校验 `TempFilePath` 指向的临时文件存在；启动恢复服务仅在该本地恢复资源仍可用时才重新调度上传任务，否则直接标记任务失败并记录原因
    - 当本地恢复资源不可用或不可恢复失败发生时，返回上传失败，并清理本地恢复检查点与临时上传文件；若网页端提供取消或废弃上传会话的接口，优先调用该接口回收远端会话
    - 上传成功后失效目标目录缓存
    - 注意：Quartz Job 在独立 DI Scope 中执行，不经过 MediatR Pipeline（`TransactionBehavior` 和 `DomainEventDispatchBehavior` 均不生效）；Job 内部需自行管理事务，且直接操作 `CloudDriveJob` 实体产生的领域事件需通过注入 `IMediator` 手动调用 `Publish()` 逐一分发并清空事件集合。此说明同样适用于任务 8.3、8.4、8.5
    - _需求: 5, 21, 27_ _设计: §5. 后台任务模块 (Quartz.NET Job), §CloudDriveJob（聚合根 - 领域层）_

  - [x] 8.3 实现 `CloudDriveBatchOperationJob` (Quartz IJob - Infrastructure 层)
    - 实现 `IJob.Execute()`，处理批量复制、移动、删除、回收站批量还原（`BatchRecycleBinRestore`）和回收站批量彻底删除（`BatchRecycleBinDelete`）任务。Job 类定义在 Infrastructure 层
    - 根据 `CloudDriveJobType` 分派到对应的 115 网页端 API（具体路径以 `requirements.md` 采集为准）：`BatchCopy` → `webapi.115.com/files/copy`、`BatchMove` → `webapi.115.com/files/move`、`BatchDelete` → `webapi.115.com/rb/delete`（移至回收站，`aid=7`）、`BatchRecycleBinRestore` → `webapi.115.com/rb/revert`、`BatchRecycleBinDelete` → `webapi.115.com/rb/delete`（`ignore_warn=1` 彻底删除）；所有请求通过 `ICloudDriveOpenApiGateway.SendWebApiRequestAsync` 统一发出，Cookie 由网关内部自动注入
    - 持续更新进度、错误摘要与任务状态
    - 对支持安全重试的批量任务记录已完成项与最近安全检查点
    - 任务完成后失效源目录与目标目录缓存
    - _需求: 8, 9, 10, 11, 21, 27_ _设计: §5. 后台任务模块 (Quartz.NET Job), §CloudDriveJob（聚合根 - 领域层）_

  - [ ] 8.4 实现 `CloudDriveClearRecycleBinJob` (Quartz IJob - Infrastructure 层)
    - 实现 `IJob.Execute()`，处理清空回收站任务（通过 `webapi.115.com/rb/clean` 或需求采集中确认的等效端点）。Job 类定义在 Infrastructure 层
    - 记录任务状态与错误摘要
    - 任务完成后统一触发相关缓存失效
    - _需求: 11, 21, 27_ _设计: §5. 后台任务模块 (Quartz.NET Job)_

  - [ ] 8.5 实现 `CloudDriveTranscodeJob` (Quartz IJob - Infrastructure 层)
    - 实现 `IJob.Execute()`，处理视频转码任务与状态回写：通过重复调用 `GET webapi.115.com/files/video?pickcode={pickcode}&share_id=0` 探测可播放性判断转码完成状态。Job 类定义在 Infrastructure 层
    - 记录任务状态、进度与错误摘要
    - 为播放接口返回 `Transcoding` 状态提供数据来源
    - _需求: 15, 21_ _设计: §5. 后台任务模块 (Quartz.NET Job), §4. 视频模块 (Video)_

  - [ ] 8.6 实现 `GetJobStatusQueryHandler` 与 `GetJobListQueryHandler`
    - 实现 `GetJobStatusQueryHandler`：通过 `GetJobStatusInput` 查询任务状态，并校验 `jobId` 归属当前用户与账号上下文
    - 实现 `GetJobListQueryHandler`：获取任务列表；`cloudAccountUid` 为空时查询当前用户下全部账号任务，非空时按账号过滤
    - _需求: 21, 28_ _设计: §5. 后台任务模块 (Quartz.NET Job)_

  - [ ] 8.7 实现后台任务审计与脱敏日志
    - 记录任务创建、状态流转、错误、检查点摘要等可审计信息
    - 确保日志中不包含 Cookie / UID / CID / USSID / 云下载签名密钥等敏感信息明文（通过 `CloudDriveSensitiveFieldsFilter` 统一脱敏）
    - _需求: 26, 28_ _设计: §1. 认证与会话管理模块 (Auth), §5. 后台任务模块 (Quartz.NET Job)_

  - [ ] 8.8 实现后台任务启动恢复协调服务
    - 应用启动时扫描 `CloudDriveJob` 中处于 `Pending` / `Running` 状态的任务记录
    - 对具备 `PayloadJson` + `CheckpointData` 或明确安全重试条件的任务重新调度到 Quartz
    - 对不可恢复任务标记为 `Failed` 并记录失败原因，避免任务永久悬挂
    - _需求: 21, 28_ _设计: §P13: 后台任务重启恢复协调, §5. 后台任务模块 (Quartz.NET Job)_

- [ ] 9. 应用层 - 文件管理 Command/Query Handler
  - [ ] 9.1 实现 `GetFileListQueryHandler` 与 `GetFileDetailQueryHandler`
    - 实现 `GetFileListQueryHandler`：获取指定目录文件列表（分页，默认根目录 `cid=0`）；网页端 API `GET webapi.115.com/files`（必填 `aid=1`）
    - 实现 `GetFileDetailQueryHandler`：获取文件详情（网页端 `GET webapi.115.com/files/file`）
    - 响应字段由 Infrastructure 层归一化适配器转换（`fid → FileId`、`n → Name`、`s → Size`、`fc` 值域转换 → `IsDirectory`）
    - Query Handler 仅执行数据查询，不触发副作用（CD0007）
    - _需求: 4, 6_ _设计: §2. 文件管理模块 (File), §CloudDriveFileManager_

  - [ ] 9.2 实现 `CreateFolderCommandHandler`
    - 新建文件夹（网页端 `POST webapi.115.com/files/add` 或等效端点，具体路径依据需求采集）并复用 `CloudDriveFileNameValidator.ValidateNewFolderName`（255 字符）
    - 成功后失效受影响目录缓存
    - 领域事件由仓储 `Track(entity)` + `DomainEventDispatchBehavior` 自动分发，Handler 内部不手动调用 `IMediator.Publish()`
    - _需求: 7, 27_ _设计: §2. 文件管理模块 (File), §CloudDriveFileManager_

  - [ ] 9.3 实现 `InitUploadCommandHandler`
    - 调用 `ICloudDriveOpenApiGateway.SendUploadInitRequestAsync` 发送 `POST uplb.115.com/3.0/sampleinitupload.php`（body: `userid={uid}&filename={name}&filesize={bytes}&target=U_1_{cid}`），返回 `CloudDriveUploadSessionDto`
    - 通过共享的 `CloudDriveUploadInitContract` 统一初始化协议；秒传时响应含 `file_id` 且无 OSS 凭证 → 直接返回文件信息，非秒传时返回 `host/object/accessid/policy/signature/expire/callback` 等 OSS 直传凭证
    - 处理二次认证（如需）：按签名校验指定的字节范围计算 SHA1 后携带签名参数重新请求（具体流程依据网页端采集证实）
    - 校验 `DirectoryId`、`FileSha1` 非空、上传前置条件与输入参数，并在目标目录不存在时返回明确错误
    - _需求: 7_ _设计: §2. 文件管理模块 (File), §CloudDriveFileManager_

  - [ ] 9.4 实现 `UploadFileCommandHandler` - 同步上传与检查点持久化
    - 执行同步上传流程：`SendUploadInitRequestAsync` → 秒传判断 → 非秒传时调用 `UploadFileAsync` 执行 OSS PostObject 直传（`POST {host}/`，携带 `key/policy/OSSAccessKeyId/signature/callback/file`）→ 解析 callback 透传的 115 业务 JSON → 返回 `FileId`、名称与路径
    - 复用与 `InitUploadCommandHandler` / `CloudDriveUploadJob` 相同的上传初始化契约，避免同步/异步路径使用不同的初始化协议
    - 支持断点续传：当提供 `PickCode` 时优先通过本地恢复检查点重新发起上传（网页端未发现专用远端分片续传接口）
    - 当上传失败且本地恢复资源不可用时，返回上传失败，并清理本地恢复检查点与临时上传文件；若网页端提供取消或废弃上传会话的接口，优先调用该接口回收远端会话
    - 上传成功后失效目标目录缓存
    - _需求: 7, 27_ _设计: §2. 文件管理模块 (File), §CloudDriveFileManager_

  - [ ] 9.5 实现 `UploadFileCommandHandler` - 异步上传编排
    - 当文件大小超过 `LargeFileAsyncThresholdBytes` 或客户端显式请求异步执行时，委托 `CloudDriveJobOrchestrator` 创建后台上传任务
    - 返回 `Accepted = true` 的 `CloudDriveUploadExecutionDto` 与 `JobId`
    - _需求: 7, 21_ _设计: §2. 文件管理模块 (File), §5. 后台任务模块 (Quartz.NET Job)_

  - [ ] 9.6 实现 `GetDownloadUrlQueryHandler`
    - 通过文件的 `pickcode` 调用网页端下载接口（具体路径以 `requirements.md` 采集为准）获取下载地址
    - 返回网页端临时下载 URL 或受控代理下载信息，并将结果标记为临时授权资源（`IsTemporary = true`）
    - 在文件不存在或地址获取失败时返回明确错误原因
    - Query Handler 不触发副作用（CD0007）
    - _需求: 6_ _设计: §2. 文件管理模块 (File), §P9: 临时资源标识_

  - [ ] 9.7 实现 `MoveFilesCommandHandler`
    - 实现文件移动逻辑（网页端 `POST webapi.115.com/files/move`）
    - 同步完成后失效源目录与目标目录缓存
    - 在超过同步执行阈值时委托 `CloudDriveJobOrchestrator` 创建后台任务；同步完成时返回 `Accepted = false`，异步受理时返回 `JobId`
    - _需求: 8, 21, 27_ _设计: §2. 文件管理模块 (File), §5. 后台任务模块 (Quartz.NET Job)_

  - [ ] 9.8 实现 `CopyFilesCommandHandler`
    - 实现文件复制逻辑（网页端 `POST webapi.115.com/files/copy`）
    - 同步完成后失效目标目录缓存
    - 在超过同步执行阈值时委托 `CloudDriveJobOrchestrator` 创建后台任务；同步完成时返回 `Accepted = false`，异步受理时返回 `JobId`
    - _需求: 9, 21, 27_ _设计: §2. 文件管理模块 (File), §5. 后台任务模块 (Quartz.NET Job)_

  - [ ] 9.9 实现 `DeleteFilesCommandHandler`
    - 实现文件删除逻辑（移至回收站，网页端 `POST webapi.115.com/rb/delete` 或等效端点）
    - 同步完成后失效源目录缓存
    - 在超过同步执行阈值时委托 `CloudDriveJobOrchestrator` 创建后台任务；同步完成时返回 `Accepted = false`，异步受理时返回 `JobId`
    - _需求: 10, 21, 27_ _设计: §2. 文件管理模块 (File), §5. 后台任务模块 (Quartz.NET Job)_

  - [ ] 9.10 实现 `RenameFileCommandHandler`
    - 实现文件/文件夹重命名逻辑（网页端 `POST webapi.115.com/files/edit` 或等效端点）并复用 `CloudDriveFileNameValidator.ValidateRenameFileName`（255 字节）
    - 成功后失效受影响目录缓存
    - _需求: 12, 27_ _设计: §2. 文件管理模块 (File), §CloudDriveFileManager_

  - [ ] 9.10a 实现 `ToggleStarCommandHandler`
    - 调用网页端星标切换接口（具体路径以 `requirements.md` 采集为准，一般为 `webapi.115.com/files/star` 或通过 `/files/edit` 传递 `star` 参数）1=星标, 0=取消
    - 成功后失效受影响目录缓存
    - _需求: 13a, 27_ _设计: §2. 文件管理模块 (File)_

  - [ ] 9.11 实现 `SearchFilesQueryHandler`
    - 实现文件搜索（网页端 `GET webapi.115.com/files/search`，分页，关键词验证，返回 `parent_id` 与可选路径）
    - 支持网页端全部筛选参数：`file_label`（文件标签）、`cid`（目录范围）、`gte_day`/`lte_day`（时间范围）、`fc`（1=仅文件夹,2=仅文件；**注意此值域与文件列表 `fc` 不同**）、`type`（文件类型大分类）、`suffix`（后缀名）
    - 搜索请求不走 `CloudDriveMetadataCache`（搜索关键词组合多变且实时性要求高，缓存命中率低），直接经 `ICloudDriveRateLimiter` 限流后通过 `ICloudDriveOpenApiGateway.SendWebApiRequestAsync` 发出
    - 错误字段解析由 `ErrorFieldAdapter` 按 `/files/search` URL 规则处理（`errCode` number + `errMsg`）
    - 仅返回网页端可提供的 `parent_id` 与系统可安全推导的路径信息
    - Query Handler 不触发副作用（CD0007）
    - _需求: 5, 25_ _设计: §2. 文件管理模块 (File), §IErrorFieldAdapter / ErrorFieldAdapter（错误响应字段适配器）_

  - [ ] 9.12 实现回收站查询与还原 Handler
    - 实现 `GetRecycleBinListQueryHandler`：获取回收站列表（网页端 `GET webapi.115.com/rb`，必填 `aid=7` / `cid=0` / `format=json`）；空回收站时网页端不返回 `data` 键，已包含 `data` 时为动态键对象（键为文件 ID string）
    - 实现 `RestoreFromRecycleBinCommandHandler`（网页端 `POST webapi.115.com/rb/revert`）：返回 `CloudDriveOperationResultDto`，并失效受影响目录缓存
    - 在批量还原超过同步执行阈值时委托 `CloudDriveJobOrchestrator` 创建后台任务；同步完成时返回 `Accepted = false`，异步受理时返回 `JobId`
    - _需求: 14, 21, 27_ _设计: §2. 文件管理模块 (File), §5. 后台任务模块 (Quartz.NET Job)_

  - [ ] 9.13 实现回收站彻底删除与清空 Handler
    - 实现 `DeleteFromRecycleBinCommandHandler`（网页端 `POST webapi.115.com/rb/delete`，`ignore_warn=1` 彻底删除）：返回 `CloudDriveOperationResultDto`
    - 在批量彻底删除超过同步执行阈值时委托 `CloudDriveJobOrchestrator` 创建后台任务；同步完成时返回 `Accepted = false`，异步受理时返回 `JobId`
    - 实现 `ClearRecycleBinCommandHandler`（网页端 `webapi.115.com/rb/clean` 或需求采集中确认的等效端点）：返回 `CloudDriveOperationResultDto`；当前实现统一通过 `CloudDriveJobOrchestrator` 创建后台任务，并返回 `Accepted = true` 与 `JobId`
    - _需求: 14, 21_ _设计: §2. 文件管理模块 (File), §5. 后台任务模块 (Quartz.NET Job)_

  - [ ] 9.14 实现 `RefreshDirectoryCommandHandler`
    - 手动刷新目录缓存
    - 支持跳过缓存直接重新拉取网页端元数据；手动刷新仍经 `ICloudDriveRateLimiter` 统一治理
    - _需求: 27_ _设计: §2. 文件管理模块 (File), §CloudDriveMetadataCache_

  - [ ] 9.15 编写文件管理 Handler 单元测试
    - 测试文件列表分页逻辑与字段归一化映射
    - 测试文件名验证（创建文件夹 255 字符边界、重命名 255 字节边界）
    - 测试缓存失效触发逻辑
    - 测试上传成功后返回路径并触发目标目录缓存失效
    - 测试批量操作/清空回收站的同步/异步分流条件，以及 `Accepted` / `JobId` 返回语义
    - 测试回收站空响应（无 `data` 键）的容错处理
    - _需求: 4, 5, 6, 7, 8, 9, 10, 11, 12, 13a, 14, 21, 27_ _设计: §2. 文件管理模块 (File)_

- [ ] 10. 应用层 - 预览、视频、云下载、分享与用户聚合 Handler
  - [ ] 10.1 实现 `GetImagePreviewQueryHandler` 与 `GetDocPreviewQueryHandler`
    - 实现 `GetImagePreviewQueryHandler`：获取图片预览信息，返回网页端临时地址或受控代理地址，并仅支持 `JPG/JPEG/PNG/GIF/BMP/WEBP`
    - 实现 `GetDocPreviewQueryHandler`：获取文档预览信息（按渲染方式分类：Direct/ThirdParty/NotAvailable），返回网页端临时地址或受控代理地址
    - 实现文件扩展名到预览类型的路由逻辑
    - 对文件不存在和格式不支持场景返回明确错误；Office 第三方预览服务未配置时返回 `RenderMode = NotAvailable`
    - Query Handler 不触发副作用（CD0007）
    - _需求: 14, 16_ _设计: §3. 预览模块 (Preview)_

  - [ ] 10.2 编写属性测试 - 预览类型路由正确性
    - **属性 P7: 预览类型路由正确性**
    - 验证各文件扩展名被正确分类到对应的 `DocRenderMode`
    - 验证视频文件不会被误分类为图片或文档预览，而是进入视频播放链路
    - 验证 Office 文档在未配置第三方预览服务时返回 `NotAvailable`
    - 验证不支持的格式返回错误
    - _需求: 14, 15, 16_

  - [ ] 10.3 实现 `GetVideoPlayUrlCommandHandler`
    - 通过文件的 `pickcode` 调用 `GET webapi.115.com/files/video?pickcode={pickcode}&share_id=0` 获取播放地址（含多清晰度与网页端播放元数据）
    - 若视频尚未完成转码（通过重复调用上述接口的可播放性探测判定）则自动提交转码并返回 `Transcoding` 状态；本次调用成功创建后台转码任务时必须返回 `JobId`
    - 对非视频类型或文件不存在场景返回明确错误
    - `share_id=0` 必带（非分享场景）；`pickcode` 为网页端接口的核心定位参数
    - 使用 Command 而非 Query，因为该操作可能触发转码提交副作用（创建后台任务），遵循 CD0007 约束
    - _需求: 15_ _设计: §4. 视频模块 (Video), §5. 后台任务模块 (Quartz.NET Job)_

  - [ ] 10.4 实现 `SubmitTranscodeCommandHandler`
    - 通过文件的 `pickcode` 提交视频转码：`GET webapi.115.com/files/video_push?pickcode={pickcode}&op=vip_push&format=json`（备选路径：`GET webapi.115.com/movies/transcode?pickcode={pickcode}&format=json`）
    - 统一复用 `CloudDriveJobOrchestrator` 创建转码任务
    - 校验文件存在且类型为视频
    - _需求: 15, 21_ _设计: §4. 视频模块 (Video), §5. 后台任务模块 (Quartz.NET Job)_

  - [ ] 10.5 实现 `GetSubtitlesQueryHandler`
    - 通过文件的 `pickcode` 调用 `GET webapi.115.com/movies/subtitle` 获取视频字幕列表，返回 `CloudDriveSubtitleListDto`（包含自动加载字幕与字幕项列表）
    - 映射 `caption_map_id`、`is_caption_map`、`sync_time`、`sha1`、`file_id` 等公开字段
    - 处理字幕不存在、自动加载字幕缺失或暂不可用的返回语义
    - 错误字段解析由 `ErrorFieldAdapter` 按 `/movies/subtitle` URL 规则处理（大写 `errNo` + `error`）
    - Query Handler 不触发副作用（CD0007）
    - _需求: 15, 25_ _设计: §4. 视频模块 (Video), §IErrorFieldAdapter / ErrorFieldAdapter（错误响应字段适配器）_

  - [ ] 10.6 实现 `GetVideoPlayProgressQueryHandler`
    - 通过文件的 `pickcode` 调用 `GET webapi.115.com/files/history?pick_code={pickcode}&fetch=one&category=1&format=json` 获取网页端记忆的播放进度
    - 若仅有 `FileId`，先通过 `CloudDriveFileManager` 解析出 `pickcode`
    - 返回 `CloudDriveVideoPlayProgressDto`（含 `file_id`、`file_name`、`hash`、`pickcode`、已播放时长、`add_time` 记录添加时间戳）
    - Query Handler 不触发副作用（CD0007）
    - _需求: 18_ _设计: §4. 视频模块 (Video)_

  - [ ] 10.7 实现 `RememberVideoPlayProgressCommandHandler`
    - 通过文件的 `pickcode` 调用 `POST webapi.115.com/files/history`（form: `pick_code={pickcode}&time={seconds}&category=1&op=update`）记忆播放进度
    - 若仅有 `FileId`，先通过 `CloudDriveFileManager` 解析出 `pickcode`
    - 校验 `Time >= 0`
    - 支持 `WatchEnd` 标记播放完毕
    - _需求: 18_ _设计: §4. 视频模块 (Video)_

  - [ ] 10.8 补充 `GetVideoPlayUrlCommandHandler` 的受限状态与用途标识
    - 当网页端返回 4K / 原画受限状态或升级引导资源时，映射明确的受限原因到 `RestrictionReason`
    - 在返回 `CloudDriveVideoPlayDto` 时显式设置 `IsTemporary = true`
    - 填充 `UsageRestrictionNotice`，明确该播放资源仅限当前已绑定用户在系统内短时使用，不得视为永久公开外链
    - _需求: 15, 16_ _设计: §4. 视频模块 (Video), §P9: 临时资源标识_

  - [ ] 10.9 实现云下载查询 Handler（Offline Query）
    - 实现 `GetOfflineTaskListQueryHandler`：获取云下载任务列表，调用 `ICloudDriveOpenApiGateway.SendOfflineApiRequestAsync` 发送 `115.com/web/lixian/?ct=lixian&ac=task_lists` 请求（自动通过 `ICookieSignatureStrategy` 生成 `sign + time + uid` 签名）
    - 响应字段归一化：`total_count → Total`、`failed_count → Failed`、`finished_count → Completed`、`downloading_count → Active`
    - 实现 `GetOfflineQuotaQueryHandler`：获取云下载空间配额与统计（`115.com/web/lixian/?ct=lixian&ac=get_quota_package_info` 或需求采集中确认的等效端点）
    - Query Handler 不触发副作用（CD0007）
    - _需求: 20_ _设计: §6. 云下载模块 (Offline), §ICookieSignatureStrategy（云下载签名预留扩展点）_

  - [ ] 10.10 实现云下载写操作 Handler（Offline Commands）
    - 实现 `AddOfflineTaskCommandHandler`：接收 `AddOfflineTaskInput`（`Urls: List<string>`, `SavePath?`），批量提交云下载任务（`115.com/web/lixian/?ct=lixian&ac=add_task_url` 或 `add_task_urls`）；签名由 `ICookieSignatureStrategy.GenerateAsync` 生成
    - 实现 `CancelOfflineTaskCommandHandler`：删除（取消）指定任务（`?ct=lixian&ac=del_task`）
    - 实现 `ClearOfflineTasksCommandHandler`：清空已完成/已失败任务（`?ct=lixian&ac=task_clear`）
    - 当 `ICookieSignatureStrategy` 未提供有效实现时返回 `Result.Failure` 带 `offline_signature_not_available` 错误码，而非抛出未处理异常
    - 错误字段解析由 `ErrorFieldAdapter` 按 `115.com/web/lixian/` URL 规则处理
    - _需求: 21_ _设计: §6. 云下载模块 (Offline), §ICookieSignatureStrategy（云下载签名预留扩展点）, §IErrorFieldAdapter / ErrorFieldAdapter（错误响应字段适配器）_

  - [ ] 10.11 实现分享管理 Handler（Share Queries）
    - 实现 `GetMySharesQueryHandler`：查询我的分享列表（`GET webapi.115.com/share/slist`，必填 `format=json`）
    - 实现 `GetShareSendsQueryHandler`：查询分享发送记录（`GET webapi.115.com/share/send`，必填 `format=json`）
    - 实现 `GetShareReceivesQueryHandler`：查询分享接收记录（`GET webapi.115.com/share/receive`，必填 `format=json`）
    - 错误字段解析由 `ErrorFieldAdapter` 按 URL 规则处理：`slist` 使用 `errNo`；`send` / `receive` 优先读 `errNo`，fallback `errno`
    - 当前需求 22 采集样本均为失败响应（低置信度，见 `requirements.md:324-335`）；解析失败时 Handler 返回 `share_sample_unavailable` 业务错误，待联调获得真实可用样本后补齐字段映射
    - Query Handler 不触发副作用（CD0007）
    - _需求: 22, 25_ _设计: §7. 分享管理模块 (Share), §IErrorFieldAdapter / ErrorFieldAdapter（错误响应字段适配器）_

  - [ ] 10.12 实现用户中心聚合 Handler（UserAggregation Queries）
    - 实现 `GetUserSpaceQueryHandler`：获取用户空间详情，通过 `UserAggregationService` 并行调用 `webapi.115.com/user/info` 与相关接口
    - 实现 `GetUserVipQueryHandler`：获取 VIP 详情与特权，通过 `UserAggregationService` 并行调用 `webapi.115.com/user/vip_limit` + `vip.115.com/*` 多源接口
    - Query Handler 不触发副作用（CD0007）— 详细实现举见 `UserAggregationService`（任务 10.13）
    - _需求: 23_ _设计: §8. 用户中心聚合模块 (UserAggregator)_

  - [ ] 10.13 实现 `UserAggregationService`（并行多源聚合）
    - 实现 `AggregateUserInfoAsync(userId, cloudAccountUid, ct)`：使用 `Task.WhenAll` 并行调用多源接口（`webapi.115.com/user/info`、`webapi.115.com/user/vip_limit`、`webapi.115.com/user/index_info`、`vip.115.com/*`、`my.115.com/*`、`pay.115.com/`）
    - 实施敏感字段过滤：通过 `CloudDriveSensitiveFieldsFilter` 统一注出需求 23 的 34 个黑名单敏感字段（手机号、邮箱、身份证、住址、生日、人口、职业、兴趣等 8 类）
    - VIP 响应中 `00_balance` 字段（以数字开头）因 C# 属性命名限制，映射时统一归一化为 `Balance`；其余数字开头字段也需遵循此归一化策略
    - 聚合失败策略：任一子接口失败时该字段置空并在日志中记录 `sub_endpoint_failed` 条目（含脱敏的错误摘要）；只有所有子接口都失败时才返回 `Result.Failure`
    - 错误字段解析由 `ErrorFieldAdapter` 按 URL 规则处理：`user/vip_limit` 用 string 型 `code`；`user/index_info` 用 string 型 `code`；`pay.115.com/` 用 `msg`（非 `message`）
    - _需求: 23, 25, 26_ _设计: §8. 用户中心聚合模块 (UserAggregator), §UserAggregationService, §IErrorFieldAdapter / ErrorFieldAdapter（错误响应字段适配器）_

- [ ] 11. 检查点 - 应用层完成验证
  - 当前阶段通过条件：
    - 认证（Cookie 会话管理）、文件、预览、视频、云下载、分享、用户聚合、任务 Handler 的必做测试通过
    - 大文件上传、批量操作、清空回收站、视频转码的同步/异步边界已可验证
    - 临时资源 DTO、`Transcoding` 状态返回与账号隔离逻辑已完成联调自检
    - Cookie 失效冷却期、`CookieInvalidated` 领域事件分发、登录态探测路径已完成联调自检
    - 视频播放进度读写、4K/原画受限状态与受限用途提示已完成联调自检
    - `UserAggregationService` 多源并行聚合与 34 个敏感字段过滤已完成联调自检
    - 云下载签名（`ICookieSignatureStrategy`）未提供实现时返回 `offline_signature_not_available` 业务错误
    - 所有 Command Handler 通过 `TransactionBehavior` 自动管理 SqlSugar 事务
    - 所有输入 DTO 通过 `ValidationBehavior` + FluentValidation 自动校验

- [ ] 12. 表现层 - API 控制器
  - Controller 仅通过 `ISender` / `IMediator.Send()` 分发 Command/Query，严禁直接注入仓储、ORM 客户端或领域服务（CD0003）
  - 除运维 Cookie 注入、登录态探测、已绑定账号列表查询以及任务列表全量查询外，所有已授权端点均需通过 query 或 body 显式传递 `cloudAccountUid`；任务列表查询留空时表示当前用户下全部账号任务，不进行默认账号推断
  - 为 Controller / Endpoint 适配层实现统一 `Result` → HTTP 响应映射：`Result.Failure` 输出非 2xx 的 `application/problem+json`，不得以 HTTP 200 表示失败
  - 日志输出与 ProblemDetails 响应统一通过 `CloudDriveSensitiveFieldsFilter` 脱敏 Cookie / UID / CID / USSID / 签名密钥
  - [ ] 12.1 实现 `CloudDriveAuthAdminController`（运维专用端点）
    - 所有端点需通过策略或授权属性（如 `[Authorize(Roles = "Admin")]`）限制访问；**严禁** 暴露给普通用户
    - POST /api/cloud-drive/admin/cookie-sessions → `IMediator.Send(new UpsertCookieSessionCommand(this.GetAuthenticatedUserId(), input))`（body: `UpsertCookieSessionInput` 含 UID/CID/USSID + ExtraFields）
    - POST /api/cloud-drive/admin/cookie-sessions/{cloudAccountUid}/cooldown → `IMediator.Send(new EnterCookieCooldownCommand(this.GetAuthenticatedUserId(), input))`（body: Reason, CooldownSeconds?）
    - DELETE /api/cloud-drive/admin/cookie-sessions/{cloudAccountUid} → `IMediator.Send(new RevokeCookieSessionCommand(this.GetAuthenticatedUserId(), input))`
    - 请求 body 经过 `CloudDriveSensitiveFieldsFilter` 确保 Cookie 原文不进入日志；ProblemDetails 响应不返回 Cookie 或加密密文
    - _需求: 1, 2_ _设计: §1. 认证与会话管理模块 (Auth), §CookieSessionManager_

  - [ ] 12.2 实现 `CloudDriveAuthController`（已绑定用户可访问）
    - GET /api/cloud-drive/auth/accounts → `IMediator.Send(new GetBoundAccountsQuery(this.GetAuthenticatedUserId()))`（不要求 `cloudAccountUid`，返回当前用户下所有已绑定账号摘要）
    - GET /api/cloud-drive/auth/login-status?cloudAccountUid={cloudAccountUid} → `IMediator.Send(new GetLoginStatusQuery(this.GetAuthenticatedUserId(), input))`（登录态探测，对应需求 3 的 `/api/auth/check`）
    - GET /api/cloud-drive/auth/user-info?cloudAccountUid={cloudAccountUid} → `IMediator.Send(new GetUserInfoQuery(this.GetAuthenticatedUserId(), input))`（用户信息多源聚合）
    - 不再提供 OAuth 的 /authorize、/pkce/session、/pkce/session/{sessionId}/status、/callback、/disconnect 端点 — Cookie 注入与撤销由运维端点（`CloudDriveAuthAdminController`）处理
    - _需求: 3, 19, 23_ _设计: §1. 认证与会话管理模块 (Auth), §LoginCheckHandler（需求 3 登录态检测）, §8. 用户中心聚合模块 (UserAggregator)_

  - [ ] 12.3 实现 `CloudDriveFileController` - 查询类端点
    - GET /api/cloud-drive/files?cloudAccountUid={cloudAccountUid}&directoryId={directoryId?}&offset={offset}&limit={limit}&type={type?}&suffix={suffix?}&asc={asc?}&orderBy={orderBy?}&customOrder={customOrder?}&stdir={stdir?}&star={star?}&cur={cur?}&showDir={showDir?} → `IMediator.Send(new GetFileListQuery(this.GetAuthenticatedUserId(), input))`
    - GET /api/cloud-drive/files/{fileId}?cloudAccountUid={cloudAccountUid} → `IMediator.Send(new GetFileDetailQuery(this.GetAuthenticatedUserId(), input))`
    - GET /api/cloud-drive/files/search?cloudAccountUid={cloudAccountUid}&keyword={keyword}&offset={offset}&limit={limit}&fileLabel={fileLabel?}&directoryId={directoryId?}&gteDay={gteDay?}&lteDay={lteDay?}&fc={fc?}&type={type?}&suffix={suffix?} → `IMediator.Send(new SearchFilesQuery(this.GetAuthenticatedUserId(), input))`
    - GET /api/cloud-drive/files/{fileId}/download?cloudAccountUid={cloudAccountUid} → `IMediator.Send(new GetDownloadUrlQuery(this.GetAuthenticatedUserId(), input))`
    - 所有查询端点均需通过 query parameter 显式传递 `cloudAccountUid`
    - _需求: 4, 5, 6_ _设计: §2. 文件管理模块 (File)_

  - [ ] 12.4 实现 `CloudDriveFileController` - 写入与上传端点
    - POST /api/cloud-drive/files/folder → `IMediator.Send(new CreateFolderCommand(this.GetAuthenticatedUserId(), input))`
    - POST /api/cloud-drive/files/upload/init → `IMediator.Send(new InitUploadCommand(this.GetAuthenticatedUserId(), input))`
    - POST /api/cloud-drive/files/upload → `IMediator.Send(new UploadFileCommand(this.GetAuthenticatedUserId(), input, fileStream))`
    - 上传端点使用 `multipart/form-data` 接收 `IFormFile file`、`cloudAccountUid`、`directoryId`、`fileSha1`（文件 SHA1）与可选 `preSha1`、`pickCode`、`forceAsync`
    - Controller 将 `IFormFile` 的 `Stream` 传递给 `UploadFileCommand`，由 `UploadFileCommandHandler` 通过 `IUploadFileStagingService` 在应用层完成临时文件落盘与异步上传编排（保持 Controller 轻量）
    - POST /api/cloud-drive/files/move → `IMediator.Send(new MoveFilesCommand(this.GetAuthenticatedUserId(), input))`
    - POST /api/cloud-drive/files/copy → `IMediator.Send(new CopyFilesCommand(this.GetAuthenticatedUserId(), input))`
    - POST /api/cloud-drive/files/delete → `IMediator.Send(new DeleteFilesCommand(this.GetAuthenticatedUserId(), input))`
    - PUT /api/cloud-drive/files/{fileId}/rename → `IMediator.Send(new RenameFileCommand(this.GetAuthenticatedUserId(), input))`
    - POST /api/cloud-drive/files/refresh → `IMediator.Send(new RefreshDirectoryCommand(this.GetAuthenticatedUserId(), input))`
    - PUT /api/cloud-drive/files/{fileId}/star → `IMediator.Send(new ToggleStarCommand(this.GetAuthenticatedUserId(), input))`（body 中携带 `cloudAccountUid`、`star`）
    - _需求: 7, 8, 9, 10, 12, 13a, 21, 27_ _设计: §2. 文件管理模块 (File), §5. 后台任务模块 (Quartz.NET Job)_

  - [ ] 12.5 实现 `CloudDriveRecycleBinController`
    - GET /api/cloud-drive/recycle-bin?cloudAccountUid={cloudAccountUid}&offset={offset}&limit={limit} → `IMediator.Send(new GetRecycleBinListQuery(this.GetAuthenticatedUserId(), input))`
    - POST /api/cloud-drive/recycle-bin/restore → `IMediator.Send(new RestoreFromRecycleBinCommand(this.GetAuthenticatedUserId(), input))`（body 中携带 `cloudAccountUid`）
    - POST /api/cloud-drive/recycle-bin/delete → `IMediator.Send(new DeleteFromRecycleBinCommand(this.GetAuthenticatedUserId(), input))`（body 中携带 `cloudAccountUid`）
    - POST /api/cloud-drive/recycle-bin/clear → `IMediator.Send(new ClearRecycleBinCommand(this.GetAuthenticatedUserId(), input))`（body 中携带 `cloudAccountUid`）
    - _需求: 14, 21_ _设计: §2. 文件管理模块 (File), §5. 后台任务模块 (Quartz.NET Job)_

  - [ ] 12.6 实现 `CloudDrivePreviewController`
    - GET /api/cloud-drive/preview/image/{fileId}?cloudAccountUid={cloudAccountUid} → `IMediator.Send(new GetImagePreviewQuery(this.GetAuthenticatedUserId(), input))`
    - GET /api/cloud-drive/preview/doc/{fileId}?cloudAccountUid={cloudAccountUid} → `IMediator.Send(new GetDocPreviewQuery(this.GetAuthenticatedUserId(), input))`
    - _需求: 14, 16_ _设计: §3. 预览模块 (Preview)_

  - [ ] 12.7 实现 `CloudDriveVideoController`
    - POST /api/cloud-drive/video/{fileId}/play → `IMediator.Send(new GetVideoPlayUrlCommand(this.GetAuthenticatedUserId(), input))`（body 中携带 `cloudAccountUid`；使用 POST 因为可能触发转码副作用）
    - POST /api/cloud-drive/video/{fileId}/transcode → `IMediator.Send(new SubmitTranscodeCommand(this.GetAuthenticatedUserId(), input))`（body 中携带 `cloudAccountUid`）
    - GET /api/cloud-drive/video/{fileId}/subtitles?cloudAccountUid={cloudAccountUid} → `IMediator.Send(new GetSubtitlesQuery(this.GetAuthenticatedUserId(), input))`
    - _需求: 15_ _设计: §4. 视频模块 (Video)_

  - [ ] 12.8 补充 `CloudDriveVideoController` 播放进度端点
    - GET /api/cloud-drive/video/{fileId}/progress?cloudAccountUid={cloudAccountUid} → `IMediator.Send(new GetVideoPlayProgressQuery(this.GetAuthenticatedUserId(), input))`
    - POST /api/cloud-drive/video/{fileId}/progress → `IMediator.Send(new RememberVideoPlayProgressCommand(this.GetAuthenticatedUserId(), input))`（body 中携带 `cloudAccountUid`、`time`、`watchEnd`）
    - _需求: 18_ _设计: §4. 视频模块 (Video)_

  - [ ] 12.9 实现 `CloudDriveOfflineController`
    - GET /api/cloud-drive/offline/tasks?cloudAccountUid={cloudAccountUid}&offset={offset}&limit={limit} → `IMediator.Send(new GetOfflineTaskListQuery(this.GetAuthenticatedUserId(), input))`
    - GET /api/cloud-drive/offline/quota?cloudAccountUid={cloudAccountUid} → `IMediator.Send(new GetOfflineQuotaQuery(this.GetAuthenticatedUserId(), input))`
    - POST /api/cloud-drive/offline/tasks → `IMediator.Send(new AddOfflineTaskCommand(this.GetAuthenticatedUserId(), input))`（body: `Urls: List<string>`, `SavePath?`, `cloudAccountUid`）
    - DELETE /api/cloud-drive/offline/tasks/{taskId} → `IMediator.Send(new CancelOfflineTaskCommand(this.GetAuthenticatedUserId(), input))`（body 或 query 中携带 `cloudAccountUid`）
    - POST /api/cloud-drive/offline/tasks/clear → `IMediator.Send(new ClearOfflineTasksCommand(this.GetAuthenticatedUserId(), input))`（body 中携带 `cloudAccountUid`）
    - 当签名实现未就绪时返回 HTTP 503 + `offline_signature_not_available` ProblemDetails
    - _需求: 20, 21_ _设计: §6. 云下载模块 (Offline)_

  - [ ] 12.10 实现 `CloudDriveShareController`
    - GET /api/cloud-drive/shares?cloudAccountUid={cloudAccountUid}&offset={offset}&limit={limit} → `IMediator.Send(new GetMySharesQuery(this.GetAuthenticatedUserId(), input))`
    - GET /api/cloud-drive/shares/sends?cloudAccountUid={cloudAccountUid}&offset={offset}&limit={limit} → `IMediator.Send(new GetShareSendsQuery(this.GetAuthenticatedUserId(), input))`
    - GET /api/cloud-drive/shares/receives?cloudAccountUid={cloudAccountUid}&offset={offset}&limit={limit} → `IMediator.Send(new GetShareReceivesQuery(this.GetAuthenticatedUserId(), input))`
    - 采集样本不可用时返回 HTTP 503 + `share_sample_unavailable` ProblemDetails，提示后续联调补齐
    - _需求: 22_ _设计: §7. 分享管理模块 (Share)_

  - [ ] 12.11 实现 `CloudDriveUserCenterController`
    - GET /api/cloud-drive/user-center/space?cloudAccountUid={cloudAccountUid} → `IMediator.Send(new GetUserSpaceQuery(this.GetAuthenticatedUserId(), input))`
    - GET /api/cloud-drive/user-center/vip?cloudAccountUid={cloudAccountUid} → `IMediator.Send(new GetUserVipQuery(this.GetAuthenticatedUserId(), input))`
    - 响应经 `CloudDriveSensitiveFieldsFilter` 过滤需求 23 的 34 个黑名单敏感字段，保证不泄漏手机号/邮箱/身份证等
    - _需求: 23, 26_ _设计: §8. 用户中心聚合模块 (UserAggregator)_

  - [ ] 12.12 实现 `CloudDriveJobController`
    - GET /api/cloud-drive/jobs/{jobId}?cloudAccountUid={cloudAccountUid} → `IMediator.Send(new GetJobStatusQuery(this.GetAuthenticatedUserId(), input))`
    - GET /api/cloud-drive/jobs?cloudAccountUid={cloudAccountUid?}&status={status?}&offset={offset}&limit={limit} → `IMediator.Send(new GetJobListQuery(this.GetAuthenticatedUserId(), input))`（`cloudAccountUid` 留空时查询全部账号）
    - _需求: 21, 28_ _设计: §5. 后台任务模块 (Quartz.NET Job)_

- [ ] 13. 模块注册与集成
  - [ ] 13.1 创建 `IServiceCollection` 扩展方法注册所有依赖
    - 创建 `AddCloudDrive(this IServiceCollection services, IConfiguration configuration)` 扩展方法（CD0009）
    - 内部调用 `AddCloudDriveInfrastructure()` 注册 Named HttpClient（`Cloud115Web` / `Cloud115My` / `Cloud115Vip` / `Cloud115Pay` / `Cloud115Offline`）、Polly 策略、仓储
    - 注册应用层服务（`CookieSessionManager`、`CloudDriveFileManager`、`UserAggregationService`、`CloudDriveJobOrchestrator`）为 Scoped
    - 确认基础设施层实现（`CloudDriveMetadataCache`、`CloudDriveRateLimiter`、`ErrorFieldAdapter`、`DataProtectionCloudDriveSecretProtector`、`LoggingCookieInvalidationNotifier`、`NotImplementedCookieSignatureStrategy`、`CloudDriveSensitiveFieldsFilter`）已在 `AddCloudDriveInfrastructure()` 中注册（见任务 4.10）
    - 注册后台任务启动恢复协调服务（HostedService 或等效启动任务），在应用启动后基于 `CloudDriveJob` 记录重建可恢复任务调度
    - 115 模块的 Command/Query Handler 和 FluentValidation 验证器均位于现有 Application 项目中，由框架已有的 `AddApplicationServices()` 统一扫描注册（`RegisterServicesFromAssembly` 和 `AddValidatorsFromAssembly` 已覆盖 Application 程序集），无需额外调用 `AddMediatR()` 或 `AddValidatorsFromAssembly()`
    - 通过 `AddQuartz()` 注册 Quartz.NET Job 类型（`CloudDriveUploadJob`、`CloudDriveBatchOperationJob`、`CloudDriveClearRecycleBinJob`、`CloudDriveTranscodeJob`）
    - 配置 `CloudDriveAppConfig` 的 Options 绑定（`services.Configure<CloudDriveAppConfig>(configuration.GetSection("CloudDrive"))`）
    - 实现启动时配置完整性校验逻辑：在 `AddCloudDrive()` 中通过 `OptionsBuilder<CloudDriveAppConfig>.Validate()` 或手动校验检查核心字段：`CookieEncryptionKeyName`、`WebApiBaseUrl`、（如启用云下载）`SignatureKeyName`；校验失败时记录 `LogLevel.Error` 日志并将 `CloudDriveFeatureEnabled` 设为 `false`，但不阻止应用启动
    - 运行期保护：所有 115 相关 Handler 在入口处通过 `IOptions<CloudDriveAppConfig>` 检查 `CloudDriveFeatureEnabled`，若为 `false` 则抛出 `CloudDriveConfigurationException`
    - 运维需通过受保护的配置（Azure KeyVault / Secret Manager / 环境变量加密）提供 Cookie 加密 key ring 与云下载签名密钥，不得以明文写入 `appsettings.json`
    - 在 `Program.cs` 中调用 `builder.Services.AddCloudDrive(builder.Configuration)`
    - _需求: 2, 26_ _设计: §9. 基础设施组件, §P8: 配置完整性校验_

  - [ ] 13.2 配置限流与缓存协同
    - 将 `CloudDriveRateLimiter` 集成到 `CloudDriveApiClient` 的请求管道中（`SendWebApiRequestAsync` / `SendUserCenterRequestAsync` / `SendOfflineApiRequestAsync` 入口统一 `AcquireAsync`）
    - 配置 `CloudDriveMetadataCache` 使用 `IDistributedCache` + `DistributedCacheExtensions`
    - 配置 `RateLimitOverflowStrategy`、队列长度、限流/瞬态异常重试参数，并输出排队/拒绝/重试/失败计数到日志与 metrics
    - 确保限流器与缓存协同工作，且按 `UserId + CloudAccountUid` 维度隔离，减少重复高频 API 调用
    - 确保仅幂等请求进入自动重试链路，非幂等写请求遵循设计定义的重试边界
    - 云下载请求通过独立的 `Cloud115Offline` 限流桶隔离，避免与普通读请求竞争配额
    - _需求: 17, 24, 27_ _设计: §CloudDriveRateLimiter, §CloudDriveMetadataCache_

  - [ ] 13.3 执行模块级装配验证
    - 验证配置缺失时启动期校验与运行期保护同时生效（`CookieEncryptionKeyName` / `WebApiBaseUrl` 缺失即可触发）
    - 验证主要 Handler、Controller、Quartz Job 可从容器成功解析
    - 验证 `ErrorFieldAdapter` 注册表在启动时完整加载 `requirements.md:454-470` 对应的全部错误字段变体
    - 验证缓存命中请求不会触发重复高频官方调用，手动刷新时绕过旧缓存且仍受限流器治理
    - 验证 Controller 仅通过 `ISender` 分发，未直接注入仓储或领域服务（CD0003）
    - 验证 `CloudDriveAuthAdminController` 端点受管理员策略保护，Cookie 原文不进入 ProblemDetails 响应
    - 验证 `Result.Failure` 会被映射为非 2xx `ProblemDetails`，不会以 HTTP 200 返回失败
    - 验证 `CloudDriveSensitiveFieldsFilter` 对 Cookie / UID / CID / USSID / 签名密钥在日志中的脱敏生效
    - 验证应用重启后后台任务恢复协调服务会重新调度可恢复任务或将不可恢复任务收敛为失败
    - _需求: 2, 17, 21, 25, 26, 27_ _设计: §P8: 配置完整性校验, §P13: 后台任务重启恢复协调, §IErrorFieldAdapter / ErrorFieldAdapter（错误响应字段适配器）_

- [ ] 14. 最终检查点 - 全部测试通过
  - 当前阶段通过条件：
    - 所有未标记 `*` 的任务完成且测试通过
    - 需求、设计、任务三份文档与实现计划保持一致（Cookie 会话认证方案；不使用 OAuth 2.0 / AppID / AppSecret / PKCE）
    - 至少完成一轮围绕 Cookie 注入、登录态探测、文件浏览、上传、后台任务、预览/播放、云下载、分享、用户聚合的端到端走查
    - 架构约束验证：Domain 层无基础设施依赖、Application 层无直连 ORM/缓存/HTTP、Controller 仅通过 ISender 分发
    - 安全验证：Cookie 存储经加密、日志输出经 `CloudDriveSensitiveFieldsFilter` 脱敏、运维端点受管理员授权保护

## 备注

- 标记 `*` 的任务为增强型测试任务；未标记 `*` 的正确性测试任务为上线前必做
- 每个任务通过 `_需求: X_` 引用具体需求编号、通过 `_设计: §组件名_` 引用 `design.md` 对应组件，确保双向可追溯（遵循 `spec-format.md:131, 138`）
- 检查点任务用于增量验证，确保每个阶段的代码质量
- 属性测试验证设计文档中定义的正确性属性（P1-P13）
- 所有实现遵循 .NET 10 DDD 四层架构 + MediatR CQRS 模式，职责清晰分离
- 架构约束编号（CD0001-CD0009）引用自 `ddd-layer-architecture.md` 规范
- Quartz.NET Job 实现位于 Infrastructure 层，通过 `IJob.Execute()` 入口从 `JobDataMap` 加载 `JobId`
- 对象映射统一使用 `ToDto()` / `ToEntity()` 静态扩展方法，禁止使用 AutoMapper（CD0004/CD0008）
- 认证方案：本模块采用 **Cookie 会话认证**（UID/CID/USSID + 可选额外字段），运维通过受保护端点注入；**严禁** 引入 OAuth 2.0 / PKCE / 授权码流程相关代码与依赖
- 敏感字段脱敏：Cookie 原文、云下载签名密钥、`CloudDriveAppConfig` 中的 `CookieEncryptionKeyName` / `SignatureKeyName` 等敏感信息在日志、异常堆栈、ProblemDetails 响应中统一通过 `CloudDriveSensitiveFieldsFilter` 脱敏
