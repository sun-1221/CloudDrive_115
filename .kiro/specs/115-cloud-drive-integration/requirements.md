# 需求文档：115 网盘集成

## 简介

本功能为 .NET 10 原生 DDD + CQRS 项目集成 115 网盘 **网页端原生 API**。系统先通过本地 User/UserSession 完成个人 JWT 会话认证，再按当前 UserSession 选择的 CookieSession 调用 `webapi.115.com`、`clouddownload.115.com`、`vip.115.com`、`pay.115.com` 等网页端 API，为前端提供标准化 REST 端点。能力是否启用严格取决于已验收脱敏 HTTP 契约；没有闭环证据的入口保持禁用。

> **架构定位**：本 spec 定义 115 网盘**后端业务模块**，基于 `.kiro/specs/115-cloud-drive-net10/` 已搭建的 .NET 10 DDD 四层框架实现。前端 Vue 3 应用通过本模块提供的 `/api/*` REST 端点访问 115 网盘数据。

> **数据源权威性**：本系统仅对接 **115 网页端原生 API**。接口事实只以 `docs/115-api-docs.md` 记录的 2026-08-09 已验收脱敏 capture result 为权威；其他历史采集、外部文档和第三方实现均不得补齐 method、path、参数、响应或错误字段。

> **认证方式**：本地用户通过用户名/密码获取 JWT；JWT 仅包含用户 ID、UserSession ID 与生命周期字段，不携带角色、115 Cookie 或账号凭据。115 Cookie 由已认证用户通过账号绑定 API 提交，后端通过 ASP.NET Core Data Protection 加密存储，请求 115 API 时按当前 UserSession 的 `ActiveCloudAccountUid` 自动注入。前端不读取、传递或记录 Cookie 原文。

> **合规声明**：系统使用的是用户在 115 网页端登录后的 Cookie 会话，归属其本人。系统不得将 API 用于图床、软件床、视频外链播放、OSS 对象存储等违反 115 服务条款的场景。

> **账号模型**：**个人多账号模型**。每个用户可绑定多个 115 账号；每个 UserSession 独立保存当前账号，未选择且存在多个 Active 账号时请求必须 fail-closed。

> **API 覆盖说明**：已验收 evidence 包含两批共 116 条结构化脱敏记录和四个固定 SHA-256 清单。可启用：文件列表、下载地址、新建、重命名、星标、单样本复制、回收站非空列表、视频播放、播放进度读取、字幕空列表、我的分享非空列表、云下载配额/计数/空列表、VIP/余额/空间。保持禁用：搜索、上传、移动、删除、回收站写操作、播放进度写入、主动转码、云下载写操作及非空任务 item、分享 send/receive 成功列表、基础资料、字幕非空 item、清晰度与音轨。网页 UI 成功但未取得 HTTP body 的动作不得视为接口完成。

> **范围排除声明**：`docs/115-api-docs.md` collected 闭集之外的所有 host/path/query 组合均不纳入当前运行时，不为其建立错误规则、DTO、缓存或后台任务。

> **分页上限说明**：`docs/115-api-docs.md` 只记录 observed `offset` / `limit`，没有建立网页端硬上限。服务资源保护只能通过系统自有配置（如 `CloudDrive:MaxPageSize`）实现，并在 ProblemDetails 中标注为系统限制。

## 前置要求与约束

1. 本系统 SHALL 只调用 `docs/115-api-docs.md` 标记为 collected 且已启用的 115 网页端契约
2. 本系统 SHALL 以本地 User/UserSession JWT 和当前用户绑定的有效 CookieSession（UID/CID/USSID）为前提；Cookie 只能通过当前用户账号绑定用例写入
3. IF Data Protection、CookieSession 或当前账号上下文缺失、格式无效或失效，THEN 系统 SHALL 在 Gateway 前禁用对应 115 请求并返回稳定错误
4. 所有下载/播放/缩略图地址 SHALL 仅供当前授权用户在本系统内短时使用，不得持久化为公开链接
5. 对于高频浏览、媒体扫描、批量操作场景，系统 SHALL 结合限流、缓存和后台任务策略，降低频控与风控风险

## 术语表

- **Cloud_Drive_Service**：115 网盘集成服务的核心服务层（应用编排与业务用例入口的统称）
- **CloudDrive_Module**：Application 层 `src/Application/CloudDrive/` 业务模块（按 Auth/Files/Search/Video 等子领域组织）
- **WebApi_Layer**：表现层（`src/WebApi/`），提供 REST Controllers、OpenAPI 与 HTTP 适配
- **Controllers**：WebApi 层 REST 控制器集合（如 `AuthController`/`FilesController`），仅通过 `ISender`/`IMediator` 调用 Application
- **Infrastructure_Layer**：基础设施层（`src/Infrastructure/CloudDrive/`），实现 Gateway、仓储、缓存、限流与外部 HTTP 客户端
- **Gateway_Layer**：访问 115 网页端 API 的网关抽象层；物理实现为单一 `ICloudDriveWebApiGateway` / `CloudDriveApiClient`（不得按子域拆出多套并行 HTTP 客户端）
- **FileGateway**：`Gateway_Layer` / `ICloudDriveWebApiGateway` 的逻辑别名（需求叙述用语；无独立物理类型）
- **User**：本地个人用户，不包含管理员或角色语义
- **UserSession**：本地 JWT 登录会话，保存撤销状态、过期时间和 `ActiveCloudAccountUid`
- **CurrentAccount**：当前 UserSession 选择的 Active 115 账号
- **ActiveCloudAccountUid 映射**：HTTP 账号字段为 `cloudAccountUid`，领域属性为 `UserSession.ActiveCloudAccountUid`，数据库列为 `active_cloud_account_uid`
- **CookieSession**：用户与 115 账号的绑定凭据记录，Cookie 三元组由 Data Protection 加密
- **Cookie_Manager** / **CookieManager**：Cookie 会话编排的逻辑别名；物理实现为应用层 `CookieSessionManager`（按 UserId + CloudAccountUid 加密存储、注入、失效和冷却）
- **ExceptionHandlingMiddleware**：WebApi 全局异常中间件，将已分类异常与失败结果映射为 `ProblemDetails`（**不**负责解析 115 响应错误字段）
- **ErrorFieldAdapter**：Gateway/ApiClient 解析路径中的错误字段适配器（`IErrorFieldAdapter`），按 URL 路径解析 115 错误字段变体并映射为内部异常
- **LoggingBehavior**：MediatR Pipeline Behavior，负责结构化请求日志与敏感字段脱敏
- **Auth_Service**：个人 JWT 会话与 CookieSession 账号上下文服务
- **File_Service**：文件管理服务
- **Preview_Service**：文件预览服务
- **Video_Service**：视频管理服务
- **Offline_Service**：云下载（离线下载）服务
- **Share_Service**：分享管理服务
- **User_Service**：用户中心服务（聚合基础信息/VIP/余额/存储空间）
- **Signature_Service**：云下载写签名的禁用扩展点；算法未采集时只返回 `offline_signature_not_available`，不得生成占位签名
- **Rate_Limiter**：按场景控制 115 API 调用频率
- **Metadata_Cache**：元数据缓存
- **Background_Job**：基于 Quartz.NET `IJob` 的后台任务
- **Pick_Code**：文件提取码，115 唯一标识符
- **Normalized_Dto**：归一化 DTO，屏蔽网页端短字段名
- **Problem_Details**：`application/problem+json` 统一错误响应

## 需求

### 需求 1：个人 JWT 会话与 115 Cookie 上下文

**用户故事：** 作为个人用户，我希望通过本地账号登录，并在访问 115 网盘时使用自己绑定的 Cookie 会话。

#### 验收标准

1. WHEN 匿名用户注册或登录成功 THEN 系统 SHALL 创建或恢复 UserSession，并返回仅包含用户 ID、会话 ID 和生命周期字段的 Bearer JWT；JWT 不包含角色、Cookie 原文或账号凭据。
2. THE WebApi_Layer SHALL 对除公开认证端点外的业务端点强制 JWT 认证；缺少有效 JWT、会话已撤销或已过期时返回 HTTP 401。
3. THE Cookie_Manager SHALL 通过 ASP.NET Core Data Protection 加密并持久化用户提交的 UID/CID/USSID；任何日志、异常、ProblemDetails 或响应均不得泄露 Cookie 原文或片段。
4. WHEN 当前 UserSession 具备有效 `ActiveCloudAccountUid` THEN Gateway_Layer SHALL 使用该用户对应 CookieSession 注入 115 请求；不得要求前端重复提交账号 UID。
5. IF CookieSession 缺失、失效或处于冷却期 THEN 系统 SHALL 在 Gateway 前短路并返回稳定业务错误，不得发起 115 请求。
6. IF 115 API 响应表示 Cookie 失效 THEN Cookie_Manager SHALL 标记对应 CookieSession、发布 CookieInvalidated 领域事件并进入冷却期。
7. THE Cookie_Manager SHALL 不从环境变量或全局用户配置读取用户 Cookie；Cookie 仅能通过当前用户的绑定用例写入。

### 需求 2：多账号绑定、选择与解绑

**用户故事：** 作为已认证用户，我希望绑定多个 115 账号，并在每个登录会话中选择当前账号。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET/POST/DELETE /api/cloud-drive/accounts` 与 `POST /api/cloud-drive/accounts/select`；所有接口均要求有效 JWT。
2. WHEN 用户提交账号 UID 与 Cookie 三元组 THEN 系统 SHALL 创建或更新该用户的 CookieSession；相同用户和账号 UID 不得产生重复有效绑定。
3. WHEN 用户查询账号列表 THEN 系统 SHALL 只返回 UID、状态和时间等摘要，不返回 Cookie 原文；WHEN 用户解绑 THEN 系统 SHALL 撤销目标 CookieSession。
4. WHEN 用户选择 Active 账号 THEN 系统 SHALL 将 UID 保存到当前 UserSession 的 `ActiveCloudAccountUid`；选择不存在或非 Active 账号返回稳定业务错误。
5. IF 当前用户存在多个 Active 账号且当前 UserSession 未选择账号 THEN 普通业务 API SHALL 不向 115 发起请求，并返回 `cloud_drive_account_context_ambiguous`。

### 需求 3：认证与上游登录态检测 API

**用户故事：** 作为前端工程师，我希望分别得到本地 JWT 会话状态和当前用户 115 账号的安全状态。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `POST /api/auth/register`、`POST /api/auth/login`、`POST /api/auth/logout`、`POST /api/auth/refresh`、`GET /api/auth/me`。
2. THE WebApi_Layer SHALL 保留 `GET /api/auth/check` 作为上游登录态探测端点；该端点允许匿名访问，但在存在 JWT 时只探测当前用户的当前 CookieSession。
3. WHEN `/api/auth/check` 收到请求时，系统 SHALL 返回 camelCase 的 `isAuthenticated`、`isLoggedIn`、`lastCheckedAt`、可选 `cooldownUntil` 和安全的 `failureReason`，并始终满足 `isAuthenticated === (isLoggedIn === true)`。
4. IF 没有当前用户、没有 Active CookieSession、Cookie 处于冷却期或账号上下文不明确 THEN `/api/auth/check` SHALL 短路且不发起 115 请求。
5. IF 探测成功 THEN /api/auth/check SHALL 返回 isLoggedIn=true、isAuthenticated=true 并省略失败原因。
6. IF 探测因网络异常失败 THEN /api/auth/check SHALL 返回 isLoggedIn=null、isAuthenticated=false 和 failureReason=network。

### 需求 4：获取文件列表

**用户故事：** 作为用户，我希望浏览 115 网盘中的文件和文件夹。

#### 验收标准

1. WHEN 用户请求获取指定目录的文件列表时，THE File_Service SHALL 调用 `GET webapi.115.com/files`；已保留的非敏感控制值包括 `aid=1,format=json`，当前未通过省略实验证明任何上游参数必填性。应用默认根目录可发送 `cid=0`，但不得把该默认值写成上游必填规则
2. THE File_Service SHALL 采用 `offset + limit` 分页，默认 `offset=0`、`limit=50`；最大值只受系统自有保护配置约束
3. THE File_Service SHALL 只传递已观察到的 query：`aid,asc,cid,count_folders,custom_order,fc_mix,format,limit,natsort,nf,o,offset,show_dir,stdir,type`；其必填性和枚举未建立，未列参数不得自行加入
4. THE File_Service SHALL 归一化网页端响应字段为 `CloudDriveFileDto`：`fid→fileId, n→fileName, s→size, fc→rawCategoryCode, pc→pickCode, tu→thumb, u→originalUrl, tp→videoImg, iv→isVideo, fvs→videoDefinition, play_long→playDuration, te→uploadTime, t→updateTime, m→isStarred, hdf→isPrivate, sta→uploadStatus, sha→sha1, ico→ico, class→fileClass`；未建立值域的字段只保留 raw/null 语义，不得硬编码枚举
5. THE File_Service SHALL 将已采集响应键 `path` 归一化为公开契约 `paths`（`CloudDrivePathNodeDto[]`），并只映射 `aid,cid,isp,name,pid`
6. WHEN 未指定目录时，THE File_Service SHALL 默认返回根目录（`cid=0`）
7. IF 目录不存在，THEN 返回 HTTP 404 ProblemDetails
8. WHEN `offset < 0`、`limit <= 0` 或 `limit` 超出系统自有 `CloudDrive:MaxPageSize` 配置时，THE File_Service SHALL 抛出 `ValidationException`，返回 HTTP 400 ProblemDetails；错误码 SHALL 表示系统保护限制，不得表述为 115 网页端官方上限

### 需求 5：文件搜索

**用户故事：** 作为用户，我希望在网盘中搜索文件。

#### 验收标准

1. WHILE `GET webapi.115.com/files/search` 没有出现在已验收证据中，THE WebApi_Layer SHALL 将 `GET /api/files/search` 标记为 disabled，并返回 HTTP 501 ProblemDetails，`extensions.code='cloud_drive_api_not_collected'`
2. THE File_Service SHALL NOT 发起任何 `/files/search` 上游请求，也不得沿用旧 manual-capture 的参数表
3. THE File_Service SHALL NOT 声明 `search_value/date/pick_code/type` 等参数的必填性、默认值或值域
4. WHEN 关键词为空或纯空白时，THE WebApi_Layer MAY 先返回 HTTP 400 ValidationProblemDetails，但有效关键词仍不得越过契约门禁
5. WHEN `offset < 0`、`limit <= 0` 或 `limit` 超出系统保护配置时，THE WebApi_Layer MAY 返回 HTTP 400 ValidationProblemDetails，但不得将该校验描述为 115 限制
6. THE CloudDriveSearchListDto SHALL 将 `itemSchemaStatus` 置为 `not_collected`，不得构造 items
7. THE File_Service SHALL NOT 映射旧搜索 item 字段或高亮片段
8. THE File_Service SHALL NOT 将搜索空结果与未采集状态混为一谈
9. WHEN 新的脱敏成功证据通过验收后，THEN THE File_Service SHALL 只按新证据更新参数、顶层和 item schema
10. UNTIL AC 5.9 完成，THE Frontend_App SHALL 保持搜索入口禁用

### 需求 6：查看文件详情

**用户故事：** 作为用户，我希望查看单个文件或文件夹的详细属性。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET /api/files/{fileId}` 端点
2. THE File_Service SHALL 仅从已采集文件列表条目与 `path` 上下文提取字段返回；独立网页端详情接口未采集且不得调用
3. THE File_Service SHALL 返回可获取属性：`fileId`/`fileName`/`pickCode`/`sha1`/`uploadTime`/`updateTime`/`size`/`isVideo`/`playDuration`/`thumb`/`originalUrl`；未返回字段使用 `null` 而非伪造
4. IF 文件不存在，THEN 返回 HTTP 404 ProblemDetails

### 需求 7：文件上传

**用户故事：** 作为用户，我希望将本地文件上传到 115 网盘。

> **状态**：**not-collected / disabled**。旧上传样本不在本轮已验收脱敏 evidence 中，不能作为实现依据。

#### 验收标准

1. WHILE 上传 HTTP 契约未通过新的脱敏 evidence 验收，THE WebApi_Layer SHALL 将 `POST /api/files/upload` 标记为 disabled，并返回 HTTP 501 + `cloud_drive_api_not_collected`
2. THE File_Service SHALL NOT 调用旧样本中的上传初始化路径
3. THE File_Service SHALL NOT 依据旧样本声明秒传判定条件
4. THE File_Service SHALL NOT 保存、记录或复用任何 OSS access ID、policy、signature、callback、host 或临时凭据
5. THE File_Service SHALL NOT 向外部对象存储发送文件
6. THE File_Service SHALL NOT 构造上传成功 DTO
7. THE WebApi_Layer SHALL NOT 接受目标目录写入请求并假装已排队
8. THE Background_Job SHALL NOT 创建上传任务或持久化本地临时文件路径
9. THE Frontend_App SHALL 禁用上传入口并展示“接口契约待采集”说明
10. IF 请求命中禁用端点，THEN 返回 501，不得转写为网络失败
11. THE ErrorFieldAdapter SHALL NOT 注册旧上传错误字段
12. WHEN 新的脱敏上传成功与错误 evidence 完整闭环后，THEN THE File_Service SHALL 再定义归一化成功 DTO
13. UNTIL AC 7.12 完成，THE File_Service SHALL NOT 声明 SHA1/PreSha1、秒传或分片规则

### 需求 8：文件下载

**用户故事：** 作为用户，我希望从 115 网盘下载文件。

> **状态**：**collected**。已取得 `GET webapi.115.com/files/download` 成功契约；URL 与标识值未保存。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET /api/files/{pickCode}/download` 端点
2. THE File_Service SHALL 调用 `GET webapi.115.com/files/download`，query 只传递 observed `_,dl,pickcode`；`_` 语义、`dl` 枚举和必填性未建立
3. THE File_Service SHALL 返回临时下载 URL 或受控代理地址
4. IF 文件不存在，THEN 返回 HTTP 404 ProblemDetails
5. IF 下载地址获取失败，THEN 返回 HTTP 502 ProblemDetails
6. THE File_Service SHALL 只映射顶层 `file_id,file_name,file_size,file_url_302,is_115chrome,is_snap,is_vip,msg,msg_code,pickcode,state,user_id`，并在归一化响应中标识地址临时性；不得记录 URL、文件名、pickCode 或账号标识
7. IF 下载链接过期，THEN 前端可再次请求 `/api/files/{pickCode}/download` 获取新链接

### 需求 9：新建文件夹

**用户故事：** 作为用户，我希望在网盘中创建新文件夹。

> **状态**：**collected**。3 个真实成功样本。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `POST /api/files/folders` 端点
2. THE File_Service SHALL 调用 `POST webapi.115.com/files/add`，无 query，form body observed `cname,pid`；必填性未通过省略实验建立
3. THE File_Service SHALL 验证名称非空、不含非法字符（`/ \ : * ? " < > |`）、长度 ≤ 255 字符
4. IF 父目录不存在，THEN 返回 HTTP 404 ProblemDetails
5. IF 名称格式无效，THEN 返回 HTTP 400 ProblemDetails
6. WHEN 响应 `state=true` 且 `errno=""` 时，THE File_Service SHALL 只按顶层 `aid,cid,cname,errno,error,file_id,file_name,state` 返回新文件夹的 `fileId`/`fileName`

### 需求 10：文件移动

**用户故事：** 作为用户，我希望将文件移动到其他目录。

> **状态**：**移动 API 待补采集**。

#### 验收标准

1. THE WebApi_Layer SHALL 将 `POST /api/files/move` 标记为 disabled，并返回 HTTP 501 + `cloud_drive_api_not_collected`
2. THE File_Service SHALL NOT 调用上游移动接口；网页 UI 成功不构成 method/path/body/response 证据
3. THE File_Service SHALL NOT 声明批量参数或请求体形态
4. WHILE 能力 disabled，THE WebApi_Layer SHALL 在资源存在性探测前直接返回 501，避免借错误差异泄露资源状态
5. THE File_Service SHALL NOT 返回成功结果、创建 Background_Job 或改变缓存

### 需求 11：文件复制

**用户故事：** 作为用户，我希望将文件复制到其他目录。

> **状态**：**collected（单样本）**。只确认 `fid[0]` 单下标形态，不证明批量规则。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `POST /api/files/copy` 端点；当前一次请求只接受单个 fileId
2. THE File_Service SHALL 调用 `POST webapi.115.com/files/copy`，无 query，form body observed `fid[0],format,pid`
3. THE File_Service SHALL NOT 声明批量下标、重名控制或字段必填性；批量输入必须逐个顺序调用或返回不支持
4. IF 目标或源不存在，THEN 返回 HTTP 404 ProblemDetails
5. WHEN 响应 `state=true` 且 `errno=""` 时返回操作结果；只解析顶层 `errno,error,state`

### 需求 12：文件删除

**用户故事：** 作为用户，我希望删除文件（移至回收站）。

> **状态**：**删除 API 待补采集**。

#### 验收标准

1. THE WebApi_Layer SHALL 将 `DELETE /api/files` 标记为 disabled，并返回 HTTP 501 + `cloud_drive_api_not_collected`
2. THE File_Service SHALL NOT 调用上游删除接口；网页 UI 进入回收站不构成 HTTP 契约
3. THE File_Service SHALL NOT 声明批量删除请求体
4. WHILE 能力 disabled，THE WebApi_Layer SHALL 在资源存在性探测前直接返回 501，避免借错误差异泄露资源状态
5. THE File_Service SHALL NOT 返回成功结果、创建 Background_Job 或改变缓存

### 需求 13：文件重命名

**用户故事：** 作为用户，我希望重命名文件或文件夹。

> **状态**：**collected**。4 个真实成功样本。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `PATCH /api/files/{fileId}` 端点（请求体含 `newFileName`）
2. THE File_Service SHALL 调用 `POST webapi.115.com/files/batch_rename`，无 query，form body observed `files_new_name[{dynamic}],format`
3. THE File_Service SHALL 验证名称非空、不含非法字符（`/ \ : * ? " < > |`）、UTF-8 编码长度 ≤ **255 字节**（`CloudDriveFileNameValidator.ValidateRenameFileName`；与新建文件夹的「255 字符」规则不同）
4. IF 文件不存在，THEN 返回 HTTP 404 ProblemDetails
5. IF 名称格式无效，THEN 返回 HTTP 400 ProblemDetails
6. WHEN 响应 `state=true` 且 `errno=0` 时返回更新后文件信息；只解析顶层 `data,errno,error,state`

### 需求 13a：文件星标

**用户故事：** 作为用户，我希望给文件添加/取消星标。

> **状态**：**collected**。5 个真实成功样本；`star` 枚举未建立。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `POST /api/files/{fileId}/star` 端点（请求体含 `starred: boolean`）
2. THE File_Service SHALL 调用 `POST webapi.115.com/files/star`，无 query，form body observed `file_id,format,star`；系统 SHALL 只映射自身 boolean，不声明上游完整枚举
3. IF 文件不存在，THEN 返回 HTTP 404 ProblemDetails
4. WHEN 响应 `state=true` 时返回更新后星标状态；只解析顶层 `error,state`

### 需求 14：回收站管理

**用户故事：** 作为用户，我希望查看和管理回收站中的文件。

> **状态**：列表顶层与非空 `data[]` item 已采集；还原/彻底删除/清空只有 UI 场景或无成功证据，HTTP 能力保持 disabled。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET /api/recycle` 端点，支持 query 参数 `offset`/`limit`
2. WHEN 用户请求查看回收站时，THE File_Service SHALL 调用 `GET webapi.115.com/rb`，query 只传递 observed `asc,format,limit,o,offset`，采用 `offset + limit` 分页；不得补入未采集的 `aid/cid`
3. THE File_Service SHALL 归一化顶层 `count,data,error,is_asc,offset,order,page_size,rb_pass,state`
4. WHEN `data` 非空时，THE File_Service SHALL 只映射 item `cid,dtime,file_name,file_size,id,parent_name,status,type`，这些字段在样本中均为 string
5. THE File_Service SHALL 将 `itemSchemaStatus='available'`；但不得声明 `status/type` 枚举、pickCode 或字段必填性
6. THE File_Service SHALL 将响应的 `count` 字段从 string 转 number
7. THE WebApi_Layer SHALL 将 `POST /api/recycle/restore` 标记为 disabled，并返回 501
8. THE WebApi_Layer SHALL 将 `DELETE /api/recycle/{id}` 标记为 disabled，并返回 501；用户完成安全密钥验证的网页场景不能替代 HTTP 契约
9. THE WebApi_Layer SHALL 将 `DELETE /api/recycle` 标记为 disabled，并返回 501；不得创建虚假的 Background_Job

### 需求 15：图片文件预览

**用户故事：** 作为用户，我希望在线预览网盘中的图片。

> **说明**：当前不推断文件列表 `u` 字段的资源语义；图片预览统一复用已采集下载地址契约。

#### 验收标准

1. WHEN 用户请求预览图片时，THE Preview_Service SHALL 调用已采集的 `GET webapi.115.com/files/download` 获取临时地址或返回受控代理地址
2. THE Preview_Service SHALL NOT 直接使用文件列表 `u/tu/tp` 作为原图契约；这些字段的完整资源语义与有效期未通过本轮证据建立
3. THE Preview_Service SHALL 支持 JPG/JPEG/PNG/GIF/BMP/WEBP
4. THE Preview_Service SHALL 排除 SVG（脚本注入风险）
5. IF 文件类型不属于支持格式，THEN 返回 HTTP 400 ProblemDetails
6. IF 文件不存在，THEN 返回 HTTP 404 ProblemDetails

### 需求 16：视频播放

**用户故事：** 作为用户，我希望在线播放视频。

> **状态**：`GET webapi.115.com/files/video` 已取得成功与错误 schema；清晰度与音轨列表仍未采集。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET /api/videos/{pickCode}/play` 端点
2. THE Video_Service SHALL 调用 `GET webapi.115.com/files/video`，query 只传递 observed `format,local,pickcode,share_id`
3. THE Video_Service SHALL 按错误样本解析 `state,errno,error`，并按成功样本只映射 `download_url,dv_profile,file_id,file_name,file_size,file_status,hdr_type,inlay_power,is_dolby_vision,is_hdr,is_mark,parent_id,pick_code,sha1,state,video_push_state`
4. WHEN 成功响应可映射时，THE Video_Service SHALL 返回 `playbackSchemaStatus='available'`；播放/下载 URL、文件名、pickCode、SHA1 等上游敏感值不得记录日志或持久化
5. THE Video_Service SHALL 返回 `qualityOptions=[]` 与 `audioTracks=[]`，不得从 `download_url` 或 `dv_profile` 推断清晰度或音轨枚举
6. IF 已映射字段只能表明不可播放或上游处理未完成，THEN THE Video_Service SHALL 返回已知状态；不得自动提交转码，也不得创建无真实提交契约的 jobId
7. THE Video_Service SHALL NOT 构造 `restrictionReason` 或 `usageRestrictionNotice`；当前成功 schema 未提供这些字段
8. IF 文件不是视频类型，THEN 返回 HTTP 400 ProblemDetails
9. IF 文件不存在，THEN 返回 HTTP 404 ProblemDetails
10. IF 后续成功播放 schema 可用并返回播放地址，THEN THE Video_Service SHALL 在响应中标识播放地址的临时性
11. THE WebApi_Layer SHALL 将 `POST /api/videos/{pickCode}/transcode` 标记为 disabled，并返回 501；播放端点 SHALL NOT 内嵌自动转码
12. THE Video_Service SHALL NOT 调用 `files/video_push` 或 `movies/transcode`；当前只确认 `video_push` 的 method/path/query 已发送，JSON 结果和费用语义未采集

### 需求 17：视频字幕

**用户故事：** 作为用户，我希望获取视频字幕列表。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET /api/videos/{pickCode}/subtitles` 端点
2. THE Video_Service SHALL 调用 `GET webapi.115.com/movies/subtitle?pickcode=...&format=json`
3. THE Video_Service SHALL 仅按 `docs/115-api-docs.md` 已采集 schema 识别 `data.autoload` 与 `data.list` 两个数组；当前样本数组 item schema 未采集
4. IF `autoload` / `list` item schema 未采集，THEN THE Video_Service SHALL 返回 `CloudDriveSubtitleListDto { autoloadSubtitle: null, items: [], itemSchemaStatus: 'not_collected' }`，不得伪造 `sid/language/title/url/type/sha1/fileId/fileName/pickCode` 等字幕字段
5. IF 后续 `docs/115-api-docs.md` 补充字幕 item schema，THEN THE Video_Service SHALL 只映射文档中明确采集到的字段
6. IF 字幕列表为空或 item schema 未采集，THEN 返回 HTTP 200 + 空数组

### 需求 18：视频播放进度

**用户故事：** 作为用户，我希望视频播放进度能被记忆。

> **状态**：**读取 collected；写入 not-collected/disabled**。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET /api/videos/{pickCode}/history` 端点
2. THE WebApi_Layer SHALL 将 `POST /api/videos/{pickCode}/history` 标记为 disabled，并返回 501
3. THE Video_Service SHALL 调用 `GET webapi.115.com/files/history`，query 只传递 observed `fetch,format,pick_code`
4. THE Video_Service SHALL NOT 调用 `POST webapi.115.com/files/history`，不得猜测 `op/time/definition/watchEnd` 映射
5. WHEN 读取成功时，THE Video_Service SHALL 只映射 `data.add_time,category,file_name,hash,pick_code,thumb,time,watch_end`；`time/watch_end` 的单位与枚举保持 unknown，敏感值不得记录

### 需求 19：文档预览

**用户故事：** 作为用户，我希望在线预览文档。

> **说明**：115 网页端未提供专门的文档预览 API。PDF/TXT 通过下载地址由前端直接渲染；Office 格式需前端集成第三方渲染库或外部预览服务，必须显式配置后启用。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET /api/documents/{pickCode}/preview` 端点
2. THE Preview_Service SHALL 返回临时下载地址或受控代理地址
3. THE Preview_Service SHALL 支持并标注渲染方式：
   - **Direct**：PDF、TXT（前端直接渲染）
   - **ThirdParty**：DOC/DOCX/XLS/XLSX/PPT/PPTX（需第三方渲染库或外部服务）
   - **NotAvailable**：未支持的文档格式
4. THE Preview_Service SHALL 在响应中标注 `renderMode: 'Direct' | 'ThirdParty' | 'NotAvailable'`
5. IF 文件类型不属于支持格式，THEN 返回 HTTP 400 ProblemDetails
6. IF 文件不存在，THEN 返回 HTTP 404 ProblemDetails
7. IF Office 格式所需第三方服务未配置，THEN 返回 `renderMode: 'NotAvailable'` 和提示 "预览服务未配置，请下载后查看"

### 需求 20：云下载 — 读取

**用户故事：** 作为用户，我希望查看当前账号下的云下载任务、配额和统计。

> **状态**：配额、计数与空列表 envelope collected；非空 item 与所有写操作 HTTP 契约未采集。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET /api/offline/tasks` 端点，参数 `page`（默认 1）
2. THE Offline_Service SHALL 调用 `POST clouddownload.115.com/web/?ac=task_lists&page=...&page_size=15&stat=12`；当前只观察 `page=1,page_size=15,stat=12`，公开 `page` 可用于分页但不得把 `stat=12` 推断为枚举；该读取请求无 body
3. THE Offline_Service SHALL NOT 为配额、计数或列表读取生成 `sign,time,uid` body；签名只在已观察到的 URL 创建 form 中出现且算法未采集
4. THE Offline_Service SHALL 归一化任务列表顶层 `count,errcode,errtype,page,page_count,page_row,page_size,quota,real_state,state,tasks,total`
5. IF `tasks` 对象的 item schema 未采集，THEN THE Offline_Service SHALL 返回 `CloudDriveOfflineTaskListDto { items: [], itemSchemaStatus: 'not_collected' }`，不得伪造 `infoHash/name/size/addTime/lastUpdate/percentDone/url/fileId/parentId/status` 等任务字段
6. IF 后续证据补充非空 task item，THEN THE Offline_Service SHALL 只映射新 evidence 字段；在此之前不得推断状态值域
7. THE WebApi_Layer SHALL 提供 `GET /api/offline/quota`，内部调用 `POST clouddownload.115.com/web/?ac=get_quota_package_info&uid=...`（无 body），映射 `count,max_size,package,surplus,used`
8. THE WebApi_Layer SHALL 提供 `GET /api/offline/stats`，内部调用 `POST clouddownload.115.com/web/?ac=task_count`（无 body），映射 `total_count,failed_count,finished_count,downloading_count,errcode,errtype,state`
9. THE Cloud_Drive_Service SHALL 将签名 `sign` 参数从日志中脱敏

### 需求 21：云下载任务管理

**用户故事：** 作为用户，我希望创建、删除、清空云下载任务。

> **状态**：URL/BT 创建与单任务删除只有网页 UI 成功场景；HTTP 成功契约、BT 解析、删除、清空与签名算法均 **not-collected / disabled**。

#### 验收标准

1. THE WebApi_Layer SHALL 将 `POST /api/offline/tasks/urls` 标记为 disabled；已观察 `ac=add_task_urls` form `savepath,sign,time,uid,url[0],wp_path_id` 只有错误响应，不能启用
2. THE WebApi_Layer SHALL 将 `POST /api/offline/tasks/bt` 标记为 disabled；UI 创建成功不构成 HTTP 契约
3. THE WebApi_Layer SHALL 将 `POST /api/offline/parse-torrent` 标记为 disabled
4. THE WebApi_Layer SHALL 将 `DELETE /api/offline/tasks/{infoHash}` 标记为 disabled
5. THE WebApi_Layer SHALL 将 `POST /api/offline/tasks:clear` 标记为 disabled；页面存在非本轮任务且未执行清空，不得创建 Background_Job
6. WHILE 上述能力 disabled，THE Offline_Service SHALL 返回 501 且不得修改任务缓存

### 需求 22：分享管理

**用户故事：** 作为用户，我希望查看我的分享（我的/发送/接收）。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET /api/shares` 端点，参数 `tab: 'slist' | 'send' | 'receive'`（必填）/`offset`/`limit`
2. WHEN `tab=slist` 时，THE Share_Service SHALL 调用 `GET webapi.115.com/share/slist`，query 只传递 observed `asc,format,limit,offset,order,show_cancel_share`
3. WHEN `tab=send` 时，THE WebApi_Layer SHALL 返回 501 + `cloud_drive_api_not_collected`，不得发起上游请求
4. WHEN `tab=receive` 时，THE WebApi_Layer SHALL 返回 501 + `cloud_drive_api_not_collected`，不得发起上游请求
5. THE Share_Service SHALL 归一化 `/share/slist` 顶层 `bind_receive_code,count,define_receive_code,errno,error,list,state`
6. WHEN `list` 非空时，THE Share_Service SHALL 只映射本文档列出的 33 个 item 字段及样本类型，将 `itemSchemaStatus='available'`；分享码、访问码、URL、文件名和账号标识不得记录
7. THE Share_Service SHALL NOT 声明 item 状态枚举、必填性或未采集字段语义；send/receive 的错误字段 `errNo,errno,error,request,state` 只用于 501 前的历史证据说明，不构成成功列表能力
8. WHEN `tab` 值不支持时，返回 HTTP 400 ProblemDetails
9. **注**：`slist` 已取得真实非空成功 item；send/receive 只有错误样本，前端对应 Tab 必须禁用

### 需求 23：用户中心聚合

**用户故事：** 作为用户，我希望查看已授权的 115 账号信息（基础信息/VIP/余额/存储空间）。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET /api/user/profile` 聚合端点，返回 `CloudDriveUserProfileDto`
2. THE User_Service SHALL 并行调用并聚合：
   - `GET webapi.115.com/user/vip_limit?feature=2`（VIP 状态）
   - `GET vip.115.com/?ct=order&ac=get_yun_card`（VIP 卡详情）
   - `GET pay.115.com/?ct=balance&ac=getBalance`（账户余额）
   - `GET webapi.115.com/files/index_info?count_space_nums=1`（存储空间）
3. THE User_Service SHALL 将 `info=null`，并将 `GET /api/user/info` 标记为 disabled；基础资料成功 schema 未采集，不得使用任何旧基础资料字段
4. THE User_Service SHALL 严禁从任何响应或日志构造昵称、头像、UID、手机号、邮箱或其他基础资料；不得将上游 `data` 原样透传
5. THE User_Service SHALL 按 `get_yun_card` 的 `data.00_balance,forever_count,num,space_count,sqcode_cancel_count,sqcode_count,url,vip_count` 映射卡片子模块；`00_balance` 业务语义/单位 unknown，不得与 pay 余额混用
6. THE User_Service SHALL 按 `files/index_info` 的 `space_info.all_remain/all_total/all_use.{size,size_format}` 与动态 `type_nums/type_summury` 映射存储空间，并保留原始拼写 `type_summury/pecent`；将 `schemaStatus='available'`
7. IF 任一子接口失败，THEN 对应子模块设为 `null`，其他正常返回（错误隔离）
8. THE WebApi_Layer SHALL 提供 `GET /api/user/vip`、`GET /api/user/balance`、`GET /api/user/storage`；`GET /api/user/info` 返回 501
9. IF Cookie 无效，THEN 返回 HTTP 401 ProblemDetails

### 需求 24：API 频控与限流

**用户故事：** 作为系统使用者，我希望控制对 115 API 的调用频率避免触发风控。

#### 验收标准

1. THE Rate_Limiter SHALL 对所有发往 115 API 的请求进行频率控制
2. THE Rate_Limiter SHALL 按场景分类设置限流规则（文件浏览/媒体扫描/后台批量）
3. WHEN 请求频率超限时，THE Rate_Limiter SHALL 排入队列或返回 HTTP 429 ProblemDetails
4. IF 115 API 返回频控响应（HTTP 429 或 errNo 匹配频控语义），THEN THE Cloud_Drive_Service SHALL 执行受限次数退避重试
5. THE Cloud_Drive_Service 与 Rate_Limiter SHALL 记录请求排队、拒绝、限流信号、退避重试的可观测日志
6. THE Rate_Limiter SHALL 与 Metadata_Cache 协同，通过缓存降低重复调用

### 需求 25：统一错误响应（ProblemDetails）

**用户故事：** 作为前端工程师，我希望后端统一使用 ProblemDetails 返回错误。

> **重要**：115 网页端各接口 envelope 字段不统一。下表只记录已验收 evidence 中实际出现的字段；“仅成功样本”不等于错误契约已建立。字段解析由 Gateway/ApiClient 路径中的 `ErrorFieldAdapter` 按 host + path + 必要操作 query 逐项适配；`ExceptionHandlingMiddleware` 只负责映射已分类异常。
>
> **业务错误码（前端词汇）**：ProblemDetails 的 `extensions.code` 使用稳定应用业务码；`extensions.upstreamCode` 为可选 115 上游码。**完整闭集**见 `design.md`「应用业务错误码表」（不含「其他」占位；业务失败路径必须填入表中某一码）。当冷却期短路业务 API（禁止继续调用 115）时返回 HTTP 401 + `auth_expired`。

#### 验收标准

1. THE WebApi_Layer SHALL 使用 `application/problem+json` 返回所有错误响应
2. THE ErrorFieldAdapter SHALL 在 Gateway/ApiClient 解析路径中按 **URL 路径逐项匹配**以下网页端错误字段变体：

   | # | 端点 | state 类型 | 错误码字段（类型） | 错误消息字段 |
   |---|------|----------|------------------|------------|
   | 1 | `GET webapi.115.com/files` | boolean | `errNo`（number，仅成功值 0） | `error` |
   | 2 | `POST webapi.115.com/files/add` | boolean | `errno`（string，仅成功值空串） | `error` |
   | 3 | `POST webapi.115.com/files/batch_rename` | boolean | `errno`（number，仅成功值 0） | `error` |
   | 4 | `POST webapi.115.com/files/copy` | boolean | `errno`（string，仅成功值空串） | `error` |
   | 5 | `POST webapi.115.com/files/star` | boolean | *（无）* | `error` |
   | 6 | `GET webapi.115.com/files/download` | boolean | `msg_code`（number；错误枚举未建立） | `msg` |
   | 7 | `GET webapi.115.com/files/video` | boolean | `errno`（number；已采错误值 406） | `error` |
   | 8 | `GET webapi.115.com/movies/subtitle` | boolean | `errNo`（number） | `error` |
   | 9 | `GET webapi.115.com/rb` | boolean | *（无）* | `error` |
   | 10 | `GET webapi.115.com/share/slist` | boolean | `errno`（number） | `error` |
   | 11 | `GET webapi.115.com/share/send`、`/share/receive` | boolean | `errNo` + `errno`（number；错误样本同时存在） | `error` |
   | 12 | `GET webapi.115.com/user/vip_limit` | boolean | `code`（string） | `message` |
   | 13 | `GET webapi.115.com/files/index_info` | boolean | `code`（number） | `error` |
   | 14 | `GET webapi.115.com/user/setting` | boolean | `errno`（number） | `error` |
   | 15 | `GET pay.115.com/?ct=balance&ac=getBalance` | boolean | `code`（number） | `msg` |
   | 16 | `GET vip.115.com/?ct=order&ac=get_yun_card` | boolean | *（无）* | *（无）* |
   | 17 | `GET vip.115.com/?ct=product&ac=product_list` | boolean | `code`（number） | `msg` |
   | 18 | `GET vip.115.com/?ct=act_coupon&ac=get_coupon_yhq_list` | boolean | `code`（number） | *（未采集）* |
   | 19 | `POST clouddownload.115.com/web/?ac=...` | boolean | `errcode`（number） | `errtype` / `error_msg` |
   | 20 | `GET webapi.115.com/files/history` | boolean | *（无数字错误码）* | `error` |

   > **指针**：实现时在 Gateway/ApiClient 中注册上表 **20 行**已观察 envelope 规则；没有错误样本的行只能作为成功解析，未知失败形态必须 fail closed 为 `api_error` 并记录脱敏 Warning。匹配器契约（与 design/tasks 2.2 同句）：
   > 1. **webapi 路径精确键**（无 `*`）：对 `webapi.115.com` 上以 `/` 开头的 path 键（如 `/files`、`/files/video`）做 path **字节级精确相等**；`/files` 与 `/files/video` / `/files/history` **不得**互相吞并。
   > 2. **同 path 操作 query**：`vip.115.com/` 与 `clouddownload.115.com/web/` 必须把 `ct/ac` 的 observed 控制值纳入规则键，不能只按 path 合并不同操作。
   > 3. **同主机最长且最具体匹配优先**：同一请求命中多行时先取 path 最长，再取操作 query 最具体；仍并列则 fail closed 记 Warning。
   > 未列接口不得注册猜测规则。`ExceptionHandlingMiddleware` 不承担上游 envelope 解析职责。

3. THE ErrorFieldAdapter SHALL 按 method + host + 精确 path + 必要操作 query 匹配对应字段规则，并将解析结果映射为内部 `CloudDrive*Exception` / 业务错误
4. THE ExceptionHandlingMiddleware SHALL 将已分类异常映射为 HTTP 状态码与 ProblemDetails（`extensions.code` 必须取自 design「应用业务错误码表」闭集）：
   - Cookie 冷却短路 / `auth_expired` → 401 Unauthorized
   - Cookie 失效 `cookie_invalid` / `CloudDriveCookieInvalidatedException`（探测失效）→ 401 Unauthorized
   - Cookie 非 Active `cookie_not_active` → 401 Unauthorized
   - 频控 / `rate_limited` → 429 Too Many Requests
   - 文件不存在 / `file_not_found` → 404 Not Found
   - 分享过期 / `share_expired` → 410 Gone
   - 网络异常 / `network_error` → 502 Bad Gateway
   - 通用业务 API 失败 / `api_error` → 502 Bad Gateway（非 `unknown_error_shape`）
   - 名称非法 / `invalid_name` → 400 Bad Request
   - 上传失败 / `upload_failed` → 500 Internal Server Error
   - 配置缺失 / `configuration_error` → 503 Service Unavailable
   - 签名不可用 / `offline_signature_not_available` → 503 Service Unavailable
   - 未采集占位 / `cloud_drive_api_not_collected` → 501 Not Implemented
   - CSRF / `csrf_failed` → 403 Forbidden
5. THE ProblemDetails SHALL 包含标准字段 `type`/`title`/`status`/`detail`/`instance`，以及自定义 `extensions.code`（稳定应用业务码，闭集见 design「应用业务错误码表」）、可选 `extensions.upstreamCode`（115 errNo/code）与 `extensions.upstreamMessage`（115 error/message）
6. WHEN Application 返回 `Result.Failure(...)` 时，THE WebApi_Layer SHALL 映射为非 2xx ProblemDetails，SHALL NOT 以 HTTP 200 表示失败
7. THE ExceptionHandlingMiddleware SHALL 对所有异常记录结构化日志

### 需求 26：日志与敏感字段脱敏

**用户故事：** 作为合规工程师，我希望所有日志对敏感字段脱敏。

#### 验收标准

1. THE Cloud_Drive_Service SHALL **严禁**日志写入以下字段原始值（黑名单与需求 23 同步）：
   - **认证密钥**：Cookie 内容（`UID`/`CID`/`USSID`）、签名 `sign` 参数、`time` 大值、用户 `uid` 全值
   - **联系方式**：`pub_mobile`、`pub_email`
   - **支付账号**：`alipay`、`wechat`、`weibo`
   - **住址与位置**：`address`、`homepage`、`location`、`location_show`、`location_birth`、`location_link`
   - **生日与风水**：`birthday`、`star`、`calendar_type`
   - **人口信息**：`gender`、`blood_type`、`is_marry`、`height`、`weight`
   - **职业与收入**：`education`、`job`、`salary`
   - **兴趣爱好**：`like_celeb`、`like_music`、`like_animal`、`like_book`、`like_video`、`interest`
2. THE LoggingBehavior SHALL 使用字段黑名单过滤结构化日志敏感字段，黑名单 SHALL 与需求 23 一致
3. THE Cookie_Manager SHALL 在日志中只记录固定占位符与会话状态，严禁记录 Cookie 的任何原值片段
4. THE Signature_Service SHALL 不记录完整 UID 和签名结果
5. THE Gateway_Layer SHALL 记录 API 调用概要：`endpoint`/`method`/`httpStatus`/`upstreamCode`/`latencyMs`（**不记录**原始请求/响应体）
6. THE LoggingBehavior SHALL 对 5xx 错误记录 `Error` 级别，对 4xx 错误记录 `Warning` 级别

### 需求 27：元数据缓存与目录刷新

**用户故事：** 作为用户，我希望浏览响应稳定快速，降低频控风险。

#### 验收标准

1. THE Metadata_Cache SHALL 按用户 + 115 账号 + 目录维度对文件列表元数据使用短 TTL 缓存，默认 TTL 不超过 30 秒，支持配置调低或关闭
2. WHILE 用户基础资料契约未采集，THE Metadata_Cache SHALL NOT 创建 `user:info` 缓存项；聚合 DTO 的 `info` 保持 null
3. THE Metadata_Cache SHALL 缓存 VIP 状态 5 分钟（`user:vip`）
4. THE Metadata_Cache SHALL 缓存存储空间 2 分钟（`user:storage`）
5. THE Metadata_Cache SHALL NOT 对文件列表使用长 TTL 或跨写操作复用旧缓存；手动刷新与写操作成功后必须绕过或失效目录列表短 TTL 缓存
6. WHEN 已启用写操作（新建目录、单项复制、重命名、星标）得到已采集成功 envelope 后，THE File_Service SHALL 使受影响目录缓存失效（通过领域事件 `DirectoryContentChanged`）；disabled 写操作不得进入该流程
7. WHEN 用户主动刷新目录时，THE File_Service SHALL 绕过或重建缓存
8. THE Metadata_Cache SHALL 只缓存 115 实际返回的数据，不得伪造字段
9. THE CloudDrive_Module SHALL 通过 Application 层抽象 `ICloudDriveMetadataCache`（`Application/Abstractions/`）封装缓存访问，Infrastructure 层基于 `IDistributedCache` 实现

### 需求 28：长耗时任务管理

**用户故事：** 作为用户，我希望后台任务状态只表达已启用且真实执行的操作，并能在重启后保持可信。

#### 验收标准

1. THE Cloud_Drive_Service SHALL NOT 为上传、主动转码、移动、删除、回收站写操作或云下载写操作创建 Background_Job；这些能力未采集，入口直接返回 501。只有未来获得完整脱敏 HTTP 契约并完成对应未勾选任务后才能启用后台任务
2. THE Background_Job SHALL 记录任务类型、关联资源标识、状态、进度、起止时间、错误信息；记录存在不代表对应 capability 已启用
3. THE Cloud_Drive_Service SHALL 提供 `GET /api/jobs/{jobId}` 查询已有任务状态，并校验用户与账号归属
4. WHEN 未来已启用操作被受理为 Background_Job 时，THE WebApi_Layer SHALL 返回 HTTP 202 Accepted 并携带 `jobId`；Accepted SHALL NOT 被映射为 Completed
5. IF capability matrix 未启用任务类型，THEN 任务编排器与恢复服务 SHALL 拒绝创建/恢复，并以固定脱敏原因标记失败
6. THE Cloud_Drive_Service SHALL 为 Background_Job 保留不含 Cookie、签名、账号标识、文件名、pickCode、临时 URL 或金额值的结构化日志
7. WHEN 且仅当已启用操作取得与已采集成功 envelope 匹配的上游确认时，THE Background_Job SHALL 标记 Completed；占位、未知、仅提交或仅受理不得标记完成

### 需求 29：业务模块组织结构

**用户故事：** 作为开发者，我希望 115 网盘业务按 DDD 分层组织在 net10 框架内。

#### 验收标准

1. THE CloudDrive_Module SHALL 位于 `src/Application/CloudDrive/`，按子领域（Auth/Files/Search/Video/RecycleBin/Offline/Share/User）组织 Commands/Queries/Handlers/DTO/Validators
2. THE Infrastructure_Layer SHALL 在 `src/Infrastructure/CloudDrive/` 实现物理单一 Gateway（`ICloudDriveWebApiGateway` / `CloudDriveApiClient`；需求中的 `FileGateway`/`VideoGateway`/`RecycleBinGateway`/`OfflineGateway`/`ShareGateway`/`UserGateway` 均为该网关的逻辑别名/能力分区，不要求独立物理类型）及 `CookieManager`（`CookieSessionManager` 逻辑别名）
3. THE WebApi_Layer SHALL 在 `src/WebApi/Controllers/` 提供 REST 控制器：`AuthController`/`FilesController`/`VideosController`/`DocumentsController`/`RecycleBinController`/`OfflineController`/`SharesController`/`UserController`/`JobsController`
4. THE CloudDrive_Module SHALL 通过 Infrastructure 层 Gateway 抽象接口访问 115 API，**严禁** Application 层直接依赖 `IHttpClientFactory` 或 `HttpClient`
5. THE Controllers SHALL 仅通过 `ISender`/`IMediator.Send()` 调用 Application 层，**严禁**直接注入 Gateway 或仓储
6. THE CloudDrive_Module SHALL 遵循 net10 框架需求 1-10 定义的 CQRS 命名、分层边界、架构约束（`CD0001`-`CD0009`）

### 需求 30：前端 API 契约

**用户故事：** 作为前端工程师，我希望后端提供稳定的 REST 端点，以便前端直接消费。

#### 验收标准

1. THE WebApi_Layer SHALL 对外暴露 RESTful `/api/*` 端点，前端通过同源请求访问（不直连 115）
2. THE WebApi_Layer SHALL 返回的成功响应 SHALL 使用统一封装或直接返回 DTO（取决于实现层决定，但需保持同一业务域一致）
3. THE WebApi_Layer SHALL 返回的错误响应 SHALL 遵循需求 25 的 ProblemDetails 格式
4. THE WebApi_Layer SHALL 在 OpenAPI 文档（`/openapi/v1.json`，基于 `Microsoft.AspNetCore.OpenApi`）暴露所有端点的请求/响应契约
5. THE Cloud_Drive_Service SHALL 支持 CORS 配置，允许前端域名访问
6. THE WebApi_Layer SHALL NOT 将 115 的短字段名（`n`/`s`/`fc`/`pc` 等）暴露至前端响应；所有响应字段 SHALL 使用归一化 DTO 的长字段名
7. THE WebApi_Layer SHALL NOT 将 Cookie、签名 `sign`、`UID` 等敏感字段暴露至前端响应
8. THE WebApi_Layer SHALL NOT 在普通业务端点中要求前端重复传递 `CloudAccountUid`；后端根据 JWT UserSession 的 `ActiveCloudAccountUid` 解析上下文。`CloudAccountUid` 可出现在账号绑定/选择/解绑端点、内部实体、缓存键、限流键和审计记录中，但不得携带 Cookie 原文。

### 需求 31：CSRF 与浏览器 Cookie 安全属性

**用户故事：** 作为安全工程师，我希望 WebApi 对状态变更请求提供 CSRF 防护，并对浏览器可见 Cookie 设置安全属性。

#### 验收标准

1. THE WebApi_Layer SHALL 对 `POST`/`PUT`/`PATCH`/`DELETE` 等状态变更请求执行 CSRF 校验（ASP.NET Core Antiforgery 或等价方案），并在 `/openapi/v1.json` 暴露凭据契约（header 或 cookie 名）
2. IF CSRF 校验失败，THEN THE WebApi_Layer SHALL 返回 HTTP 403 ProblemDetails，且 `extensions.code` 为稳定业务码（如 `csrf_failed`）
3. WHEN WebApi 向浏览器签发任何会话/CSRF 相关 Cookie 时，THE WebApi_Layer SHALL 设置 `HttpOnly`、`Secure`（非本地开发）与显式 `SameSite` 策略
4. THE WebApi_Layer SHALL NOT 将 115 会话三元组（UID/CID/USSID）以浏览器 Cookie 形式下发；115 Cookie 仅服务端加密持久化并注入上游请求
5. THE WebApi_Layer SHALL 确保前端可按 OpenAPI 契约自动携带 CSRF 凭据，无需自造 token

## 非功能性需求

### 性能与响应

1. 文件列表查询首屏 P95 响应时间 ≤ 1.5 秒（缓存命中场景 ≤ 300ms）
2. 视频播放端点响应 P95 ≤ 2 秒；指标覆盖已采集基础字段映射与错误返回，不包含主动转码
3. 用户聚合接口 P95 ≤ 2 秒（并行子调用）
4. 单个已启用上游 API 调用默认超时 10 秒；disabled 能力不发起调用
5. Polly 策略只对已启用幂等读取应用：瞬时错误最多重试 3 次 + 指数退避 (1s/2s/4s)，超过阈值熔断 30 秒；写操作不自动重放

### 安全

1. **Cookie 加密存储**：使用 ASP.NET Core Data Protection，`IDataProtector` purpose: `CloudDrive.Cookie.v1`
2. **Cookie 热加载**：通过 `IOptionsMonitor<CookieOptions>` 监听配置变更
3. **敏感字段归一化层过滤**：用户信息聚合端点严禁透传原始 `pub_mobile`/`pub_email` 等字段
4. **日志脱敏**：Cookie、签名、账号标识、文件名、pickCode、临时 URL 与金额均不得记录原值或原值片段
5. **CORS 白名单**：仅允许显式配置的前端域名
6. **速率限制**：基于 ASP.NET Core `RateLimiter` 中间件按场景分类限流
7. **CSRF 防护**：状态变更请求须执行 Antiforgery 校验并在 OpenAPI 暴露凭据契约；详见需求 31

### 可观测性

1. 结构化日志（Serilog）：记录 request_id/endpoint/method/status/upstream_code/latency_ms
2. OpenTelemetry traces：覆盖 HTTP 入站请求 → Application Handler → Gateway → 115 API 全链路
3. Metrics：`cloud_drive.requests.total`、`cloud_drive.requests.latency_ms`、`cloud_drive.errors.total{category}`
4. 健康检查：`/health/live`（应用存活）、`/health/ready`（Data Protection key ring 可用 + 数据库可达）；用户 CookieSession 只在请求上下文中检查

### 部署与系统

1. Docker 镜像化部署；支持 Kubernetes 多副本横向扩展
2. Data Protection key ring 持久化到共享存储（Redis/文件系统）以支持多实例
3. `appsettings.json` + 环境变量覆盖机制；生产环境 Data Protection key ring 通过 Secret 或共享存储配置，用户 Cookie 不从全局配置注入
4. Quartz.NET `JobStore` 使用 SqlSugar 持久化，支持任务跨实例协调
5. 数据库迁移通过 SqlSugar CodeFirst 或独立脚本执行

### 测试覆盖

1. Domain 层单元测试覆盖率 ≥ 90%
2. Application 层 Handler 单元测试覆盖所有 Success / ValidationFailure / UpstreamError 路径
3. Gateway 层使用 `HttpMessageHandler` mock 测试 115 API 调用与归一化
4. WebApi 层集成测试覆盖 ProblemDetails、CORS、认证失败、频控 6 个关键场景
5. 契约测试：OpenAPI schema 与前端 TypeScript 类型定义对齐
