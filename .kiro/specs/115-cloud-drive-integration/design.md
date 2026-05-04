# 设计文档：115 网盘集成

## 概述

本设计文档描述 115 网盘集成功能的技术架构与实现方案。系统基于 .NET 10 原生技术栈，采用 DDD 四层架构 + MediatR CQRS 模式，通过 **Cookie 会话认证机制**（UID/CID/USSID）对接 **115 网页端 API**（`webapi.115.com`、`my.115.com`、`vip.115.com`、`pay.115.com`、`115.com/web/lixian` 等），为用户提供文件管理、预览播放、云下载、分享管理、后台任务等能力。

> **认证方案声明**：本系统不使用 OAuth 2.0、AppID/AppSecret、授权码或 PKCE 流程。Cookie 由运维/管理员通过受保护的配置注入与轮换，系统不主动发起授权流程。当 Cookie 失效时，系统仅标记状态、进入冷却期并发布 `CookieInvalidated` 领域事件，不尝试自动恢复。

> **框架依赖说明：** 本模块构建在 `115-cloud-drive-net10` 定义的 .NET 10 原生框架之上，复用其 MediatR CQRS 分发、Pipeline Behavior（Validation / Transaction / Logging）、SqlSugar 仓储基类、Quartz.NET 调度、IDistributedCache JSON 封装、FluentValidation 集成、全局异常中间件 + ProblemDetails 等基础设施。

> **CQRS 返回模式说明：** 本模块所有 Command 使用框架定义的 `ICommand` / `ICommand<TResponse>` 接口，所有 Query 使用 `IQuery<TResponse>` 接口。Handler 统一返回 `Result` / `Result<T>`，业务级错误（如文件不存在、名称非法）通过 `Result.Failure(error)` 返回；基础设施级异常（如 `CloudDriveNetworkException`、`CloudDriveConfigurationException`）仍然抛出，由全局异常中间件统一处理。Command 使用 `sealed record` 定义，Handler 使用 `internal sealed` 修饰。

> **表现层失败响应说明：** WebApi Controller / Endpoint 适配层不得直接将 `Result.Failure(...)` 作为 HTTP 200 响应体返回；必须依据 `Error.Code` 通过集中映射输出非 2xx 的 `ProblemDetails`。仅抛出的异常继续由全局异常中间件处理。

### 设计目标

- 遵循 .NET 10 DDD 四层架构（Domain / Application / Infrastructure / WebApi），职责清晰分离
- 通过 **Cookie 会话机制**对接 115 网页端 API，Cookie 由运维注入，系统不主动发起授权流程
- 提供文件 CRUD、搜索、回收站、预览、视频播放、云下载、分享管理与多源用户信息聚合等完整能力
- 内置频控限流、缓存、后台任务机制，降低 API 调用频率与风控风险
- 统一错误响应字段适配（errNo / errCode / errno / code / msg / message 等变体），由 `ErrorFieldAdapter` 按 URL 路径定向处理
- 严格遵守认证与响应内容的合规要求，不在日志/持久化/对外响应中暴露 Cookie 完整值或敏感用户字段

### 关键设计决策

| 决策 | 选择 | 理由 |
|------|------|------|
| 认证模式 | **Cookie 会话认证**（UID/CID/USSID + 可选额外字段），管理员注入 | 对接 115 网页端原生 API，避开开放平台授权链路 |
| Cookie 存储 | 通过 `ICloudDriveSecretProtector` 加密持久化至数据库 | 支持单账号 Cookie 安全存储，避免明文泄露 |
| 账号隔离 | 当前用户 + 后端解析出的唯一 115 账号 UID | 当前版本为单账号模型；账号 UID 仅用于后端内部 Cookie、缓存、限流与审计隔离 |
| 账号上下文传递 | 前端公开 `/api/*` 端点不得要求 `CloudAccountUid`；应用层由 `ICloudDriveAccountContextResolver` 解析当前唯一账号上下文，运维端点可显式指定账号 UID |
| Cookie 失效处理 | 标记状态 + 冷却期（默认 60 秒，可通过 `CookieCooldownSeconds` 配置）+ `CookieInvalidated` 领域事件 | 避免失效后短时间内反复探测或暴露内部状态，系统不尝试自动恢复 |
| 签名服务 | `ICookieSignatureStrategy` 为云下载签名预留扩展点 | 115 网页端云下载接口需 `sign + time + uid` 三元组，算法需联调还原 |
| 限流策略 | 令牌桶算法 + 有界排队/拒绝 + 按用户/115账号/场景维度 | 支持分类请求频率、超限处置与可观测性 |
| 缓存方案 | IDistributedCache + JSON 封装（DistributedCacheExtensions） | 按用户+115账号+目录维度缓存，支持 TTL 配置 |
| 后台任务 | Quartz.NET IJob + IScheduler + JobOrchestrator | 大文件上传、批量操作、清空回收站、转码等长耗时任务 |
| 写操作返回模型 | 同步结果 + 异步受理双态 DTO | 统一表达立即完成与后台排队的结果 |
| 错误字段适配 | `ErrorFieldAdapter` 注册表按 URL 路径声明 `errNo / errCode / errno / code / msg / message` 变体 | 115 网页端不同 URL 的错误响应字段存在多种变体，需统一解析口径 |
| 失败响应 | `Result.Failure` 统一映射为非 2xx `ProblemDetails`，异常交给全局异常中间件 | 兼容现有 `Result` 模式与 WebApi 基线，避免 `200 + failure body` |
| HTTP 客户端 | IHttpClientFactory + Polly + Cookie Gateway 抽象 | 连接池管理、按幂等边界应用重试、超时、熔断并保持分层清晰 |
| 图片/文档预览 | 通过下载地址 + 前端渲染 | 115 网页端无专门预览 API |
| 视频播放 | 网页端 `webapi.115.com/movies` + 转码接口 | 115 网页端提供视频转码与播放地址获取能力 |
| 资源访问策略 | 临时直链优先，保留受控代理模式 | 满足临时资源、受限访问与合规要求 |

### 前置约束与边界

- **数据源范围**：本系统仅对接 115 网页端原生 API（`webapi.115.com`、`my.115.com`、`vip.115.com`、`pay.115.com`、`115.com/web/lixian` 等），不调用开放平台 OAuth 接口（`passportapi.115.com`、`proapi.115.com`、`qrcodeapi.115.com`）。未经用户主动授权的网页端抓包接口不在范围内
- 系统以前置条件已满足为部署前提：运维必须已为每个待访问的 115 账号提供有效的 Cookie 会话三元组（UID/CID/USSID）；若未配置 Cookie 或系统检测到配置缺失，必须禁用 115 集成功能并记录配置错误。具体采用双层保护机制：
  - **启动期校验**：在 `AddCloudDrive()` 扩展方法中通过 `IOptions<CloudDriveAppConfig>` 的 `ValidateOnStart()` 或手动校验逻辑，检查 Cookie 加密密钥、签名密钥、网页端基础域名等核心配置是否完整；校验失败时记录 `LogLevel.Error` 日志并将 `CloudDriveFeatureEnabled` 标志设为 `false`，但不阻止应用启动
  - **运行期保护**：所有 115 相关 Handler 在入口处检查 `CloudDriveFeatureEnabled` 标志（通过 `IOptions<CloudDriveAppConfig>` 注入），若为 `false` 则立即抛出 `CloudDriveConfigurationException`，确保不会发起任何 115 API 调用
- 所有下载地址、预览地址、播放地址均视为临时授权资源，只能在当前已绑定用户上下文中短时使用，不得持久化为公开永久链接
- Cookie 原文、签名密钥、用户敏感字段（手机号、邮箱、身份证、住址、生日等）不得写入日志、响应体或非加密持久化存储；日志输出前由 `CloudDriveSensitiveFieldsFilter` 按需求 23 / 26 的黑名单脱敏
- 对于高频浏览、媒体扫描、批量操作与跨设备访问场景，必须组合使用限流、缓存、后台任务和退避策略，以降低风控风险

### 账号隔离策略

- Cookie 会话记录按 `UserId + CloudAccountUid` 维度组织，其中 `CloudAccountUid` 为后端从 Cookie 配置解析出的 115 账号唯一标识（Cookie 方案无 AppId 维度）
- 当前版本为单账号模型：每个用户上下文最多解析出一个 Active 115 账号；若不存在或存在多条 Active 记录，业务 Handler 应返回明确 ProblemDetails，提示运维修复 Cookie 配置
- 元数据缓存键按 `UserId + CloudAccountUid + DirectoryId/FileId` 组合生成，确保后续恢复多账号时缓存维度稳定；当前公开 API 不接收账号参数
- 限流桶按 `UserId + CloudAccountUid + Category` 维度维护，避免后台任务、媒体播放与云下载互相挤占配额
- 后台任务记录需关联 `CloudAccountUid`，以便审计、恢复和重放时准确定位账号上下文
- 前端公开 `/api/*` 端点不得通过 query、body、header 显式传递 `CloudAccountUid`；账号上下文由应用层通过 `ICloudDriveAccountContextResolver` 统一解析
- 运维端点（Cookie 注入、冷却、撤销）可以显式指定 `CloudAccountUid`，但必须位于受保护的 `/api/admin/cloud-drive/*` 命名空间，不纳入普通前端消费面

### 同步/异步执行边界

- 文件列表、详情、搜索、用户信息查询等短请求采用同步执行
- `UploadAsync` 使用 `CloudDriveUploadExecutionDto` 表达双态结果：同步完成时返回上传后的 `FileId`、名称与路径；后台受理时返回 `Accepted = true` 与 `JobId`
- 批量复制/移动/删除与回收站还原/彻底删除使用 `CloudDriveOperationResultDto` 返回同步确认或后台任务受理信息；清空回收站当前统一通过后台任务执行并返回 `Accepted = true` 与 `JobId`；新建文件夹与重命名保持同步并返回资源详情
- 视频播放请求在发现网页端尚未返回可播放转码流时，应自动提交转码任务，并以 `Transcoding` 状态响应客户端；后台转码任务通过重复调用 `GET webapi.115.com/files/video?pickcode={pickcode}&share_id=0` 等可播放性探测方式判断完成状态
- 上传恢复检查点和后台任务检查点统一保存在任务记录中，便于从最近的安全检查点恢复；上传任务当前仅保存 `TempFilePath` 等本地恢复资源信息，不依赖远端分片偏移续传
- 上传、移动、复制、删除、重命名、还原、清空回收站成功后，必须失效或刷新受影响目录缓存

### 幂等与重试边界

- 仅对令牌刷新、用户信息、文件列表/详情、搜索、下载地址、回收站列表、视频播放信息、字幕列表等幂等读请求执行自动退避重试
- 新建文件夹、上传初始化、移动、复制、删除、重命名、回收站还原/彻底删除/清空、视频转码提交等非幂等写操作默认不自动重试；由调用方感知失败或依赖后台任务/检查点恢复
- 对于状态未知的写请求，系统不得盲目重放；必须先通过官方状态查询、检查点或任务记录确认安全后再继续执行
- 上传失败时，“释放已占用的资源”按两级执行：始终清理已持久化的本地恢复检查点与临时上传文件；若 115 官方提供取消或废弃上传会话的接口，则优先调用官方接口回收远端会话

### Web API 失败响应边界

- Controller 仅负责 HTTP 输入/输出适配与 `ISender.Send()` 分发，不承载业务逻辑，也不得直接返回领域层 `Result` 作为成功响应体
- `Result.IsSuccess` 映射为正常 2xx 响应；后台受理场景返回 202 / Accepted 语义并携带 `JobId`
- `Result.IsFailure` 必须通过集中映射输出非 2xx `ProblemDetails`，并在响应中保留稳定的业务错误码（例如 `extensions.code`）
- 抛出的 `CloudDrive*Exception` 与其他未处理异常继续由全局异常中间件统一处理

## 组件与接口

### 1. 认证与会话管理模块 (Auth)

_覆盖需求: 1, 2, 3_

> **认证方案说明**：本系统采用 **Cookie 会话认证**（UID/CID/USSID），对接 115 网页端 API。Cookie 由运维/管理员通过受保护的配置注入与轮换，系统不主动发起授权流程。当 Cookie 失效时，系统仅标记状态、进入冷却期并发布 `CookieInvalidated` 领域事件，不尝试自动恢复。

#### CloudDriveAuth 模块（CQRS Command/Query）

应用层通过 MediatR Command/Query 编排 Cookie 会话管理与登录态检测。Controller 构造 Command/Query 对象并通过 `IMediator.Send()` 分发。

```csharp
// --- Commands ---
/// 管理员注入或更新指定 115 账号的 Cookie 会话（UID/CID/USSID + 可选额外字段）
public sealed record UpsertCookieSessionCommand(Guid UserId, UpsertCookieSessionInput Input) : ICommand<CloudDriveCookieSessionDto>;

/// 撤销指定 115 账号的 Cookie 会话（主动解绑，运维专用）
public sealed record RevokeCookieSessionCommand(Guid UserId, RevokeCookieSessionInput Input) : ICommand;

/// 标记 Cookie 失效并进入冷却期（由失效检测链路内部触发，非对外端点）
public sealed record EnterCookieCooldownCommand(Guid UserId, EnterCookieCooldownInput Input) : ICommand;

// --- Queries ---
/// 探测当前唯一 115 账号的登录态（对应需求 3 `/api/auth/check`）
public sealed record GetLoginStatusQuery(Guid UserId) : IQuery<CloudDriveAuthCheckDto>;

/// 聚合查询 115 账号的用户信息（对应需求 23 多源聚合：user_info + vip_limit + get_yun_card + pay.115.com/）
public sealed record GetUserInfoQuery(Guid UserId) : IQuery<CloudDriveUserInfoDto>;
```

> 说明：每个 Command/Query 对应一个 `ICommandHandler` / `IQueryHandler`，Handler 返回 `Result<T>` 或 `Result`，内部调用应用层服务或 Domain 方法。
> 说明：`UpsertCookieSessionCommand` 仅面向运维/管理员可访问的受保护端点；Cookie 来源由运维通过受保护配置注入，系统不主动发起 OAuth 授权。
> 说明：`GetLoginStatusQuery` 通过调用 `GET webapi.115.com/files?cid=0&limit=1` 探测 Cookie 是否仍然可用：(1) 返回业务成功响应 → 登录态有效；(2) 返回 `errNo=login_required` 或特定登录失效错误码 → 触发 `EnterCookieCooldownCommand`；(3) 网络异常或未分类错误 → 返回 `IsLoggedIn = null`（未知状态），不触发冷却。
> 说明：`RevokeCookieSessionCommand` 不调用远程登出接口（网页端无对应接口），仅清除本地 `CookieSession` 记录、失效相关缓存与限流桶。
> 说明：普通 Command Handler 中的领域事件由框架 `DomainEventDispatchBehavior` 在 Handler 执行后、事务提交前自动分发（前提是仓储在 Insert/Update 时调用了 `IDomainEventSource.Track(entity)`），Handler 内部不应手动调用 `IMediator.Publish()`。仅 Quartz Job 因不经过 MediatR Pipeline，需要手动发布领域事件。
> 说明：公开前端端点的输入 DTO 不包含 `CloudAccountUid`。Command/Query Handler 在进入应用层后通过 `ICloudDriveAccountContextResolver` 解析当前唯一账号上下文，再传递给 Cookie、Gateway、缓存、限流与后台任务组件。仅运维 Cookie 管理输入 DTO 可以显式携带 `CloudAccountUid`。
> 说明：Cookie 失效或探测失败时基础设施层抛出 `CloudDriveCookieInvalidatedException`，由全局异常中间件统一处理并保留原始错误字段供上层提示；同时触发 `CookieInvalidated` 领域事件发布。

#### CookieSessionManager

应用层服务，按 `UserId + CloudAccountUid` 编排 Cookie 会话生命周期（读取、更新、加密存储、失效标记、冷却管理）。通过构造函数注入依赖，不继承框架基类。注入 `ICookieSessionRepository`、`ICloudDriveSecretProtector`、`ICookieInvalidationNotifier` 与 `IOptions<CloudDriveAppConfig>`，负责仓储读写、机密保护、失效通知与配置读取的编排协作。

> 说明：`CookieSessionManager` 的职责是编排 Cookie 读取 → 注入请求头 → 失效检测 → 冷却管理的流程，属于应用层编排逻辑。纯业务规则（如"Cookie 是否处于冷却期"、"是否已失效"）保留在 `CookieSession` 实体方法中。
> 说明：与 OAuth 方案不同，Cookie 没有自动刷新机制 — 失效后必须由管理员重新注入。`CookieSessionManager` 在检测到 Cookie 失效时，仅标记状态、进入冷却期并发布 `CookieInvalidated` 领域事件，不尝试自动恢复；冷却期默认 60 秒，可通过 `CookieCooldownSeconds` 配置调整。

```csharp
public class CookieSessionManager
{
    /// 存储或更新 Cookie 会话（加密持久化 UID/CID/USSID + 额外字段）
    Task UpsertSessionAsync(Guid userId, string cloudAccountUid, CookieTriplet cookies, IDictionary<string, string>? extraFields = null);

    /// 获取当前有效的 Cookie 字符串（用于请求头注入；若处于冷却期则抛出 `CloudDriveCookieInvalidatedException`）
    Task<string> GetValidCookieHeaderAsync(Guid userId, string cloudAccountUid);

    /// 检查指定账号是否已绑定可用 Cookie
    Task<bool> IsAuthorizedAsync(Guid userId, string cloudAccountUid);

    /// 标记 Cookie 失效并进入冷却期（由 ApiClient 在探测到失效响应时调用）
    Task MarkInvalidatedAsync(Guid userId, string cloudAccountUid, string reason);

    /// 撤销指定账号的 Cookie 会话
    Task RevokeSessionAsync(Guid userId, string cloudAccountUid);
}

/// Cookie 三元组值对象（Domain 层）
public readonly record struct CookieTriplet(string Uid, string Cid, string Ussid);
```

#### ICookieInvalidationNotifier

应用层抽象，用于发布 Cookie 失效事件与运维通知。默认实现通过 `IMediator.Publish()` 发布 `CookieInvalidated` 领域事件；事件处理器可记录审计日志、发送运维通知、触发前端重新绑定引导。

```csharp
public interface ICookieInvalidationNotifier
{
    Task NotifyAsync(Guid userId, string cloudAccountUid, string reason, CancellationToken cancellationToken = default);
}
```

#### ICloudDriveSecretProtector

应用层抽象，用于对需要持久化的机密值执行 Protect / Unprotect。基础设施层默认使用 ASP.NET Core Data Protection 实现；同一部署拓扑下必须持久化并共享 key ring，以保证服务重启后仍可解密既有 Cookie 与签名密钥，多实例部署时也能读取同一份加密数据。

```csharp
public interface ICloudDriveSecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedText);
}
```

> 说明：在 Cookie 方案下，`ICloudDriveSecretProtector` 的加密目标为：(1) `CookieSession` 聚合根的 UID/CID/USSID 三元组与可选额外字段；(2) `ICookieSignatureStrategy` 所依赖的云下载签名密钥。禁止在日志或对外响应体中输出以上原文。

#### ICookieSignatureStrategy（云下载签名预留扩展点）

应用层抽象，负责为 115 网页端云下载接口（`115.com/web/lixian/`）生成 `sign + time + uid` 签名三元组。签名密钥由运维通过受保护配置注入，经 `ICloudDriveSecretProtector` 加密存储。

> 说明：115 网页端云下载签名算法当前未在 `docs/115-api-docs.md` 中采集，需通过真实 Cookie 场景联调还原（参见需求 20.3）。当前设计保留扩展点，以便联调确认后替换具体算法实现。默认实现必须返回明确的 `offline_signature_not_available` 业务错误，不得生成占位签名。

```csharp
public interface ICookieSignatureStrategy
{
    /// 生成云下载请求的签名三元组
    CookieSignature Generate(Guid userId, string cloudAccountUid, string requestBody);
}

/// 云下载签名结果（值对象）
public readonly record struct CookieSignature(string Sign, long Time, string Uid);
```

#### LoginCheckHandler（需求 3 登录态检测）

`GetLoginStatusQuery` 的处理器，按以下策略探测 Cookie 是否仍然可用：

1. 从 `CookieSessionManager` 获取当前 `CookieSession` 的冷却状态；若处于冷却期内，直接返回 `IsLoggedIn = false` 并在 `CooldownUntil` 字段标明剩余冷却时间，不发起官方请求
2. 否则调用 `GET webapi.115.com/files?cid=0&limit=1` 并解析响应：
    - 业务成功（`state == true`）→ `IsLoggedIn = true`
    - 识别为登录失效类错误码（如 `errNo=login_required` 或 `ErrorFieldAdapter` 判定为登录失效的其他变体）→ `IsLoggedIn = false`，同时触发 `EnterCookieCooldownCommand`
    - 其他未知错误或网络超时 → `IsLoggedIn = null`（未知状态），不触发冷却
3. 返回公开 DTO `CloudDriveAuthCheckDto { IsAuthenticated, FailureReason? }`；内部诊断日志可记录脱敏后的 `CloudAccountUid`、`LastCheckedAt` 与 `CooldownUntil?`

> 说明：该 Handler 为 Query Handler，符合 CQRS 约束（不发布事件、不写库）；冷却状态推进通过独立的 `EnterCookieCooldownCommand` 完成，保持 Query 与 Command 职责分离。

### 2. 文件管理模块 (File)

_覆盖需求: 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 13a, 14_

#### CloudDriveFile 模块（CQRS Command/Query）

应用层通过 MediatR Command/Query 编排文件 CRUD、搜索、回收站等操作.

```csharp
// --- Queries ---
public sealed record GetFileListQuery(Guid UserId, GetFileListInput Input) : IQuery<PagedResultDto<CloudDriveFileDto>>;
public sealed record GetFileDetailQuery(Guid UserId, GetFileInput Input) : IQuery<CloudDriveFileDetailDto>;
public sealed record SearchFilesQuery(Guid UserId, SearchFileInput Input) : IQuery<PagedResultDto<CloudDriveSearchResultDto>>;
public sealed record GetDownloadUrlQuery(Guid UserId, GetDownloadUrlInput Input) : IQuery<CloudDriveDownloadDto>;
public sealed record GetRecycleBinListQuery(Guid UserId, GetRecycleBinListInput Input) : IQuery<PagedResultDto<CloudDriveRecycleBinItemDto>>;

// --- Commands ---
public sealed record CreateFolderCommand(Guid UserId, CreateFolderInput Input) : ICommand<CloudDriveFolderDto>;
public sealed record InitUploadCommand(Guid UserId, InitUploadInput Input) : ICommand<CloudDriveUploadSessionDto>;
public sealed record UploadFileCommand(Guid UserId, UploadFileInput Input, Stream? FileStream) : ICommand<CloudDriveUploadExecutionDto>;
public sealed record MoveFilesCommand(Guid UserId, MoveFilesInput Input) : ICommand<CloudDriveOperationResultDto>;
public sealed record CopyFilesCommand(Guid UserId, CopyFilesInput Input) : ICommand<CloudDriveOperationResultDto>;
public sealed record DeleteFilesCommand(Guid UserId, DeleteFilesInput Input) : ICommand<CloudDriveOperationResultDto>;
public sealed record RenameFileCommand(Guid UserId, RenameFileInput Input) : ICommand<CloudDriveFileDetailDto>;
public sealed record ToggleStarCommand(Guid UserId, ToggleStarInput Input) : ICommand<CloudDriveOperationResultDto>;
public sealed record RestoreFromRecycleBinCommand(Guid UserId, RecycleBinOperationInput Input) : ICommand<CloudDriveOperationResultDto>;
public sealed record DeleteFromRecycleBinCommand(Guid UserId, RecycleBinOperationInput Input) : ICommand<CloudDriveOperationResultDto>;
public sealed record ClearRecycleBinCommand(Guid UserId, ClearRecycleBinInput Input) : ICommand<CloudDriveOperationResultDto>;
```

> 说明：`InitUploadCommand` 仅返回上传会话/凭证，不代表文件已上传成功；`CloudDriveOperationResultDto` 与 `CloudDriveUploadExecutionDto` 统一表达同步完成和异步受理结果.
> 说明：`InitUploadCommandHandler`、`UploadFileCommandHandler` 与 `CloudDriveUploadJob` 共享 `CloudDriveUploadInitContract`，统一使用 `POST uplb.115.com/3.0/sampleinitupload.php` 与 `userid` / `filename` / `filesize` / `target=U_1_{cid}` 请求体，并将网页端响应统一映射为 `CloudDriveUploadSessionDto`（秒传或 OSS 凭证），避免不同执行路径出现上传初始化协议分裂.
> 说明：目录刷新仅作为应用层内部缓存失效能力，不暴露公开 `POST /api/files/refresh` 端点；前端刷新当前目录时重新调用 `GET /api/files`。

#### CloudDriveFileManager

应用层服务，按 `UserId + CloudAccountUid` 上下文编排文件操作的缓存协作与 API 调用（缓存优先读取、写操作后缓存失效等）。通过构造函数注入依赖，调用 `ICloudDriveOpenApiGateway` 时仅需传递 `UserId + CloudAccountUid`；网关内部从 `CookieSessionManager` 获取当前 Cookie 并注入请求头。Cookie 方案不携带 `AppId` 维度。

> 说明：`CloudDriveFileManager` 的职责是编排"检查缓存→调用 API→写入缓存→失效缓存"的流程，属于应用层编排逻辑。纯业务规则（如文件名合法性验证）保留在 Domain 层的 `CloudDriveFileNameValidator` 静态类中.

```csharp
public class CloudDriveFileManager
{
    /// 获取文件列表（缓存优先）
    Task<CloudDriveFileListResult> GetFileListAsync(Guid userId, string cloudAccountUid, string directoryId, int offset, int limit);

    /// 获取文件详情（缓存优先）
    Task<CloudDriveFileDetail> GetFileDetailAsync(Guid userId, string cloudAccountUid, string fileId);

    /// 执行文件操作并使缓存失效
    Task<T> ExecuteWithCacheInvalidationAsync<T>(Guid userId, string cloudAccountUid, string[] affectedDirectoryIds, Func<Task<T>> action);
}
```

> 说明：文件名验证逻辑由 Domain 层的 `CloudDriveFileNameValidator` 静态类提供，`CloudDriveFileManager` 和 Handler 调用该静态方法完成名称校验。注意：网页端新建文件夹接口（对应需求 7）限制文件夹名最长 **255 字符**，而重命名接口（对应需求 12）限制文件名最多 **255 字节**（UTF-8 下中文字符占 3 字节）；具体限值依据需求采集中的网页端响应。因此验证器须提供 `ValidateNewFolderName`（255 字符）和 `ValidateRenameFileName`（255 字节）两种校验方法。

### 3. 预览模块 (Preview)

_覆盖需求: 15, 19_

#### CloudDrivePreview 模块（CQRS Query）

应用层通过 MediatR Query 根据文件类型提供预览信息.

```csharp
public sealed record GetImagePreviewQuery(Guid UserId, GetPreviewInput Input) : IQuery<CloudDriveImagePreviewDto>;
public sealed record GetDocPreviewQuery(Guid UserId, GetPreviewInput Input) : IQuery<CloudDriveDocPreviewDto>;
```

> 说明：`GetImagePreviewQuery` 仅支持 `JPG/JPEG/PNG/GIF/BMP/WEBP` 六类格式，返回官方临时地址或系统受控代理地址；不支持的格式或不存在的文件必须返回明确错误.
> 说明：`GetDocPreviewQuery` 仅支持 `PDF/TXT` 直接渲染，以及 `DOC/DOCX/XLS/XLSX/PPT/PPTX` 第三方渲染；文件不存在或格式不支持时必须返回明确错误，Office 第三方服务未配置时必须返回 `RenderMode = NotAvailable`，且不得伪造可预览能力.

### 4. 视频模块 (Video)

_覆盖需求: 16, 17, 18_

#### CloudDriveVideo 模块（CQRS Command/Query）

应用层通过 MediatR Command/Query 编排视频播放与转码.

```csharp
public sealed record GetVideoPlayUrlCommand(Guid UserId, GetVideoPlayInput Input) : ICommand<CloudDriveVideoPlayDto>;
public sealed record SubmitTranscodeCommand(Guid UserId, SubmitTranscodeInput Input) : ICommand<CloudDriveVideoTranscodeDto>;
public sealed record GetSubtitlesQuery(Guid UserId, GetSubtitlesInput Input) : IQuery<CloudDriveSubtitleListDto>;
public sealed record GetVideoPlayProgressQuery(Guid UserId, GetVideoPlayProgressInput Input) : IQuery<CloudDriveVideoPlayProgressDto>;
public sealed record RememberVideoPlayProgressCommand(Guid UserId, RememberVideoPlayProgressInput Input) : ICommand;
```

> 说明：`GetVideoPlayUrlCommand` 使用 Command 而非 Query，因为在检测到视频尚未转码完成时需要触发转码提交副作用（创建后台任务），违反 Query 不允许副作用的 CD0007 约束。若本次调用成功创建后台转码任务，则必须返回 `JobId`，否则可保持为空。返回 `Transcoding` 状态时客户端应轮询任务进度.
> 说明：115 网页端视频播放、字幕、转码与播放进度接口均以 `pickcode` 为核心定位参数（网页端字段名为 `pickcode`，应用层 DTO 使用 `PickCode`）；若上层仅持有 `FileId`，应用层应先解析出 `pickcode` 再调用 `webapi.115.com/files/video`（播放）、`webapi.115.com/movies/subtitle`（字幕）与相关播放进度接口；具体网页端接口名以 `requirements.md` 采集为准。4K / 原画可用性以网页端返回结果与用户会员等级限制为准；返回 `CloudDriveVideoPlayDto` 时必须显式设置 `IsTemporary = true`，并通过 `UsageRestrictionNotice` 明确该播放资源仅限当前已绑定用户在本系统内短时使用。若网页端提供 `multitrack_list` 的音轨列表，返回模型必须保留音轨索引信息以支持音轨切换。

### 5. 后台任务模块 (Quartz.NET Job)

_覆盖需求: 28_

#### CloudDriveJob 查询（CQRS Query）

应用层通过 MediatR Query 管理长耗时任务状态查询.

```csharp
public sealed record GetJobStatusQuery(Guid UserId, GetJobStatusInput Input) : IQuery<CloudDriveJobDto>;
public sealed record GetJobListQuery(Guid UserId, GetJobListInput Input) : IQuery<PagedResultDto<CloudDriveJobDto>>;
```

#### CloudDriveJobOrchestrator

应用层协作组件，负责决定操作是同步执行还是转为后台任务，并统一创建 `CloudDriveJob` 记录、通过应用层抽象 `ICloudDriveJobScheduler` 调度对应的后台 Job；Infrastructure 层当前由 Quartz.NET 提供具体实现.

```csharp
public interface ICloudDriveJobOrchestrator
{
    Task<Guid> EnqueueLargeUploadAsync(Guid userId, UploadFileInput input, CancellationToken cancellationToken = default);
    Task<Guid> EnqueueBatchOperationAsync(Guid userId, string cloudAccountUid, CloudDriveJobType jobType, object payload, CancellationToken cancellationToken = default);
    Task<Guid> EnqueueClearRecycleBinAsync(Guid userId, string cloudAccountUid, CancellationToken cancellationToken = default);
    Task<Guid> EnqueueVideoTranscodeAsync(Guid userId, SubmitTranscodeInput input, CancellationToken cancellationToken = default);
}
```

> 说明：`CloudDriveJobOrchestrator` 内部通过 `ICloudDriveJobScheduler.ScheduleAsync()` 触发调度，Quartz Job 仍通过 `JobDataMap` 传递 `JobId`，再从数据库 `CloudDriveJob.PayloadJson` 字段加载业务载荷（JSON 反序列化为对应的输入参数对象）。每个 Job 在独立 DI Scope 中执行（由 `UseMicrosoftDependencyInjectionJobFactory()` 保证）。`CloudDriveJobOrchestrator` 在创建 `CloudDriveJob` 记录时，必须将业务输入参数序列化为 JSON 写入 `PayloadJson` 字段；上传任务仅持久化可恢复元数据与 `TempFilePath`，不直接序列化文件流.
> 说明：`CloudDriveJob` 是后台任务状态与恢复信息的唯一事实来源；当前 Quartz 仅承担进程内调度，不作为持久化来源。应用启动后必须执行任务恢复协调：扫描数据库中 `Pending` / `Running` 的任务记录，对具备 `PayloadJson` + `CheckpointData` 或明确安全重试条件的任务重新调度，对不可恢复任务标记为 `Failed` 并记录原因。对于上传任务，只有在载荷或检查点中的 `TempFilePath` 对应本地临时文件仍然存在时，才视为具备可安全恢复条件.

### 6. 云下载模块 (Offline)

_覆盖需求: 20, 21_

> **接口映射说明**：本模块对接 115 网页端云下载（离线下载）API（`115.com/web/lixian/?ct=lixian&ac={操作符}`），所有请求需 `sign + time + uid` 签名与 `ct=lixian&ac={操作符}` 查询参数。

#### CloudDriveOffline 模块（CQRS Command/Query）

应用层通过 MediatR Command/Query 编排云下载任务生命周期与配额查询。

```csharp
// --- Queries ---
/// 查询云下载任务列表（对应需求 20 AC1-AC3）
public sealed record GetOfflineTaskListQuery(Guid UserId, GetOfflineTaskListInput Input) : IQuery<PagedResultDto<CloudDriveOfflineTaskDto>>;

/// 查询云下载空间配额与统计（对应需求 20 AC4-AC5）
public sealed record GetOfflineQuotaQuery(Guid UserId) : IQuery<CloudDriveOfflineQuotaDto>;

/// 查询云下载任务统计（对应需求 20 AC7）
public sealed record GetOfflineStatsQuery(Guid UserId) : IQuery<CloudDriveOfflineStatsDto>;

// --- Commands ---
/// 新增云下载任务（支持 URL 列表提交，对应需求 21 AC1）
public sealed record AddOfflineTaskCommand(Guid UserId, AddOfflineTaskInput Input) : ICommand<CloudDriveOperationResultDto>;

/// 删除（取消）指定云下载任务
public sealed record CancelOfflineTaskCommand(Guid UserId, CancelOfflineTaskInput Input) : ICommand<CloudDriveOperationResultDto>;

/// 清空已完成/已失败任务
public sealed record ClearOfflineTasksCommand(Guid UserId, ClearOfflineTasksInput Input) : ICommand<CloudDriveOperationResultDto>;
```

> 说明：`POST /api/offline/tasks/urls` 与 `POST /api/offline/tasks/bt` 均映射为 `AddOfflineTaskCommand`，由 `AddOfflineTaskInput.TaskKind` 区分 `Url` / `Bt`；`POST /api/offline/parse-torrent` 在当前 `docs/115-api-docs.md` 未采集，Controller 可返回 `cloud_drive_api_not_collected`，不得定义猜测型 115 path。
> 说明：所有云下载写操作（Add/Cancel/Clear）均为非幂等，默认不自动重试；仅查询操作（Get*）允许瞬时错误重试。
> 说明：任务列表响应字段归一化：`total_count → total`、`failed_count → failed`、`finished_count → completed`、`downloading_count → active`（见 `requirements.md:370`）。
> 说明：任务添加时由 `ICookieSignatureStrategy` 生成签名；若签名实现尚未提供，`Result.Failure` 返回 `offline_signature_not_available` 业务错误而非抛出异常。

---

### 7. 分享管理模块 (Share)

_覆盖需求: 22_

> **接口映射说明**：本模块对接 115 网页端分享 API（`webapi.115.com/share/slist`、`/share/send`、`/share/receive`），均需 `format=json` 必填参数；`send` 与 `receive` 的错误字段优先读 `errNo`，fallback `errno`（见 `requirements.md:464`）。

#### CloudDriveShare 模块（CQRS Query）

应用层通过 MediatR Query 管理分享资源（对应需求 22 AC1-AC6）。

```csharp
// --- Queries ---
/// 查询我的分享列表（/share/slist）
public sealed record GetMySharesQuery(Guid UserId, GetShareListInput Input) : IQuery<PagedResultDto<CloudDriveShareItemDto>>;

/// 查询分享发送记录（/share/send）
public sealed record GetShareSendsQuery(Guid UserId, GetShareListInput Input) : IQuery<PagedResultDto<CloudDriveShareSendItemDto>>;

/// 查询分享接收记录（/share/receive）
public sealed record GetShareReceivesQuery(Guid UserId, GetShareListInput Input) : IQuery<PagedResultDto<CloudDriveShareReceiveItemDto>>;
```

> 说明：当前需求 22 采集样本均为失败响应（低置信度，见 `requirements.md:324-335`）；`ErrorFieldAdapter` 先以双写字段规则处理这些响应，Handler 遇到解析失败时返回明确的 `share_sample_unavailable` 业务错误。联调获得真实可用样本后需补齐字段映射。
> 说明：分享相关写操作（创建/撤销分享）暂不在需求范围内；本模块仅提供读视图。

---

### 8. 用户中心聚合模块 (UserAggregator)

_覆盖需求: 23_

> **接口映射说明**：对应需求 23 多源聚合 — `my.115.com/proapi/3.0/index.php?method=user_info&uid=...`（主资料）、`webapi.115.com/user/vip_limit?feature=2`（VIP 配额，错误字段 `code` string）、`webapi.115.com/files/index_info?count_space_nums=1`（存储空间，错误字段 `code` number + `error`）、`vip.115.com/*`（VIP 详情）、`pay.115.com/`（错误字段 `msg`）。`GetUserInfoQuery` 已在认证模块中定义。

#### CloudDriveUserAggregation 模块（CQRS Query）

应用层通过 MediatR Query 聚合多源用户信息。本模块通过 `UserAggregationService` 内部并行调用多源接口，并做失败容错（任一子接口失败不影响其他字段返回）。

```csharp
/// 获取用户聚合画像（对应需求 23 AC1）
public sealed record GetUserProfileQuery(Guid UserId) : IQuery<CloudDriveUserProfileDto>;

/// 获取用户空间详情（对应需求 23 AC3）
public sealed record GetUserSpaceQuery(Guid UserId) : IQuery<CloudDriveSpaceInfoDto>;

/// 获取用户 VIP 详情与特权（对应需求 23 AC4, AC5）
public sealed record GetUserVipQuery(Guid UserId) : IQuery<CloudDriveVipInfoDto>;

/// 获取账户余额（对应需求 23 AC5）
public sealed record GetUserBalanceQuery(Guid UserId) : IQuery<CloudDriveBalanceDto>;
```

#### UserAggregationService

应用层服务，按 `UserId + CloudAccountUid` 上下文并行调用多源接口并组装结果。通过构造函数注入 `ICloudDriveOpenApiGateway`、`IOptions<CloudDriveAppConfig>`、`ILogger<UserAggregationService>` 等依赖。

```csharp
public class UserAggregationService
{
    /// 并行聚合用户主资料、VIP、空间等信息；单个子接口失败返回空值并记录错误摘要
    Task<CloudDriveUserInfoDto> AggregateUserInfoAsync(Guid userId, string cloudAccountUid, CancellationToken cancellationToken = default);
}
```

> 说明：聚合过程需严格过滤 **需求 23 黑名单中的 34 个敏感字段**（手机号、邮箱、身份证、住址、生日、人口、职业、兴趣等 8 类），保证对外响应只包含可公开展示的资料；过滤由 `CloudDriveSensitiveFieldsFilter` 在 DTO 映射阶段完成（见 `requirements.md:416-425`）。
> 说明：VIP 响应中 `00_balance` 字段（以数字开头）因 C# 属性命名限制，映射时统一归一化为 `Balance`；其余数字开头字段也需遵循此归一化策略。
> 说明：聚合失败策略：采用 `Task.WhenAll` 并行调用，任一子接口失败时该字段置空并在日志中记录 `sub_endpoint_failed` 条目（含脱敏的错误摘要）；只有所有子接口都失败时才返回 `Result.Failure`。

---

### 9. 基础设施组件

_覆盖需求: 24, 25, 26, 27, 29_

#### ICloudDriveOpenApiGateway / CloudDriveApiClient

_覆盖需求: 25, 26, 29_

应用层通过 `ICloudDriveOpenApiGateway` 抽象访问 115 网页端 API（定义在 Application 层 `Abstractions/` 目录），基础设施层由 `CloudDriveApiClient` 实现该接口并负责与 115 网页端 **7 个域名分组** 的通信：

| 序号 | 域名 | 用途 | 认证方式 |
|:---:|---|---|---|
| 1 | `webapi.115.com` | 文件管理、视频、搜索、分享、回收站 | Cookie |
| 2 | `my.115.com` | 用户基本信息 | Cookie |
| 3 | `vip.115.com` / `pay.115.com` | VIP 状态、余额 | Cookie |
| 4 | `115.com/web/lixian` | 云下载任务 | Cookie + sign/time/uid 签名 |
| 5 | `uplb.115.com` | **上传初始化**（`/3.0/sampleinitupload.php`） | Cookie + userid 参数 |
| 6 | `*.oss-cn-*.aliyuncs.com` | **OSS 直传**（PostObject） | OSS policy + signature（无 Cookie） |
| 7 | `passportapi.115.com` | 登录态探测（预留） | Cookie |

> 说明：接口名称保留 `ICloudDriveOpenApiGateway`（为保持应用层依赖稳定），但**语义涵盖为 115 API 统一网关抽象**；如需更准确的命名可后续重构为 `ICloud115WebApiGateway`。

```csharp
public interface ICloudDriveOpenApiGateway
{
    /// 发送网页端业务 API 请求（默认 webapi.115.com）
    /// 自动从 CookieSessionManager 读取当前 Cookie 并注入请求头；若 Cookie 处于失效冷却期则抛出 CloudDriveCookieInvalidatedException
    Task<T> SendWebApiRequestAsync<T>(Guid userId, string cloudAccountUid, HttpMethod method, string endpoint, CloudDriveApiContext context, CancellationToken cancellationToken = default);

    /// 发送用户中心类 API 请求（my.115.com / vip.115.com / pay.115.com）
    /// context.TargetDomain 指向具体域名；支持需求 23 的多源聚合
    Task<T> SendUserCenterRequestAsync<T>(Guid userId, string cloudAccountUid, HttpMethod method, string endpoint, CloudDriveApiContext context, CancellationToken cancellationToken = default);

    /// 发送云下载 API 请求（115.com/web/lixian/）
    /// 自动从 ICookieSignatureStrategy 生成 sign + time + uid 签名并合并到请求 Body；必须在请求 URL 上追加 `?ct=lixian&ac={操作符}`
    Task<T> SendOfflineApiRequestAsync<T>(Guid userId, string cloudAccountUid, HttpMethod method, string endpoint, CloudDriveApiContext context, CancellationToken cancellationToken = default);

    /// 上传初始化（POST uplb.115.com/3.0/sampleinitupload.php）
    /// 返回秒传结果或 OSS 直传凭证（object/accessid/host/policy/signature/expire/callback）
    Task<T> SendUploadInitRequestAsync<T>(Guid userId, string cloudAccountUid, CloudDriveApiContext context, CancellationToken cancellationToken = default);

    /// OSS 直传（POST {host}/ — PostObject 方式，不需 Cookie 注入，使用 sampleinitupload 返回的 policy/signature/callback）
    /// 响应体为 115 业务 JSON（OSS callback 透传）
    Task<T> UploadFileAsync<T>(Guid userId, string cloudAccountUid, string uploadUrl, Stream fileStream, string fileName, IDictionary<string, string>? formFields = null, CancellationToken cancellationToken = default);
}

// 上传三步时序（网页端实测 2026-04-18）：
// 1. SendUploadInitRequestAsync → POST uplb.115.com/3.0/sampleinitupload.php
//    body: userid={uid}&filename={name}&filesize={bytes}&target=U_1_{cid}
//    秒传: 响应含 file_id 且无 OSS 凭证 → 直接完成
//    非秒传: 响应含 { object, accessid, host, policy, signature, expire, callback }
// 2. UploadFileAsync → POST {host}/ (OSS PostObject multipart)
//    字段: key=${object}, policy, OSSAccessKeyId=${accessid}, signature, callback, file
//    响应: 115 业务 JSON（OSS callback 透传 samplecompleteupload.php 的结果）
// 3. samplecompleteupload.php 由 OSS 服务端回调，客户端不可见；
//    callbackBody: sha1, filename, filesize, mimeType, userid, target, bucket, cb_token
//    响应 state:boolean + code:number + data:{ file_id, file_name, pick_code, sha1, cid, ... }

/// 统一请求上下文：按 115 网页端的 query string / urlencoded form / JSON / multipart 四类必需构造方式组合
public sealed class CloudDriveApiContext
{
    public string? TargetDomain { get; init; }                        // 多域名聚合时显式指定
    public IDictionary<string, string>? QueryParameters { get; init; } // query string
    public IDictionary<string, string>? FormFields { get; init; }      // application/x-www-form-urlencoded
    public object? JsonBody { get; init; }                             // application/json
    public string? Category { get; init; }                             // 限流桶场景分类
}

public class CloudDriveApiClient : ICloudDriveOpenApiGateway
{
    // 负责 HTTP 通信、Cookie 注入、签名生成、错误解析（委托 ErrorFieldAdapter）、重试边界控制与脱敏日志
}
```

> 说明：`CloudDriveApiClient` 对 115 网页端响应的成功判定由 `IErrorFieldAdapter` 按 URL 路径动态解析（见下一节）；默认语义为 HTTP 成功且响应中 `state == true`（值型兼容 `1`）且统一错误码字段为 0 / 未出现失败映射时视为成功；否则映射为适当的 `CloudDrive*Exception`（详见错误码映射表）。

#### IErrorFieldAdapter / ErrorFieldAdapter（错误响应字段适配器）

_覆盖需求: 25_

应用层抽象覆盖 115 网页端错误响应字段变体表中的 **13 行 URL 规则**（errNo / errCode / errno / code / msg / message / Message / err / error / 等任意组合），按 **URL 路径** 声明具体映射规则。基础设施层由 `ErrorFieldAdapter` 实现，保持注册表为 Immutable 或配置驱动，避免硬编码造成独立 URL 不可解析。

```csharp
public interface IErrorFieldAdapter
{
    /// 根据请求 URL 与响应 JSON 解析出标准化的错误视图
    CloudDriveApiError? TryParse(Uri requestUri, JsonDocument responseJson);
}

public sealed class ErrorFieldAdapter : IErrorFieldAdapter
{
    // 内部持有 Dictionary<string, ErrorFieldDescriptor>，键为 URL 路径模式
    // 值为该 URL 下的错误字段名称 + 语义（号码类型/消息字段）
}

public sealed record CloudDriveApiError(int? Code, string? Message, string RawField, Uri SourceUri);

/// 单条 URL 对应的字段解析规则
public sealed record ErrorFieldDescriptor(
    string UrlPattern,                 // 例："/files/search"（匹配 webapi.115.com/files/search）
    string CodeFieldName,              // 例："errCode"
    ErrorCodeType CodeType,            // Number / String
    string MessageFieldName,           // 例："message" 或 "error"
    string? FallbackCodeFieldName = null,  // 备选字段（如 errNo 与 errno 双写场景）
    string? FallbackMessageFieldName = null);

public enum ErrorCodeType { Number, String }
```

> 说明：注册表需覆盖需求 25 错误字段变体表中的全部 URL 规则，包括：
> - `/files/search` 使用 `errCode` (number) + `error`
> - `/movies/subtitle` 使用大写 `errNo` + `error`
> - `user/vip_limit` 使用 `code` (string) + `message`
> - `files/index_info` 使用 `code` (number) + `error`
> - `pay.115.com/` 使用 `msg`（非 `message`）
> - `share/send`, `share/receive` 优先读 `errNo`，fallback `errno`
> - `/files/video_push` 使用 `errNo` (number) + `error`，fallback `errno`（转码提交，非 VIP 场景返回 `state: false`）
> - `/movies/transcode` 使用 `errNo` (number) + `error`，fallback `errno`（转码备选路径）
> - `/files/history` 使用 `state` (boolean) + `error`（播放进度读写，无数字错误码）
> - `uplb.115.com/3.0/sampleinitupload` 响应为 `text/html` 包裹的 JSON；秒传时含 `file_id`，非秒传时含 OSS 凭证（无标准错误码字段，由 `CloudDriveApiClient` 单独判定）
> - 其他 按需求表 注册
>
> 说明：未命中任何 URL 模式的请求使用默认规则：读 `state` (bool/number) + `code` / `errno` + `message` / `error`；若上述字段均缺失，仅以 HTTP 状态码判定成败。

#### CloudDriveRateLimiter

_覆盖需求: 24_

频控组件，基于令牌桶算法与有界排队/拒绝策略，实现按用户 + 115 账号 + 场景维度限流。注册为 Singleton 生命周期，内部通过 `ConcurrentDictionary<string, TokenBucket>` 按维度键持有令牌桶状态，确保跨请求共享限流计数。单机部署场景下使用进程内内存状态即可；多实例部署时需替换为分布式限流方案（如 Redis 令牌桶），当前设计预留 `ICloudDriveRateLimiter` 接口抽象以支持后续替换.

```csharp
public class CloudDriveRateLimiter
{
    /// 获取执行许可（可能等待或抛出限流异常）
    Task<IDisposable> AcquireAsync(Guid userId, string cloudAccountUid, string? category = null, CancellationToken cancellationToken = default);

    /// 记录 API 返回的频控信号（429 等），触发退避
    void RecordThrottleSignal(Guid userId, string cloudAccountUid, string? category = null);
}
```

> 说明：`AcquireAsync` 根据 `RateLimitOverflowStrategy` 选择排队或立即拒绝；队列长度超过 `RateLimitMaxQueueLength` 时直接返回限流异常
> 组件需要记录排队次数、拒绝次数、退避重试次数和最终失败次数，并输出到日志或指标系统

#### CloudDriveMetadataCache

_覆盖需求: 27_

元数据缓存组件，按用户 + 115 账号 + 目录维度缓存文件列表与详情.

> 说明：Application 层定义 `ICloudDriveMetadataCache` 接口（仅包含 `GetOrSetAsync`、`InvalidateDirectoryAsync`、`InvalidateFileAsync` 方法签名），实现类 `CloudDriveMetadataCache` 位于 Infrastructure 层，内部使用 `IDistributedCache` + `DistributedCacheExtensions`（Application 层不引用 `IDistributedCache`，遵循 Ports & Adapters 模式）。

```csharp
public class CloudDriveMetadataCache
{
    /// 获取或设置缓存
    Task<T?> GetOrSetAsync<T>(string cacheKey, Func<Task<T>> factory, TimeSpan? ttl = null);

    /// 使指定目录缓存失效
    Task InvalidateDirectoryAsync(Guid userId, string cloudAccountUid, string directoryId);

    /// 使指定文件缓存失效
    Task InvalidateFileAsync(Guid userId, string cloudAccountUid, string fileId);
}

```

> 说明：读路径必须先检查 `CloudDriveMetadataCache`；仅当缓存未命中或用户显式执行手动刷新时，才进入 `CloudDriveRateLimiter` 与 `CloudDriveApiClient` 的官方请求链路
> 搜索结果不走缓存：搜索关键词组合多变且实时性要求高，缓存命中率低，因此搜索请求直接经过限流器发往官方 API
> 手动刷新请求必须绕过现有缓存并重新拉取官方数据，但该次官方拉取仍需经过限流器统一治理
> 缓存内容仅允许写入官方实际返回字段或系统可验证的派生值；对官方未提供且不可验证的字段必须保留为空值或缺省值

---

## 数据模型

### 实体与值对象

_覆盖需求: 1, 2, 28_

#### CookieSession（聚合根 - 领域层）

存储用户的 115 网页端 Cookie 会话信息（三元组 UID/CID/USSID + 可选额外字段）。继承 `AggregateRoot<Guid>`，作为独立的事务边界与仓储入口。保留 `public` 无参构造函数供 SqlSugar ORM 反序列化使用，业务代码通过领域方法操作状态。

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | Guid | 主键 |
| UserId | Guid | 系统用户 ID |
| CloudAccountUid | string | 115 账号唯一标识（UID） |
| EncryptedCookiePayload | string | 加密后的 Cookie 认证负载（JSON 序列化的 `{ uid, cid, ussid, extraFields }`） |
| KeyVersion | int | 加密密钥版本（用于 key ring 轮换时的向后兼容解密） |
| Status | CookieSessionStatus (enum) | 状态（Active, Invalidated, Revoked） |
| InvalidationReason | string? | 失效原因描述（仅 Status=Invalidated 时有值） |
| CooldownUntil | DateTime? | 冷却期截止时间（默认为标记失效后 60 秒，通过 `CookieCooldownSeconds` 配置） |
| LastProbedAt | DateTime? | 上次登录态探测时间 |
| CreationTime | DateTime | 创建（首次注入）时间 |
| LastModificationTime | DateTime? | 最后修改时间 |

```csharp
public enum CookieSessionStatus
{
    Active,       // 当前有效
    Invalidated,  // 探测到失效，处于冷却期或待运维重新注入
    Revoked       // 管理员主动解绑
}
```

> 说明：`CookieSession` 实体方法包括 `MarkInvalidated(reason, cooldownSeconds)`、`EnterCooldown(cooldownSeconds)`、`IsInCooldown()`、`Revoke()`、`RefreshProbe(timestamp)` 等，封装状态转换规则；UID/CID/USSID 原文不暴露为属性，只能通过 `CookieSessionManager.GetValidCookieHeaderAsync` 在受保护的解密管道中取用。

#### CloudDriveJob（聚合根 - 领域层）

后台任务记录。继承 `AggregateRoot<Guid>`，作为独立的事务边界与仓储入口。保留 `public` 无参构造函数供 SqlSugar ORM 反序列化使用，业务代码通过领域方法操作状态.

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | Guid | 主键 |
| UserId | Guid | 系统用户 ID |
| CloudAccountUid | string | 关联的 115 账号唯一标识 |
| JobType | CloudDriveJobType (enum) | 任务类型（Upload, BatchCopy, BatchMove, BatchDelete, BatchRecycleBinRestore, BatchRecycleBinDelete, ClearRecycleBin, Transcode） |
| Status | CloudDriveJobStatus (enum) | 状态（Pending, Running, Completed, Failed, Cancelled） |
| ResourceId | string? | 关联资源标识 |
| Progress | int | 进度百分比 (0-100) |
| ErrorMessage | string? | 错误信息 |
| PayloadJson | string? | 业务载荷数据（JSON 序列化的任务输入参数，如上传参数、批量操作文件列表等，供 Job 从数据库恢复完整业务上下文） |
| CheckpointData | string? | 后台任务恢复检查点数据（JSON；上传任务当前主要保存 `TempFilePath` 等本地恢复资源信息） |
| StartTime | DateTime? | 开始时间 |
| EndTime | DateTime? | 结束时间 |
| CreationTime | DateTime | 创建时间 |

#### CloudDriveAppConfig（配置类 - 应用层）

基础设施配置，通过 `IOptions<CloudDriveAppConfig>` + `IConfiguration` 注入。定义在 Application 层，供 Handler 和 Infrastructure 层共同使用。**Cookie 方案下无 AppId/AppSecret/CallbackUrl 等 OAuth 字段**。

| 字段 | 类型 | 说明 |
|------|------|------|
| CookieEncryptionKeyName | string | `ICloudDriveSecretProtector` 用于加密 Cookie 负载的 key ring 名称 |
| SignatureKeyName | string? | 云下载签名密钥在 `ICloudDriveSecretProtector` 中的 key ring 名称（运维注入，可为空表示未启用云下载） |
| CookieCooldownSeconds | int | Cookie 失效后的冷却期（默认 60 秒） |
| WebApiBaseUrl | string | 网页端 API 基础域名，默认 `https://webapi.115.com/` |
| MyApiBaseUrl | string | 用户中心 API 基础域名，默认 `https://my.115.com/` |
| VipApiBaseUrl | string | VIP 中心 API 基础域名，默认 `https://vip.115.com/` |
| PayApiBaseUrl | string | 支付中心 API 基础域名，默认 `https://pay.115.com/` |
| OfflineApiBaseUrl | string | 云下载 API 基础域名，默认 `https://115.com/web/lixian/` |
| DefaultRateLimit | int | 默认每秒最大请求数 |
| RateLimitOverrides | Dictionary<string, int>? | 按场景覆盖的 QPS 配置 |
| RateLimitOverflowStrategy | RateLimitOverflowStrategy | 超限后的处理方式（Queue / Reject） |
| RateLimitMaxQueueLength | int | 限流排队的最大队列长度 |
| ThrottleRetryMaxAttempts | int | 官方返回限流信号后的最大退避重试次数 |
| ThrottleRetryBaseDelayMilliseconds | int | 限流退避基础延迟 |
| ThrottleRetryMaxDelayMilliseconds | int | 限流退避最大延迟 |
| CacheTtlSeconds | int | 元数据缓存 TTL（秒） |
| LargeFileAsyncThresholdBytes | long | 超过该阈值的上传默认走后台任务 |
| OfficePreviewServiceUrl | string? | Office 文档第三方预览服务地址（可选） |
| CloudDriveFeatureEnabled | bool | 115 集成功能是否启用（启动期校验通过后设为 true，配置缺失时为 false） |

```csharp
/// 定义在 Application 层，与 CloudDriveAppConfig 配置类一起使用
public enum RateLimitOverflowStrategy
{
    Queue,
    Reject
}
```

### DTO 定义

_覆盖需求: 30_

#### 公共与认证相关 DTO

```csharp
public interface ICloudDriveAccountContextResolver
{
    Task<CloudDriveAccountContext> ResolveAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record CloudDriveAccountContext(Guid UserId, string CloudAccountUid, string CookieUid);

public class CloudDriveOperationResultDto
{
    public bool Accepted { get; set; }          // 是否转为后台任务；false 表示已同步完成
    public Guid? JobId { get; set; }            // Accepted=true 时必填
    public string? Message { get; set; }        // 结果或受理说明
}

// 运维 DTO：不纳入普通前端消费面
public class CloudDriveCookieSessionDto
{
    public string CloudAccountUid { get; set; }
    public CookieSessionStatus Status { get; set; }      // Active / Invalidated / Revoked
    public DateTime CreationTime { get; set; }
    public DateTime? LastModificationTime { get; set; }
    public DateTime? CooldownUntil { get; set; }         // 失效状态下的冷却期截止时间
}

// 内部探测 DTO：Controller 对外映射为 CloudDriveAuthCheckDto
public class CloudDriveLoginStatusDto
{
    public string CloudAccountUid { get; set; }
    public bool? IsLoggedIn { get; set; }                // true=有效，false=失效，null=未知（探测异常）
    public DateTime LastCheckedAt { get; set; }
    public DateTime? CooldownUntil { get; set; }         // 处于冷却期时返回截止时间
    public string? FailureReason { get; set; }           // 失效原因描述（统一脱敏）
}

public class CloudDriveAuthCheckDto
{
    public bool IsAuthenticated { get; set; }
    public string? FailureReason { get; set; }
}

public class CloudDriveUserInfoDto
{
    public string UserName { get; set; }       // 官方 user_name
    public string? AvatarSmallUrl { get; set; }  // user_face_s
    public string? AvatarMediumUrl { get; set; } // user_face_m
    public string? AvatarLargeUrl { get; set; }  // user_face_l
    public CloudDriveSpaceInfoDto? SpaceInfo { get; set; }
    public CloudDriveVipInfoDto? VipInfo { get; set; }
}

public class CloudDriveUserProfileDto
{
    public CloudDriveUserInfoDto? Info { get; set; }
    public CloudDriveSpaceInfoDto? Storage { get; set; }
    public CloudDriveVipInfoDto? Vip { get; set; }
    public CloudDriveBalanceDto? Balance { get; set; }
}

public class CloudDriveSpaceInfoDto
{
    public string AllTotal { get; set; }         // 总空间（格式化值）
    public string AllUse { get; set; }           // 已用空间（格式化值）
    public string AllRemain { get; set; }        // 剩余空间（格式化值）
    public long AllTotalBytes { get; set; }      // 总空间（字节）
    public long AllUseBytes { get; set; }        // 已用空间（字节）
    public long AllRemainBytes { get; set; }     // 剩余空间（字节）
}

public class CloudDriveVipInfoDto
{
    public string? LevelName { get; set; }       // VIP 等级名称
    public DateTime? Expire { get; set; }        // VIP 过期时间
}

public class CloudDriveBalanceDto
{
    public decimal? Balance { get; set; }        // 由 pay.115.com 或 vip.115.com 余额字段归一化
    public string? DisplayText { get; set; }
}

// 运维 DTO：不纳入普通前端消费面
public class CloudDriveAccountSummaryDto
{
    public string CloudAccountUid { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreationTime { get; set; }
}

public class UpsertCookieSessionInput
{
    public string CloudAccountUid { get; set; }           // 115 账号 UID
    public string Uid { get; set; }                       // Cookie UID
    public string Cid { get; set; }                       // Cookie CID
    public string Ussid { get; set; }                     // Cookie USSID
    public IDictionary<string, string>? ExtraFields { get; set; }  // 可选额外 Cookie 字段（如 acw_tc 等）
}

public class EnterCookieCooldownInput
{
    public string CloudAccountUid { get; set; }           // 运维指定账号 UID
    public string Reason { get; set; }                    // 失效原因（供日志与审计）
    public int? CooldownSeconds { get; set; }             // 可选覆盖默认冷却时长
}

public class RevokeCookieSessionInput
{
    public string CloudAccountUid { get; set; }           // 运维指定账号 UID
}
```

#### 关键输入 DTO（节选）

```csharp
public class GetFileListInput
{
    public string? DirectoryId { get; set; }    // 目录 ID，默认根目录 cid=0
    public int Offset { get; set; }
    public int Limit { get; set; } = 20;        // 最大 1150
    public int? Type { get; set; }              // 文件类型筛选：1=文档,2=图片,3=音乐,4=视频,5=压缩,6=应用,7=书籍
    public string? Suffix { get; set; }         // 文件后缀名筛选
    public int? Asc { get; set; }               // 排序方向：1=升序, 0=降序
    public string? OrderBy { get; set; }        // 排序字段：file__name, file__size, user__utime, file__type
    public int? CustomOrder { get; set; }       // 官方 custom_order
    public int? Stdir { get; set; }             // 官方 stdir
    public int? Star { get; set; }              // 星标过滤：1=仅星标, 0=全部
    public int? Cur { get; set; }               // 官方 cur
    public int? ShowDir { get; set; }           // 是否显示目录：0 或 1
}

public class SearchFileInput
{
    public string Keyword { get; set; }         // 查找关键字（官方 search_value）
    public int Offset { get; set; }
    public int Limit { get; set; } = 50;        // offset+limit 合计不得超过 10000
    public string? DirectoryId { get; set; }    // 目标目录 cid（-1=不返回列表内容）
    public string? Date { get; set; }           // 网页端 date 筛选（格式 2020-11-19）
    public string? PickCode { get; set; }       // 网页端 pick_code
    public int? CountFolders { get; set; }      // 网页端 count_folders，默认 1
    public string? Source { get; set; }         // 网页端 source
    public int? Type { get; set; }              // 文件类型大分类：1=文档,2=图片,3=音乐,4=视频,5=压缩包,6=应用
    public string Format { get; set; } = "json";
}

public class ToggleStarInput
{
    public string FileId { get; set; }          // 文件(夹) ID
    public bool Star { get; set; }              // true=添加星标, false=取消星标
}

public class GetFileInput
{
    public string? FileId { get; set; }         // 文件(夹) ID，与 Path 二选一
    public string? Path { get; set; }           // 文件路径（官方 2025-06-06 新增，分隔符支持 / 和 >），与 FileId 二选一
}

/// 上传初始化输入
/// 基础设施层须将 DirectoryId 转换为官方 target 格式 "U_1_{DirectoryId}"（如 U_1_0 表示根目录），
/// FileSha1 映射为官方 fileid，PreSha1 映射为官方 preid。
public class InitUploadInput
{
    public string DirectoryId { get; set; }     // 上传目标目录 ID（根目录为 "0"）
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public string FileSha1 { get; set; }        // 文件完整 SHA1（官方 fileid）
    public string? PreSha1 { get; set; }        // 文件前 128K SHA1（官方 preid）
}

public class UploadFileInput
{
    public string DirectoryId { get; set; }
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public string FileSha1 { get; set; }
    public string? PreSha1 { get; set; }
    public string? PickCode { get; set; }       // 断点续传时对应官方 pick_code
    public bool ForceAsync { get; set; }
    public string? TempFilePath { get; set; }
}

public class GetPreviewInput
{
    public string FileId { get; set; }
}

public class GetVideoPlayInput
{
    public string? FileId { get; set; }
    public string? PickCode { get; set; }       // 官方接口必需；若缺失则先由 FileId 解析
    public int? Definition { get; set; }        // 期望清晰度：1/2/3/4/5/100
}

public class SubmitTranscodeInput
{
    public string? FileId { get; set; }
    public string? PickCode { get; set; }       // 官方接口必需；若缺失则先由 FileId 解析
    public string Op { get; set; } = "vip_push"; // 转码方式：vip_push（VIP等级加速）或 pay_push（枫叶加速）
}

public class GetSubtitlesInput
{
    public string? FileId { get; set; }
    public string? PickCode { get; set; }       // 官方接口必需；若缺失则先由 FileId 解析
}

public class GetVideoPlayProgressInput
{
    public string? FileId { get; set; }
    public string? PickCode { get; set; }       // 官方接口必需；若缺失则先由 FileId 解析
}

public class RememberVideoPlayProgressInput
{
    public string? FileId { get; set; }
    public string? PickCode { get; set; }       // 官方接口必需；若缺失则先由 FileId 解析
    public long Time { get; set; }              // 当前播放进度（秒）
    public bool WatchEnd { get; set; }          // 是否播放完毕
}
```

#### 文件相关 DTO

```csharp
/// 文件列表项
public class CloudDriveFileDto
{
    public string FileId { get; set; }       // 文件 ID（115 返回的 fid/cid）
    public string Name { get; set; }         // 文件名
    public long Size { get; set; }           // 文件大小（字节）
    public string FileType { get; set; }     // 文件类型/扩展名
    public bool IsDirectory { get; set; }    // 是否为文件夹
    public DateTime ModifiedTime { get; set; } // 修改时间
    public string? PickCode { get; set; }    // 文件提取码（下载、播放、字幕等接口依赖此字段）
    public string? Sha1 { get; set; }        // SHA1 哈希
    public bool IsStar { get; set; }           // 是否星标（官方 ism 字段）
    public List<CloudDriveFileLabelDto>? Labels { get; set; } // 文件标签列表（官方 fl 字段）
    public bool IsVideo { get; set; }          // 是否为视频（官方 isv 字段）
    public long? PlayLong { get; set; }        // 音视频时长（秒）
    public int? VideoDefinition { get; set; }  // 视频清晰度（官方 def 字段：1=标清,2=高清,3=超清,4=1080P,5=4K,100=原画）
    public string? ThumbnailUrl { get; set; } // 缩略图地址（官方 thumb 字段）
    public string? OriginalImageUrl { get; set; } // 原图地址（官方 uo 字段，如有）
}

/// 文件详情
public class CloudDriveFileDetailDto : CloudDriveFileDto
{
    public DateTime CreatedTime { get; set; }  // 创建时间（官方 ptime）
    public long? SizeBytes { get; set; }       // 精确字节大小（官方 size_byte）
    public int? FileCount { get; set; }        // 文件数量（官方 count，文件夹时有值）
    public int? FolderCount { get; set; }      // 子文件夹数量（官方 folder_count）
    public long? PlayLong { get; set; }        // 视频时长（秒）
    public string? ParentId { get; set; }      // 父目录 ID
    public List<CloudDrivePathNodeDto>? Paths { get; set; } // 路径树（官方 paths 数组）
}

/// 路径树节点
public class CloudDrivePathNodeDto
{
    public string FileId { get; set; }         // 目录 ID
    public string FileName { get; set; }       // 目录名称
}

/// 文件标签（官方 fl 数组元素）
public class CloudDriveFileLabelDto
{
    public string Id { get; set; }             // 标签 ID
    public string Name { get; set; }           // 标签名称
    public string? Color { get; set; }         // 标签颜色
    public int Sort { get; set; }              // 标签排序
}

/// 搜索结果
public class CloudDriveSearchResultDto : CloudDriveFileDto
{
    public string ParentId { get; set; }       // 官方 parent_id
    public string? Path { get; set; }          // 可选路径；仅在系统可安全推导时返回
}

/// 文件夹信息
public class CloudDriveFolderDto
{
    public string FolderId { get; set; }
    public string Name { get; set; }
    public string? ParentId { get; set; }
    public string? Path { get; set; }
}

/// 上传初始化结果（内部 DTO，不直接暴露给当前前端代理上传流程）
public class CloudDriveUploadSessionDto
{
    public string? PickCode { get; set; }      // 官方上传任务标识/续传标识
    public int Status { get; set; }            // 1=非秒传，2=秒传
    public string? Target { get; set; }
    public string? Bucket { get; set; }
    public string? ObjectKey { get; set; }
    public string? Callback { get; set; }
    public string? CallbackVar { get; set; }
    public string? UploadEndpoint { get; set; } // OSS PostObject host，来自 sampleinitupload 返回的 host 字段
    public IDictionary<string, string>? FormFields { get; set; }
    public string? SignKey { get; set; }       // 二次认证 sign_key
    public string? SignCheck { get; set; }     // 二次认证 sign_check
    public DateTime? ExpiresAt { get; set; }
}

/// 上传成功后的文件信息
public class CloudDriveUploadedFileDto
{
    public string FileId { get; set; }
    public string Name { get; set; }
    public string? Path { get; set; }
}

/// 上传执行结果（同步完成或异步受理）
public class CloudDriveUploadExecutionDto : CloudDriveOperationResultDto
{
    public CloudDriveUploadedFileDto? File { get; set; }
}

/// 回收站项
/// 注意：回收站列表的 type 字段值域（1=文件, 2=目录）与文件列表的 fc 字段值域（0=文件夹, 1=文件）反转，
/// 基础设施层反序列化时须分别处理 IsDirectory 映射。
public class CloudDriveRecycleBinItemDto : CloudDriveFileDto
{
    public string RecycleBinItemId { get; set; } // 回收站项 ID（网页端 `id` 字段），用于 `webapi.115.com/rb/*` 还原/彻底删除的 `tid` 参数，与 FileId 不同
    public int RestoreStatus { get; set; }     // 回收站项状态（官方 status：-1=还原中, 0=正常）
    public DateTime DeletedTime { get; set; }
    public string? OriginalParentId { get; set; }
    public string? OriginalParentName { get; set; }
    public string? OriginalPath { get; set; }  // 仅在系统可安全推导时返回
}

/// 下载信息
public class CloudDriveDownloadDto
{
    public string DownloadUrl { get; set; }    // 临时下载地址或系统受控代理地址
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public bool IsTemporary { get; set; } = true;
}

```

#### 云下载相关 DTO

```csharp
public class GetOfflineTaskListInput
{
    public int Page { get; set; } = 1;
}

public class AddOfflineTaskInput
{
    public OfflineTaskKind TaskKind { get; set; } // Url 或 Bt
    public List<string>? Urls { get; set; }
    public string? InfoHash { get; set; }
    public IReadOnlyList<int>? Wanted { get; set; }
    public string? SavePath { get; set; }
    public string? TorrentSha1 { get; set; }
    public string? PickCode { get; set; }
    public string? SaveDirectoryId { get; set; }
}

public enum OfflineTaskKind
{
    Url,
    Bt
}

public class CancelOfflineTaskInput
{
    public string InfoHash { get; set; }
}

public class ClearOfflineTasksInput
{
    public string? Status { get; set; }
}

public class CloudDriveOfflineTaskDto
{
    public string InfoHash { get; set; }
    public string Name { get; set; }
    public long Size { get; set; }
    public string Status { get; set; }
    public decimal PercentDone { get; set; }
    public string? FileId { get; set; }
    public string? ParentId { get; set; }
}

public class CloudDriveOfflineQuotaDto
{
    public object? Package { get; set; }
    public int Count { get; set; }
    public int Surplus { get; set; }
    public long MaxSize { get; set; }
    public long Used { get; set; }
}

public class CloudDriveOfflineStatsDto
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
}
```

#### 预览相关 DTO

```csharp
/// 图片预览
public class CloudDriveImagePreviewDto
{
    public string PreviewUrl { get; set; }     // 临时预览地址或系统受控代理地址
    public string FileName { get; set; }
    public string FileType { get; set; }
    public bool IsTemporary { get; set; } = true;
}

/// 文档预览
public class CloudDriveDocPreviewDto
{
    public string PreviewUrl { get; set; }     // 临时下载/预览地址或系统受控代理地址
    public string FileName { get; set; }
    public string FileType { get; set; }
    public DocRenderMode RenderMode { get; set; } // Direct（PDF/TXT）、ThirdParty（Office）或 NotAvailable（未配置第三方服务）
    public string? ThirdPartyPreviewUrl { get; set; } // 第三方预览服务 URL（仅 Office 格式）
    public bool IsTemporary { get; set; } = true;
}

public enum DocRenderMode
{
    Direct,      // PDF、TXT - 前端直接渲染
    ThirdParty,  // Office 格式 - 需第三方预览服务
    NotAvailable // 第三方服务未配置
}
```

#### 视频相关 DTO

```csharp
/// 视频播放信息（映射自 `GET webapi.115.com/files/video` 响应）
/// 注意：网页端此接口 `pickcode` 通过 query string 传递，必须携带 `share_id=0` 表示非分享场景。
public class CloudDriveVideoPlayDto
{
    public VideoPlayStatus Status { get; set; } = VideoPlayStatus.Ready;
    public Guid? JobId { get; set; }           // 自动提交转码时返回
    public List<VideoQualityOption> QualityOptions { get; set; } // 多清晰度选项
    public List<AudioTrackOption> AudioTracks { get; set; }      // 官方多音轨信息
    public int Duration { get; set; }          // 视频时长（秒）
    public int? UserDef { get; set; }            // 官方记忆的用户上次选中清晰度（官方 user_def：1=标清,2=高清,3=超清,4=1080P,5=4K,100=原画）
    public int? SelectedDefinition { get; set; } // 本次用户请求的清晰度（前端传入）
    public int? UserRotate { get; set; }       // 记忆视频旋转角度（官方 user_rotate：0/90/180/270）
    public int? UserTurn { get; set; }         // 记忆视频翻转方向（官方 user_turn：0=不翻转, 1=水平, 2=垂直）
    public string? RestrictionReason { get; set; } // 4K/原画等受限原因
    public Dictionary<string, string>? PlaybackMetadata { get; set; } // 官方返回的播放器元数据
    public bool IsTemporary { get; set; } = true;
    public string? UsageRestrictionNotice { get; set; }
}

public enum VideoPlayStatus
{
    Ready,
    Transcoding
}

public class VideoQualityOption
{
    public int Definition { get; set; }        // 清晰度标识：1/2/3/4/5/100
    public int? DefinitionNew { get; set; }    // 新版清晰度标识（官方 definition_n）
    public string? Title { get; set; }         // 官方清晰度名称
    public string PlayUrl { get; set; }        // 播放地址
    public int? Height { get; set; }           // 视频高度（官方 height）
    public int? Width { get; set; }            // 视频宽度（官方 width）
}

public class AudioTrackOption
{
    public int TrackIndex { get; set; }        // multitrack_list 的索引，用于拼接 audio_track 参数
    public string Title { get; set; }
    public bool IsSelected { get; set; }
}

/// 视频转码状态
public class CloudDriveVideoTranscodeDto
{
    public string FileId { get; set; }
    public string Status { get; set; }         // 转码状态
    public Guid? JobId { get; set; }           // 关联的后台任务 ID
}

/// 视频播放进度（映射自 docs/115-api-docs.md 中的 webapi.115.com/files/history）
/// 读取：GET /files/history?pick_code={pickcode}&fetch=one&category=1&format=json；写入：POST /files/history form-urlencoded。
public class CloudDriveVideoPlayProgressDto
{
    public string FileId { get; set; }         // 官方 file_id
    public string FileName { get; set; }       // 官方 file_name
    public string Hash { get; set; }           // 官方 hash
    public string PickCode { get; set; }       // 官方 pick_code
    public long PlayedSeconds { get; set; }    // 已播放时长（秒）
    public long? AddTime { get; set; }         // 记录添加时间戳（官方 add_time）
}

/// 字幕列表
public class CloudDriveSubtitleListDto
{
    public CloudDriveSubtitleDto? AutoloadSubtitle { get; set; }
    public List<CloudDriveSubtitleDto> Items { get; set; } = [];
}

/// 字幕信息
public class CloudDriveSubtitleDto
{
    public string SubtitleId { get; set; }     // 字幕 ID (sid)
    public string Language { get; set; }       // 字幕语言
    public string Title { get; set; }          // 字幕标题
    public string SubtitleUrl { get; set; }    // 字幕下载地址
    public string? Type { get; set; }          // 字幕类型
    public string? Sha1 { get; set; }          // 官方 sha1
    public string? FileId { get; set; }        // 官方 file_id
    public string? FileName { get; set; }      // 字幕文件名
    public string? PickCode { get; set; }      // 关联视频的提取码
    public string? CaptionMapId { get; set; }  // 官方 caption_map_id
    public bool? IsCaptionMap { get; set; }    // 官方 is_caption_map
    public int? SyncTime { get; set; }         // 官方 sync_time
}

```

#### 任务相关 DTO

```csharp
/// 后台任务信息
public class CloudDriveJobDto
{
    public Guid Id { get; set; }
    public CloudDriveJobType JobType { get; set; }
    public CloudDriveJobStatus Status { get; set; }
    public string? ResourceId { get; set; }
    public int Progress { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

public class GetJobStatusInput
{
    public Guid JobId { get; set; }
}

public class GetJobListInput
{
    public CloudDriveJobStatus? Status { get; set; }
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; } = 20;
}
```

### 115 API 响应模型（基础设施层内部使用）

```csharp
/// 115 API 通用响应包装
/// 注意：授权 API 的 state 为 number（0=失败, 1=成功），业务 API 的 state 为 boolean（true/false）。
/// 使用自定义 JsonConverter 统一将两种格式反序列化为 bool。
public class CloudDrive115Response<T>
{
    [JsonConverter(typeof(BoolOrNumberJsonConverter))]
    public bool State { get; set; }            // 是否成功（兼容 number 0/1 和 boolean true/false）
    public int Code { get; set; }              // 业务状态码（业务 API 使用）
    public string? Message { get; set; }       // 业务消息（业务 API 使用）
    public int Errno { get; set; }             // 错误码（授权 API 使用）
    public string? Error { get; set; }         // 错误消息（授权 API 使用）
    public T? Data { get; set; }               // 业务数据
}

/// System.Text.Json 默认不支持 number→bool 反序列化，
/// 此转换器统一处理 115 API 中 state 字段的两种 JSON 类型。
public class BoolOrNumberJsonConverter : JsonConverter<bool>
{
    // Read: true/false → bool; 0 → false, 非0 → true
    // Write: 始终输出 boolean
}
```

> 说明：基础设施层解析 115 网页端响应的成败判定由 `IErrorFieldAdapter` 按 URL 路径动态解析（见组件章节）；默认语义为 HTTP 成功且响应中 `state` 字段（由 `BoolOrNumberJsonConverter` 将 `1` 等数值转为 `true`）为 `true` 且 `ErrorFieldAdapter` 返回的 `CloudDriveApiError` 为 null 时视为成功；否则映射为适当的 `CloudDrive*Exception`（见错误码映射表）。

> **网页端特殊接口行为说明（依据 `requirements.md` 采集）：**
> - **文件列表/搜索响应**：字段归一化映射（`fid → FileId`、`n → Name`、`s → Size`、`fc` 值域转换等）由 Infrastructure 层反序列化字段适配器处理；详具体归一化表 `requirements.md:112` 与 `requirements.md:125`。
> - **视频播放** `GET webapi.115.com/files/video`：`pickcode` 通过 query string 传递，返回网页端多清晰度 URL 列表；必须携带 `share_id=0` 以表示非分享场景（信息来自采集样本，见 `requirements.md:302`）。
> - **视频字幕** `GET webapi.115.com/movies/subtitle`：错误字段使用大写 `errNo` （注里名标注大写 N）+ `error`，由 `ErrorFieldAdapter` 单独配置。
> - **回收站列表** `GET webapi.115.com/rb`：必须携带 `aid=7`、`cid=0`、`format=json` 三个必填参数（`aid=1` 为正常目录，`aid=7` 为回收站）；响应 `data` 在非空回收站下为动态键对象（键为文件 ID string），空回收站官方不返回 `data` 键。
> - **云下载系列接口** `115.com/web/lixian/*`：所有请求必须追加 `?ct=lixian&ac={操作符}`；Body 需携带 `sign + time + uid` 三元组，由 `ICookieSignatureStrategy` 生成；`task_lists` 等接口的补充参数 `stat`（默认 0 表示全部）。
> - **分享接口** `webapi.115.com/share/slist` 、`/share/send`、`/share/receive`：均必须携带 `format=json`；`send` 与 `receive` 的错误字段优先读 `errNo`，fallback `errno`（见 `requirements.md:464`）。
> - **用户聚合接口**：`my.115.com/proapi/3.0/index.php?method=user_info&uid=...`（主资料）、`webapi.115.com/user/vip_limit?feature=2`（VIP 配额，错误字段 `code` string）、`webapi.115.com/files/index_info?count_space_nums=1`（存储空间，错误字段 `code` number + `error`）、`vip.115.com/*`（VIP 响应包含 `00_balance` 索引访问归一化为 `balance`）、`pay.115.com/`（错误字段 `msg`、非 `message`）。
> - **上传链路**：依据 `docs/115-api-docs.md` 2026-04-18 手动补充采集，先调用 `POST uplb.115.com/3.0/sampleinitupload.php` 获取秒传结果或 OSS PostObject 凭证，再向返回的 OSS `host` 直传文件；当前前端代理上传只调用后端 `POST /api/files/upload`，不暴露 `/files/upload/init`。

### 错误码映射

系统将 115 网页端错误码映射为内部业务异常，统一通过 `ErrorFieldAdapter` 按 URL 路径解析原始错误字段后再分类：

| 场景 | 异常类型 | 说明 |
|------|----------|------|
| Cookie 失效 / 登录态无效 | `CloudDriveCookieInvalidatedException` | `ErrorFieldAdapter` 识别到已知的登录失效类错误码，例如 `errNo=login_required`；同时发布 `CookieInvalidated` 领域事件并令 `CookieSession` 进入冷却期 |
| HTTP 401 / 403 | `CloudDriveCookieInvalidatedException` | Cookie 不有效或被服务端拒绝访问；同样触发失效标记与冷却 |
| HTTP 404 / 业务不存在 | `CloudDriveFileNotFoundException` | 文件、目录或回收站资源不存在 |
| HTTP 429 / 业务频控错误码 | `CloudDriveRateLimitException` | 触发频控或风控限制，`CloudDriveRateLimiter.RecordThrottleSignal` 同步接收信号 |
| HTTP 400 / 业务请求错误 | `CloudDriveApiException` | 网页端接口返回请求非法、参数错误或业务操作不允许 |
| HTTP 5xx / 网关异常 | `CloudDriveApiException` | 115 服务异常或不可用 |
| `ErrorFieldAdapter` 未命中查找规则 | `CloudDriveApiException` | 带一个 `unknown_error_shape` 错误码，保留原始 JSON 脱敏后的摘要供排查 |
| 网络超时 / 连接失败 | `CloudDriveNetworkException` | 区分 timeout 与 connection failure 并保留原因 |
| 配置缺失 / `CloudDriveFeatureEnabled == false` | `CloudDriveConfigurationException` | 启动期校验失败或运行期保护检测到功能被禁用 |

---

## 正确性属性（Properties）

以下属性定义了系统必须满足的正确性约束，用于指导测试和验证。

### P1: Cookie 安全性

- **属性**: 对于任意 Cookie 存储操作，UID/CID/USSID 三元组与可选额外字段在持久化层中必须以加密形式存储；在同一部署 key ring 下，服务重启后仍必须能够解密既有 Cookie 和云下载签名密钥；且在日志输出、对外响应与异常中不得包含明文 Cookie
- **关联需求**: 需求 1 (AC1, AC2), 需求 2 (AC1, AC2), 需求 26 (AC1, AC2)
- **验证方式**: 单元测试验证 `CookieSessionManager.UpsertSessionAsync` 存储的值经过加密；集成测试验证在同一 key ring 下服务重启后仍可成功解密且日志输出由 `CloudDriveSensitiveFieldsFilter` 脱敏后不包含原始 Cookie 字符串

### P2: Cookie 失效处理

- **属性**: 对于任意使用 Cookie 的 API 调用，若 `CloudDriveApiClient` 探测到 Cookie 失效（`errNo=login_required` 或 HTTP 401/403），则系统必须：(1) 立即将 `CookieSession.Status` 标记为 `Invalidated` 并设置 `CooldownUntil`；(2) 抛出 `CloudDriveCookieInvalidatedException` 而非模糊错误；(3) 发布 `CookieInvalidated` 领域事件；(4) 在冷却期内不重复发起探测请求。系统不尝试自动恢复失效 Cookie（必须由运维重新注入）
- **关联需求**: 需求 1 (AC4, AC5, AC6, AC7, AC8), 需求 3 (AC3, AC4)
- **验证方式**: 集成测试模拟 `errNo=login_required` 与 HTTP 401/403 两种失效场景，验证 `CookieSession.Status` 被标记为 `Invalidated`、`CookieInvalidated` 事件被发布，冷却期内的重复探测返回缓存结果而不发起新的官方请求

### P3: 频控限流一致性

- **属性**: 对于任意时间窗口内的 API 请求序列，实际发往 115 网页端的请求数不得超过配置的每秒最大请求数（按用户 + 115 账号 + 场景维度）
- **关联需求**: 需求 17 (AC1, AC2, AC3)
- **验证方式**: 单元测试验证 `CloudDriveRateLimiter` 在并发请求下，通过的请求数不超过配置上限

### P4: 缓存一致性

- **属性**: 对于任意写操作（上传、移动、复制、删除、重命名、还原、清空回收站），操作成功后受影响目录的缓存必须被失效或刷新；对于任意手动刷新请求，系统必须绕过或重建缓存并返回官方最新数据
- **关联需求**: 需求 20 (AC2, AC3)
- **验证方式**: 集成测试验证写操作后缓存被清除；验证用户显式刷新目录时绕过旧缓存并返回最新官方数据

### P5: 文件名验证

- **属性**: 对于任意文件/文件夹创建或重命名操作，若名称为空或包含非法字符，则系统必须拒绝操作并返回明确的验证错误，而非将无效名称传递给 115 API
- **关联需求**: 需求 7 (AC2, AC4), 需求 12 (AC2, AC4)
- **验证方式**: 单元测试验证 `CloudDriveFileNameValidator.ValidateFileName` 对空字符串和非法字符抛出 `CloudDriveInvalidNameException`

### P6: 错误码映射完整性

- **属性**: 对于 115 网页端 API 返回的任意非成功响应（HTTP 错误、HTTP 成功但 `state == false` / `errNo != 0` / `errCode != 0` / `code != 0` / `errno != 0` 等多种字段变体）以及网络超时/连接失败，系统必须由 `IErrorFieldAdapter` 根据 URL 路径查找对应的字段规则并映射为正确的内部业务异常，而非抛出未处理的原始异常
- **关联需求**: 需求 25 (AC1, AC2, AC3, AC4, AC5, AC6)
- **验证方式**: 单元测试验证 `ErrorFieldAdapter` 按 URL 路径正确解析需求 25 错误字段变体表中的全部 URL 规则（包括 `/files/search` 的 `errCode` number + `error`、`/movies/subtitle` 的大写 `errNo`、`user/vip_limit` 的 string 型 `code`、`files/index_info` 的 number 型 `code`、`pay.115.com/` 的 `msg`、`share/send|receive` 的双写 `errNo/errno` 等），并进一步映射为 `CloudDriveCookieInvalidatedException`、`CloudDriveRateLimitException`、`CloudDriveFileNotFoundException`、`CloudDriveApiException`、`CloudDriveNetworkException`、`CloudDriveConfigurationException` 中对应的类型

### P7: 预览类型路由正确性

- **属性**: 对于任意文件预览请求，系统必须根据文件扩展名正确路由到对应的预览策略（图片→下载地址渲染，视频→可播放地址或 Transcoding 状态，PDF/TXT→直接渲染，Office→第三方渲染或不可用），且不支持的格式必须返回明确的不支持提示
- **关联需求**: 需求 14 (AC2, AC3), 需求 15 (AC5), 需求 16 (AC2, AC3, AC4, AC6)
- **验证方式**: 单元测试验证图片/文档/视频扩展名被正确路由到对应预览策略；验证 Office 文档在未配置第三方预览服务时返回 `NotAvailable`；验证不支持的格式返回错误

### P8: 配置完整性校验

- **属性**: 系统启动时，若 `CookieEncryptionKeyName`、`WebApiBaseUrl` 或（如启用云下载）`SignatureKeyName` 任一核心配置缺失，则 `CloudDriveFeatureEnabled` 必须被设为 `false`；在应用运行期任何 Handler 进入前必须检测到该标志并抛出 `CloudDriveConfigurationException`，不得尝试调用任何 115 API
- **关联需求**: 需求 2 (AC2, AC3, AC4)
- **验证方式**: 集成测试验证上述配置缺失时，所有 115 相关 Handler 方法抛出 `CloudDriveConfigurationException`；正确配置下不抛出该异常

### P9: 临时资源标识

- **属性**: 对于任意返回下载地址、播放地址或预览地址的响应 DTO，`IsTemporary` 字段必须为 `true`
- **关联需求**: 需求 6 (AC5), 需求 14 (AC1), 需求 15 (AC2, AC12), 需求 16 (AC1)
- **验证方式**: 单元测试验证所有返回临时资源的 DTO 在构造与映射为对外响应后均保持 `IsTemporary = true`

### P10: 用户数据隔离

- **属性**: 对于任意缓存读写操作，不同用户、不同 115 账号的缓存数据必须完全隔离；用户 A 或账号 X 的缓存键不得与用户 B 或账号 Y 的缓存键冲突或交叉访问
- **关联需求**: 需求 20 (AC5)
- **验证方式**: 单元测试验证 `CloudDriveMetadataCache` 为不同 userId / cloudAccountUid 生成不同的缓存键，且一个用户或账号的失效操作不影响另一个用户或账号的缓存

### P11: 缓存数据真实性

- **属性**: 对于任意写入 `CloudDriveMetadataCache` 的条目，缓存字段必须来源于官方实际响应或系统可验证的派生值；系统不得凭空生成官方未提供且不可验证的元数据
- **关联需求**: 需求 20 (AC4)
- **验证方式**: 属性测试验证缓存映射仅包含官方响应字段和可验证派生字段；当官方缺失字段时返回空值或缺省值而非伪造数据

### P12: 失败响应语义一致性

- **属性**: 对于任意 Handler 返回的 `Result.Failure(error)`，表现层必须将其转换为非 2xx 的 `ProblemDetails` 响应，并保留稳定的业务错误码；系统不得以 HTTP 200 表示失败
- **关联需求**: 需求 18 (AC7)
- **验证方式**: 集成测试验证 Controller 对典型业务失败（如名称非法、文件不存在、配置禁用）输出预期状态码与 `application/problem+json`

### P13: 后台任务重启恢复协调

- **属性**: 对于任意在进程重启前处于 `Pending` 或 `Running` 状态且具备可恢复检查点的后台任务，系统启动后必须重新协调其状态：可恢复任务重新调度，不可恢复任务标记为失败并记录原因
- **关联需求**: 需求 21 (AC4, AC6)
- **验证方式**: 集成测试模拟进程中断后重新启动，验证恢复协调服务会根据 `PayloadJson` 与 `CheckpointData` 重新调度或失败收敛任务

---

## 需求↔设计覆盖矩阵

| 需求 | 覆盖设计组件 |
|------|------------|
| 需求 1 | §1. 认证与会话管理模块 (Auth), §实体与值对象 |
| 需求 2 | §1. 认证与会话管理模块 (Auth), §实体与值对象 |
| 需求 3 | §1. 认证与会话管理模块 (Auth) |
| 需求 4 | §2. 文件管理模块 (File) |
| 需求 5 | §2. 文件管理模块 (File) |
| 需求 6 | §2. 文件管理模块 (File) |
| 需求 7 | §2. 文件管理模块 (File) |
| 需求 8 | §2. 文件管理模块 (File) |
| 需求 9 | §2. 文件管理模块 (File) |
| 需求 10 | §2. 文件管理模块 (File) |
| 需求 11 | §2. 文件管理模块 (File) |
| 需求 12 | §2. 文件管理模块 (File) |
| 需求 13 | §2. 文件管理模块 (File) |
| 需求 13a | §2. 文件管理模块 (File) |
| 需求 14 | §2. 文件管理模块 (File) |
| 需求 15 | §3. 预览模块 (Preview) |
| 需求 16 | §4. 视频模块 (Video) |
| 需求 17 | §4. 视频模块 (Video) |
| 需求 18 | §4. 视频模块 (Video) |
| 需求 19 | §3. 预览模块 (Preview) |
| 需求 20 | §6. 云下载模块 (Offline) |
| 需求 21 | §6. 云下载模块 (Offline), P13 |
| 需求 22 | §7. 分享管理模块 (Share) |
| 需求 23 | §8. 用户中心聚合模块 (UserAggregator) |
| 需求 24 | §CloudDriveRateLimiter, P3 |
| 需求 25 | §ICloudDriveOpenApiGateway / CloudDriveApiClient, §IErrorFieldAdapter / ErrorFieldAdapter, §错误码映射, P6, P12 |
| 需求 26 | §ICloudDriveOpenApiGateway / CloudDriveApiClient |
| 需求 27 | §CloudDriveMetadataCache, P4, P11 |
| 需求 28 | §5. 后台任务模块 (Quartz.NET Job), §实体与值对象 |
| 需求 29 | §9. 基础设施组件 |
| 需求 30 | §DTO 定义 |
