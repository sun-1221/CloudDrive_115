# 需求文档：115 网盘集成

## 简介

本功能为 .NET 10 原生 DDD + CQRS 项目集成 115 网盘 **网页端原生 API**，通过后端 Cookie 认证机制调用 `webapi.115.com`、`115.com/web/lixian`、`my.115.com`、`vip.115.com`、`pay.115.com` 等网页端 API，为前端提供标准化的 REST 端点。涵盖文件 CRUD（读取优先）、搜索、回收站、图片/视频/文档预览、云下载（离线下载）、分享管理、用户中心等能力。

> **架构定位**：本 spec 定义 115 网盘**后端业务模块**，基于 `.kiro/specs/115-cloud-drive-net10/` 已搭建的 .NET 10 DDD 四层框架实现。前端 Vue 3 应用通过本模块提供的 `/api/*` REST 端点访问 115 网盘数据。

> **数据源权威性**：本系统仅对接 **115 网页端原生 API**，以 `docs/115-api-docs.md`（2026-05-04 最新自动采集，`runId: 20260504073432-ck05g2`，并保留 2026-04-18 上传链路手动补充，共 76 个唯一接口）为权威数据源。开放平台文档（`/open/*`）仅作为参数含义、业务约束、特殊行为的补充参考。**不**通过 OAuth 换取 Access/Refresh Token 直接调用开放平台 API。

> **认证方式**：采用 **Cookie-based 认证**（UID/CID/USSID），由管理员从已登录的 115 网页端导出 Cookie 注入系统配置；后端通过 ASP.NET Core Data Protection 加密存储，请求 115 API 时自动注入。**不使用 OAuth 2.0、AppID/AppSecret、PKCE 流程**。

> **合规声明**：系统使用的是用户在 115 网页端登录后的 Cookie 会话，归属其本人。系统不得将 API 用于图床、软件床、视频外链播放、OSS 对象存储等违反 115 服务条款的场景。

> **账号模型**：**单账号模型**。系统仅维护一个 115 账号的 Cookie 会话，不支持多账号切换。

> **API 覆盖说明**：已通过 `docs/115-api-docs.md` 获取 76 个网页端接口证据（74 个 2026-05-04 自动采集 + 2 个上传链路手动补充），本系统使用其中 **20 个业务接口**（含 2026-04-18 补充采集的上传初始化 `uplb.115.com/3.0/sampleinitupload.php`、OSS 直传，以及 2026-05-04 自动采集确认的视频转码 `webapi.115.com/files/video_push`、播放进度 `webapi.115.com/files/history`）。对文档中已有采集证据的接口，后端 SHALL 直接按该文档访问 115；不得改写成开放平台 OAuth 口径或继续标注为待联调。**部分写操作接口**（文件增删改、下载地址、云下载创建/删除/清空、回收站还原/彻底删除/清空等）**尚未在该文档中采集**，仍标注为「待补采集」。上传链路涉及外部 OSS 域名 `*.oss-cn-*.aliyuncs.com`（阿里云对象存储直传）。

> **官方 API 范围排除声明**：以下 13 个官方网页端端点**明确不纳入本系统**，避免引入不必要的数据收集或侵入性调用：
> - `webapi.115.com/category/shortcut`（快捷分类，非核心功能）
> - `webapi.115.com/label/list`（文件标签列表，后续迭代）
> - `webapi.115.com/user/allow_protocol`、`/user/fingerprint`、`/user/setting`（用户协议、设备指纹、用户设置，属浏览器探针）
> - `webapi.115.com/usershare/list`（与 `/share/slist` 功能重叠）
> - `115.com/?ac=setting&even=saveedit`（保存编辑设置，无业务需求）
> - `act.115.com/*`（活动开关，非网盘功能）
> - `appversion.115.com/*`（客户端版本，非网盘功能）
> - `msg.115.com/*`、`yunmsg.115.com/*`、`pmsg.115.com/*`、`home.115.com/*`（消息/通讯录，非网盘功能）
> - `q.115.com/*`（社区功能，非网盘）
> - `passportapi.115.com/*`（账号安全预警，非网盘）

> **分页上限说明**：`docs/115-api-docs.md` 当前仅采集到网页端 `offset` / `limit` 参数，未采集到 `limit=1150`、`offset+limit≤10000` 等网页端硬上限。系统不得将开放平台推断值作为 115 网页端硬拦截规则；如需保护服务资源，只能通过系统自有配置（如 `CloudDrive:MaxPageSize`）限制，并在 ProblemDetails 中标注为系统保护限制而非 115 官方限制。

## 前置要求与约束

1. 本系统 SHALL 仅使用 115 网页端已公开接口，严格遵守 115 用户协议
2. 本系统 SHALL 以部署方持有有效的 115 网页端 Cookie（UID/CID/USSID）为前提，通过环境变量或安全配置文件注入
3. IF Cookie 缺失、格式无效或失效，THEN 系统 SHALL 禁用 115 相关功能并记录错误
4. 所有下载/播放/缩略图地址 SHALL 仅供当前授权用户在本系统内短时使用，不得持久化为公开链接
5. 对于高频浏览、媒体扫描、批量操作场景，系统 SHALL 结合限流、缓存和后台任务策略，降低频控与风控风险

## 术语表

- **Cloud_Drive_Service**：115 网盘集成服务的核心服务层
- **Auth_Service**：Cookie 认证服务（取代 OAuth Service）
- **File_Service**：文件管理服务
- **Preview_Service**：文件预览服务
- **Video_Service**：视频管理服务
- **Offline_Service**：云下载（离线下载）服务
- **Share_Service**：分享管理服务
- **User_Service**：用户中心服务（聚合基础信息/VIP/余额/存储空间）
- **Cookie_Manager**：基于 Data Protection 加密存储 UID/CID/USSID
- **Signature_Service**：为云下载接口生成 `sign + time + uid` 签名
- **Rate_Limiter**：按场景控制 115 API 调用频率
- **Metadata_Cache**：元数据缓存
- **Background_Job**：基于 Quartz.NET `IJob` 的后台任务
- **Pick_Code**：文件提取码，115 唯一标识符
- **Normalized_Dto**：归一化 DTO，屏蔽网页端短字段名
- **Problem_Details**：`application/problem+json` 统一错误响应

## 需求

### 需求 1：Cookie 会话认证

**用户故事：** 作为系统管理员，我希望通过注入一组 115 网页端 Cookie 建立系统与 115 的会话。

> **说明**：不同于 OAuth 需要用户主动授权，本系统采用**管理员注入 Cookie** 模式。系统不涉及用户密码、短信、二维码等登录流程。

#### 验收标准

1. THE Cookie_Manager SHALL 支持通过环境变量（`COOKIE_UID`/`COOKIE_CID`/`COOKIE_USSID`）或配置文件读取明文 Cookie
2. THE Cookie_Manager SHALL 通过 ASP.NET Core Data Protection（`IDataProtector`）加密 Cookie 并持久化，key ring 持久化共享以支持服务重启和多实例部署
3. WHEN Gateway 发起 115 API 请求时，THE Cookie_Manager SHALL 在 Header 注入 `Cookie: UID=...; CID=...; USSID=...`
4. IF 115 API 响应指示登录态失效（HTTP 401 或 errNo/code 匹配登录失效语义），THEN THE Cookie_Manager SHALL 发布领域事件 `CookieInvalidated`
5. WHEN `CookieInvalidated` 事件发生后，THE Cloud_Drive_Service SHALL 在冷却时间内（默认 60 秒，可通过 `CookieCooldownSeconds` 配置调整）短路后续 115 API 调用，直接返回 `auth_expired` 错误
6. THE Cookie_Manager SHALL 严禁通过日志、响应体、异常消息泄露原始 Cookie 值；日志仅记录前 4 位 + `****`
7. THE Cloud_Drive_Service SHALL 采用单账号模型，不支持多账号切换

### 需求 2：Cookie 配置管理

**用户故事：** 作为系统管理员，我希望通过配置管理 Cookie 的注入、更新与失效处理。

#### 验收标准

1. THE Cloud_Drive_Service SHALL 在启动时验证 Cookie 配置完整性（UID/CID/USSID 三项非空）
2. IF Cookie 配置缺失，THEN THE Cloud_Drive_Service SHALL 记录 `LogLevel.Error` 日志并将 `CloudDriveFeatureEnabled` 标志设为 `false`，不阻止应用启动
3. WHEN 配置更新时，THE Cookie_Manager SHALL 支持热加载新 Cookie 无需重启
4. THE Cookie_Manager SHALL 严禁以明文形式将 Cookie 出现在代码仓库、日志或响应中
5. THE Cloud_Drive_Service SHALL 提供 `GET /api/auth/check` 端点让前端查询 Cookie 是否有效

### 需求 3：登录态检测 API

**用户故事：** 作为前端工程师，我希望后端提供统一的登录态校验端点。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET /api/auth/check`，返回 `{ isAuthenticated: boolean, failureReason?: string }`
2. WHEN 收到 `/api/auth/check` 请求时，THE Cloud_Drive_Service SHALL 通过 `FileGateway` 调用 `webapi.115.com/files?cid=0&limit=1` 轻量探测
3. IF 探测成功，THEN 返回 `{ isAuthenticated: true }`
4. IF 探测失败且登录态无效，THEN 返回 `{ isAuthenticated: false, failureReason: 'cookie_invalid' }`
5. IF 探测失败且原因为网络异常，THEN 返回 `{ isAuthenticated: false, failureReason: 'network' }`
6. THE WebApi_Layer SHALL 对此端点不强制要求认证

### 需求 4：获取文件列表

**用户故事：** 作为用户，我希望浏览 115 网盘中的文件和文件夹。

#### 验收标准

1. WHEN 用户请求获取指定目录的文件列表时，THE File_Service SHALL 调用 `GET webapi.115.com/files`，必填参数 `aid=1`（网盘档案 ID；回收站为 `aid=7`）、`cid=0` 表示根目录、`format=json`
2. THE File_Service SHALL 采用 `offset + limit` 分页，默认 `offset=0`、`limit=50`；`docs/115-api-docs.md` 未采集网页端最大 `limit`，实现不得以开放平台推断值作为 115 硬上限
3. THE File_Service SHALL 支持透传筛选与排序参数：`o`（`user_ptime`/`file_name`/`file_size`）、`asc`（1/0）、`type`（网页端已采集参数存在但值域未采集）、`show_dir`、`star=1`（仅星标）、`natsort`、`snap`、`count_folders`、`record_open_time`、`code`（待采集含义）、`scid`（待采集含义）、`source`、`suffix`；不得将未采集的 `type=1..7` 语义当作 115 官方枚举
4. THE File_Service SHALL 归一化网页端响应字段为 `CloudDriveFileDto`：`fid→fileId, n→fileName, s→size, fc→rawCategoryCode, pc→pickCode, tu→thumb, u→originalUrl, tp→videoImg, iv→isVideo, fvs→videoDefinition, play_long→playDuration, te→uploadTime, t→updateTime, m→isStarred, hdf→isPrivate, sta→uploadStatus, sha→sha1, ico→ico, class→fileClass`；`fc/sta/hdf/class/fvs/iv` 等字段值域未在 `docs/115-api-docs.md` 中定义，派生 `category/isDirectory/uploadStatus` 前 SHALL 通过真实网页端样本验证，未知时返回 `category='unknown'` / `isDirectory=null`，不得硬编码互相冲突的开放平台推断值
5. THE File_Service SHALL 归一化响应中的 `path` 数组为 `CloudDriveBreadcrumbDto[]`
6. WHEN 未指定目录时，THE File_Service SHALL 默认返回根目录（`cid=0`）
7. IF 目录不存在，THEN 返回 HTTP 404 ProblemDetails
8. WHEN `offset < 0`、`limit <= 0` 或 `limit` 超出系统自有 `CloudDrive:MaxPageSize` 配置时，THE File_Service SHALL 抛出 `ValidationException`，返回 HTTP 400 ProblemDetails；错误码 SHALL 表示系统保护限制，不得表述为 115 网页端官方上限

### 需求 5：文件搜索

**用户故事：** 作为用户，我希望在网盘中搜索文件。

#### 验收标准

1. WHEN 用户提交搜索关键词时，THE File_Service SHALL 调用 `GET webapi.115.com/files/search`
2. THE File_Service SHALL 支持筛选参数：`search_value`（必填）、`aid`（默认 1）、`cid`（默认 0，指定搜索目录）、`type`（网页端为 empty/数值）、`date`（`YYYY-MM-DD`，日期筛选）、`pick_code`（可选，按提取码查询）、`count_folders`（默认 1）、`source`、`format=json`；**开放平台的 `gte_day`/`lte_day`/`fc`/`suffix`/`file_label` 参数在网页端采集中未出现**，本系统暂不采用
3. THE File_Service SHALL 采用 `offset + limit` 分页；`docs/115-api-docs.md` 未采集网页端 `offset + limit` 总上限，系统不得以 `10000` 作为 115 硬限制
4. WHEN 关键词为空或纯空白时，返回 HTTP 400 ProblemDetails
5. WHEN `offset < 0`、`limit <= 0` 或 `limit` 超出系统自有 `CloudDrive:MaxPageSize` 配置时，返回 HTTP 400 ProblemDetails；错误码 SHALL 表示系统保护限制，不得表述为 115 网页端官方上限
6. THE File_Service SHALL 按 `docs/115-api-docs.md` 已采集 schema 归一化 `/files/search` 顶层字段：`count`、`page_size`、`page_count`、`offset`、`is_asc`、`order`、`suffix`、`fc_mix`、`type`、`state`、`error`、`errCode`
7. WHEN 搜索响应 `data` 为数组时，THE File_Service SHALL 映射 2026-05-04 已采集的搜索 item 字段：`fid`、`aid`、`cid`、`n`、`s`、`sta`、`pc`、`fc`、`m`、`t`、`te`、`tp`、`ico`、`dp`、`ns`、`sha`、`hdf`、`fl`、`u`、`iv`、`play_long` 等；未在文档中列明的字段不得推断
8. THE File_Service SHALL 将 `CloudDriveSearchListDto.ItemSchemaStatus` 置为 `"available"`；IF 后续响应不再符合已采集数组 item schema，THEN 返回 `Items = []` 与 `ItemSchemaStatus = "not_collected"`，并记录 Warning 日志
9. THE File_Service SHALL 将搜索 item 的 `ns` 作为高亮片段原文或清洗后的显示名候选保存，不得要求前端把 `ns` 作为可信 HTML 直接渲染；`fc`、`type`、`sta`、`m` 等值域仍以未采集语义处理，不得硬编码为官方枚举
10. WHEN 搜索无匹配结果时，返回 HTTP 200 + 空数组，`ItemSchemaStatus` 保持 `"available"`

### 需求 6：查看文件详情

**用户故事：** 作为用户，我希望查看单个文件或文件夹的详细属性。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET /api/files/{fileId}` 端点
2. THE File_Service SHALL 通过网页端详情接口（待补采集）或从文件列表响应提取字段返回
3. THE File_Service SHALL 返回可获取属性：`fileId`/`fileName`/`pickCode`/`sha1`/`uploadTime`/`updateTime`/`size`/`isVideo`/`playDuration`/`thumb`/`originalUrl`；未返回字段使用 `null` 而非伪造
4. IF 文件不存在，THEN 返回 HTTP 404 ProblemDetails

### 需求 7：文件上传

**用户故事：** 作为用户，我希望将本地文件上传到 115 网盘。

> **状态**：**已采集完整三步上传链路**（2026-04-18 手动抓包实测）。网页端统一使用 `sampleinitupload.php`（无论文件大小），大文件与小文件走同一初始化接口，通过响应字段分流秒传/OSS 直传。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `POST /api/files/upload` 端点
2. THE File_Service SHALL 首先调用 `POST uplb.115.com/3.0/sampleinitupload.php`（Content-Type: `application/x-www-form-urlencoded`），必填参数 `userid`（当前 115 账号 UID）、`filename`、`filesize`（字节）、`target=U_1_{cid}`（`cid=0` 表示根目录）；响应为 `text/html` 实际为 JSON 载荷
3. IF 响应载荷包含 `file_id` 且无 OSS 凭证字段，THEN 判定为秒传成功并直接返回文件信息
4. 非秒传时响应包含 OSS 直传凭证：`{ object, accessid, host, policy, signature, expire, callback }`（`host` 形如 `https://fhnfile.oss-cn-shenzhen.aliyuncs.com`；`callback` 为 base64 编码的 OSS callback 配置，其 `callbackUrl` 固定为 `http://uplb.115.com/3.0/samplecompleteupload.php`）
5. THE File_Service SHALL 使用 OSS PostObject 方式（`POST {host}/`，multipart/form-data，字段包含 `key=${object}`、`policy`、`OSSAccessKeyId=${accessid}`、`signature`、`callback`、`file`）将文件直传至 115 的 OSS bucket
6. THE File_Service SHALL 将 OSS 直传的响应体（OSS callback 透传回的 115 业务响应）归一化为 `{ state, code, data: { file_id, file_name, pick_code, sha1, file_size, cid, user_id, is_video, ... } }` 并作为完成结果返回
7. THE File_Service SHALL 支持在请求中指定目标目录 ID（映射到 `target=U_1_{cid}`）
8. THE File_Service SHALL 为大文件后台上传保留可恢复元数据（`TempFilePath` + `pickCode` + OSS 凭证快照），凭证过期（`expire` 字段到期）时重新调用 `sampleinitupload.php` 获取新凭证
9. WHEN 文件较大或可能超时时，THE File_Service SHALL 通过 Background_Job 异步执行并提供进度查询
10. IF 网络中断且本地恢复资源不可用，THEN 返回 HTTP 502 ProblemDetails 并释放资源
11. IF OSS 直传响应 `state=false`，THEN 将响应的 `code` 与 `message` 映射为上传错误并返回 HTTP 502
12. WHEN 上传成功后，THE File_Service SHALL 返回文件信息（`fileId`/`fileName`/`directoryId`/`pickCode`/`sha1`）

### 需求 8：文件下载

**用户故事：** 作为用户，我希望从 115 网盘下载文件。

> **状态**：**下载地址 API 待补采集**。

#### 验收标准

1. THE WebApi_Layer SHALL 预留 `GET /api/files/{pickCode}/download` 端点
2. THE File_Service SHALL 调用网页端下载地址接口（待补采集）获取临时下载 URL
3. THE File_Service SHALL 返回临时下载 URL 或受控代理地址
4. IF 文件不存在，THEN 返回 HTTP 404 ProblemDetails
5. IF 下载地址获取失败，THEN 返回 HTTP 502 ProblemDetails
6. THE File_Service SHALL 在响应中标识地址的临时性
7. IF 下载链接过期，THEN 前端可再次请求 `/api/files/{pickCode}/download` 获取新链接

### 需求 9：新建文件夹

**用户故事：** 作为用户，我希望在网盘中创建新文件夹。

> **状态**：**新建文件夹 API 待补采集**。

#### 验收标准

1. THE WebApi_Layer SHALL 预留 `POST /api/files/folders` 端点
2. THE File_Service SHALL 调用网页端新建文件夹接口（待补采集），参数含 `parentId`/`folderName`
3. THE File_Service SHALL 验证名称非空、不含非法字符（`/ \ : * ? " < > |`）、长度 ≤ 255 字符
4. IF 父目录不存在，THEN 返回 HTTP 404 ProblemDetails
5. IF 名称格式无效，THEN 返回 HTTP 400 ProblemDetails
6. WHEN 创建成功后，THE File_Service SHALL 返回新文件夹的 `fileId`/`fileName`

### 需求 10：文件移动

**用户故事：** 作为用户，我希望将文件移动到其他目录。

> **状态**：**移动 API 待补采集**。

#### 验收标准

1. THE WebApi_Layer SHALL 预留 `POST /api/files/move` 端点
2. THE File_Service SHALL 调用网页端移动接口（待补采集），参数含 `fileIds[]` 和 `targetDirectoryId`
3. THE File_Service SHALL 支持批量移动
4. IF 目标或源不存在，THEN 返回 HTTP 404 ProblemDetails
5. WHEN 成功后，THE File_Service SHALL 返回操作结果并使受影响目录缓存失效

### 需求 11：文件复制

**用户故事：** 作为用户，我希望将文件复制到其他目录。

> **状态**：**复制 API 待补采集**。

#### 验收标准

1. THE WebApi_Layer SHALL 预留 `POST /api/files/copy` 端点
2. THE File_Service SHALL 调用网页端复制接口（待补采集）
3. THE File_Service SHALL 支持批量复制，允许控制是否重名副本（以采集为准）
4. IF 目标或源不存在，THEN 返回 HTTP 404 ProblemDetails
5. WHEN 成功后返回操作结果

### 需求 12：文件删除

**用户故事：** 作为用户，我希望删除文件（移至回收站）。

> **状态**：**删除 API 待补采集**。

#### 验收标准

1. THE WebApi_Layer SHALL 预留 `DELETE /api/files` 端点（请求体含 `fileIds[]`）
2. THE File_Service SHALL 调用网页端删除接口（待补采集）
3. THE File_Service SHALL 支持批量删除
4. IF 文件不存在，THEN 返回 HTTP 404 ProblemDetails
5. WHEN 成功后返回操作结果并使缓存失效

### 需求 13：文件重命名

**用户故事：** 作为用户，我希望重命名文件或文件夹。

> **状态**：**重命名 API 待补采集**。

#### 验收标准

1. THE WebApi_Layer SHALL 预留 `PATCH /api/files/{fileId}` 端点（请求体含 `newFileName`）
2. THE File_Service SHALL 调用网页端重命名接口（待补采集）
3. THE File_Service SHALL 验证名称非空、不含非法字符、长度符合限制
4. IF 文件不存在，THEN 返回 HTTP 404 ProblemDetails
5. IF 名称格式无效，THEN 返回 HTTP 400 ProblemDetails
6. WHEN 成功后返回更新后文件信息

### 需求 13a：文件星标

**用户故事：** 作为用户，我希望给文件添加/取消星标。

> **状态**：**星标切换 API 待补采集**。列表接口已支持 `star=1` 筛选。

#### 验收标准

1. THE WebApi_Layer SHALL 预留 `POST /api/files/{fileId}/star` 端点（请求体含 `starred: boolean`）
2. THE File_Service SHALL 调用网页端星标切换接口（待补采集）
3. IF 文件不存在，THEN 返回 HTTP 404 ProblemDetails
4. WHEN 成功后返回更新后星标状态

### 需求 14：回收站管理

**用户故事：** 作为用户，我希望查看和管理回收站中的文件。

> **状态**：列表查询顶层字段已采集（`GET webapi.115.com/rb`）；回收站项目明细字段、还原/彻底删除/清空 API **待补采集**。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET /api/recycle` 端点，支持 query 参数 `offset`/`limit`
2. WHEN 用户请求查看回收站时，THE File_Service SHALL 调用 `GET webapi.115.com/rb`，必填参数 `aid=7`（回收站档案）、`cid=0`、`format=json`，采用 `offset + limit` 分页（默认 `limit=30`；最大值仅受系统自有 `CloudDrive:MaxPageSize` 配置约束）
3. THE File_Service SHALL 仅按 `docs/115-api-docs.md` 已采集 schema 归一化顶层字段：`count`、`rb_pass`、`state`、`error`、`offset`、`page_size`、`order`、`is_asc`
4. IF 响应缺失项目明细字段或 `data` 字段，THEN THE File_Service SHALL 返回 `CloudDriveRecycleListDto { items: [], itemSchemaStatus: 'not_collected' }`，不得伪造 `fileName/fileSize/deletedTime/pickCode/status` 等未采集字段
5. IF 后续 `docs/115-api-docs.md` 补充回收站项目明细 schema，THEN THE File_Service SHALL 只映射文档中明确采集到的字段；在此之前 `CloudDriveRecycleBinItemDto` 字段均视为不可用
6. THE File_Service SHALL 将响应的 `count` 字段从 string 转 number
7. THE WebApi_Layer SHALL 预留 `POST /api/recycle/restore` 端点（还原，待采集）
8. THE WebApi_Layer SHALL 预留 `DELETE /api/recycle/{id}` 端点（彻底删除，待采集）
9. THE WebApi_Layer SHALL 预留 `DELETE /api/recycle` 端点（清空，待采集）；清空 SHALL 提交为 Background_Job 异步执行

### 需求 15：图片文件预览

**用户故事：** 作为用户，我希望在线预览网盘中的图片。

> **说明**：网页端文件列表响应中 `u` 字段（`originalUrl`）为图片原图地址，可直接渲染；若过期，通过重新请求列表或下载地址获取新 URL。

#### 验收标准

1. WHEN 用户请求预览图片时，THE Preview_Service SHALL 直接返回 `CloudDriveFileDto.originalUrl`
2. IF `originalUrl` 为空，THEN THE Preview_Service SHALL 调用下载地址接口（待补采集）兜底
3. THE Preview_Service SHALL 支持 JPG/JPEG/PNG/GIF/BMP/WEBP
4. THE Preview_Service SHALL 排除 SVG（脚本注入风险）
5. IF 文件类型不属于支持格式，THEN 返回 HTTP 400 ProblemDetails
6. IF 文件不存在，THEN 返回 HTTP 404 ProblemDetails

### 需求 16：视频播放

**用户故事：** 作为用户，我希望在线播放视频。

> **重要**：`webapi.115.com/files/video` 网页端实际为 **GET** 方法（非开放平台的 POST），参数名 `pickcode`（小写无下划线）。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET /api/videos/{pickCode}/play` 端点
2. THE Video_Service SHALL 调用 `GET webapi.115.com/files/video?pickcode=...&share_id=0&local=1&format=json` 获取视频播放端点响应；`share_id=0` 表示非分享场景，分享场景传入实际分享 ID。当前仅采集错误响应 schema，成功播放地址与元数据待补采集
3. THE Video_Service SHALL 仅按 `docs/115-api-docs.md` 当前已采集 schema 处理 `/files/video` 错误响应字段：`state`、`errno`、`error`
4. IF `/files/video` 尚未采集到成功播放 schema，THEN THE Video_Service SHALL 返回 `video_play_schema_not_collected` ProblemDetails 或 `CloudDriveVideoPlayDto { playbackSchemaStatus: 'not_collected', qualityOptions: [], audioTracks: [] }`，不得伪造播放地址、清晰度枚举或播放元数据
5. IF 后续 `docs/115-api-docs.md` 补充成功播放 schema，THEN THE Video_Service SHALL 只映射文档中明确采集到的播放地址、清晰度、音轨与元数据字段，并将未采集字段返回 `null` 或空数组
6. IF 视频尚未完成转码，THEN 返回 `status: 'transcoding'` 状态并附带 `jobId`（若已提交转码）
6a. THE Video_Service SHALL 通过 `GET webapi.115.com/files/video_push?pickcode=...&op=vip_push&format=json` 提交主动转码请求（`op=pay_push` 为备选，用于按量计费场景）；备选路径为 `GET webapi.115.com/movies/transcode?pickcode=...&format=json`；两路径均在非 VIP 场景下返回 `{ state: false, errNo: N, errno: N, error: '...', request: '...' }` 错误响应
7. IF 后续已采集成功播放 schema 中包含 VIP 受限或升级引导字段，THEN THE Video_Service SHALL 按采集字段映射到 `restrictionReason` 或 `usageRestrictionNotice`；在字段未采集前不得基于 URL 猜测受限状态
8. IF 文件不是视频类型，THEN 返回 HTTP 400 ProblemDetails
9. IF 文件不存在，THEN 返回 HTTP 404 ProblemDetails
10. IF 后续成功播放 schema 可用并返回播放地址，THEN THE Video_Service SHALL 在响应中标识播放地址的临时性
11. THE WebApi_Layer SHALL 将主动转码提交暴露为独立端点 `POST /api/videos/{pickCode}/transcode`；播放端点本身 SHALL 保持 `GET /api/videos/{pickCode}/play`

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

> **状态**：**已采集读/写端点**（2026-05-04 自动化场景确认；2026-04-18 手动场景补充历史对象字段）。网页端使用 `webapi.115.com/files/history`，错误字段为 `state:boolean` + `error:string`（非 `errNo`/`errCode` 风格）。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET /api/videos/{pickCode}/history` 端点
2. THE WebApi_Layer SHALL 提供 `POST /api/videos/{pickCode}/history` 端点（请求体含 `time: number`/`watchEnd: boolean`）
3. THE Video_Service SHALL 调用 `GET webapi.115.com/files/history?pick_code=...&format=json`（可选 `&fetch=one`）读取播放进度；2026-05-04 自动样本返回空数组，2026-04-18 手动样本记录 `fetch=one&category=1` 分支可能返回对象字段 `{ file_id, file_name, hash, pick_code, time, add_time }`，实现 SHALL 兼容空数组/对象两种形态
4. THE Video_Service SHALL 调用 `POST webapi.115.com/files/history`（Content-Type: `application/x-www-form-urlencoded`，body: `pick_code=...&op=...&time=N&definition=N`）写入播放进度；`watchEnd` 映射为特定 `op` 值；响应结构 `{ state: boolean, error: string }`
5. IF 读取响应缺失播放进度对象字段或返回空数组，THEN THE Video_Service SHALL 返回空播放进度/从头播放语义，不得伪造 `file_id/file_name/hash/time/add_time`

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

> **状态**：列表/配额/统计接口已采集；创建/删除/清空**待补采集**（见需求 21）。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET /api/offline/tasks` 端点，参数 `page`（默认 1）
2. THE Offline_Service SHALL 调用 `POST 115.com/web/lixian/?ct=lixian&ac=task_lists`（必填 Query 参数 `ct=lixian&ac=task_lists`；**Body 需签名参数** `page/stat/uid/sign/time`）
3. THE Offline_Service SHALL 使用 `Signature_Service` 生成 POST Body `{ page, stat, uid, sign, time }`（`stat` 为任务状态过滤参数，默认 0 表示全部）；签名算法尚未在 `docs/115-api-docs.md` 中采集，默认实现 SHALL 返回 `offline_signature_not_available` ProblemDetails，直到通过真实 Cookie 场景补采集并验证签名算法
4. THE Offline_Service SHALL 仅按 `docs/115-api-docs.md` 已采集 schema 归一化任务列表顶层字段：`page`、`page_count`、`page_row`、`page_size`、`count`、`quota`、`total`、`state`、`real_state`、`errtype`
5. IF `tasks` 对象的 item schema 未采集，THEN THE Offline_Service SHALL 返回 `CloudDriveOfflineTaskListDto { items: [], itemSchemaStatus: 'not_collected' }`，不得伪造 `infoHash/name/size/addTime/lastUpdate/percentDone/url/fileId/parentId/status` 等任务字段
6. IF 后续 `docs/115-api-docs.md` 补充云下载任务 item schema，THEN THE Offline_Service SHALL 只映射文档中明确采集到的字段；在此之前不得采用开放平台 `status -1/0/1/2` 推断值域
7. THE WebApi_Layer SHALL 提供 `GET /api/offline/quota` 端点，调用 `GET 115.com/web/lixian/?ct=lixian&ac=get_quota_package_info`；归一化响应为 `CloudDriveOfflineQuotaDto { package: object, count, surplus, maxSize, used }`
8. THE WebApi_Layer SHALL 提供 `GET /api/offline/stats` 端点，调用 `POST 115.com/web/lixian/?ct=lixian&ac=task_count`；统计字段映射：`total_count→total`、`failed_count→failed`、`finished_count→completed`、`downloading_count→active`；归一化为 `{ total, active, completed, failed }`
9. THE Cloud_Drive_Service SHALL 将签名 `sign` 参数从日志中脱敏

### 需求 21：云下载任务管理

**用户故事：** 作为用户，我希望创建、删除、清空云下载任务。

> **状态**：创建/删除/清空任务 API **待补采集**。

#### 验收标准

1. THE WebApi_Layer SHALL 预留 `POST /api/offline/tasks/urls`（`{ urls: string[], saveDirectoryId? }`）
2. THE WebApi_Layer SHALL 预留 `POST /api/offline/tasks/bt`（`{ infoHash, wanted, savePath, torrentSha1, pickCode, saveDirectoryId? }`）
3. THE WebApi_Layer SHALL 预留 `POST /api/offline/parse-torrent` 端点（BT 种子解析）
4. THE WebApi_Layer SHALL 预留 `DELETE /api/offline/tasks/{infoHash}`
5. THE WebApi_Layer SHALL 预留 `POST /api/offline/tasks:clear`（清空）；SHALL 提交为 Background_Job 异步执行
6. WHEN 任务操作完成后，THE Offline_Service SHALL 使任务列表缓存失效

### 需求 22：分享管理

**用户故事：** 作为用户，我希望查看我的分享（我的/发送/接收）。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET /api/shares` 端点，参数 `tab: 'slist' | 'send' | 'receive'`（必填）/`offset`/`limit`
2. WHEN `tab=slist` 时，THE Share_Service SHALL 调用 `GET webapi.115.com/share/slist?offset=0&limit=30&format=json`
3. WHEN `tab=send` 时，调用 `GET webapi.115.com/share/send?offset=0&limit=30&format=json`
4. WHEN `tab=receive` 时，调用 `GET webapi.115.com/share/receive?offset=0&limit=30&format=json`
5. THE Share_Service SHALL 仅按 `docs/115-api-docs.md` 已采集 schema 归一化分享顶层字段：`/share/slist` 的 `count/list/define_receive_code/bind_receive_code/state/error/errno`，以及 `/share/send`、`/share/receive` 的 `state/error/errNo/errno/request`
6. IF `list` item schema 或 send/receive 成功列表 schema 未采集，THEN THE Share_Service SHALL 返回 `CloudDriveShareListDto { items: [], itemSchemaStatus: 'not_collected' }`，不得伪造 `shareId/title/createTime/status/receiveCode/shareUrl` 等字段
7. IF 后续 `docs/115-api-docs.md` 补充分享 item schema，THEN THE Share_Service SHALL 只映射文档中明确采集到的字段
8. WHEN `tab` 值不支持时，返回 HTTP 400 ProblemDetails
9. **注**：三个分享接口当前采集样本均为失败或空列表响应（低置信度），成功 item 结构待实际验证

### 需求 23：用户中心聚合

**用户故事：** 作为用户，我希望查看已授权的 115 账号信息（基础信息/VIP/余额/存储空间）。

#### 验收标准

1. THE WebApi_Layer SHALL 提供 `GET /api/user/profile` 聚合端点，返回 `CloudDriveUserProfileDto`
2. THE User_Service SHALL 并行调用并聚合：
   - `GET my.115.com/proapi/3.0/index.php?method=user_info&uid=...`（基础信息）
   - `GET webapi.115.com/user/vip_limit?feature=2`（VIP 状态）
   - `GET vip.115.com/?ct=order&ac=get_yun_card`（VIP 卡详情）
   - `GET pay.115.com/`（账户余额）
   - `GET webapi.115.com/files/index_info?count_space_nums=1`（存储空间）
3. THE User_Service SHALL 归一化基础信息为 `CloudDriveUserInfoDto`：`user_id→uid, user_name→nickname, user_face→avatarUrl, is_vip→isVip, pub_mobile_mask→maskedPhone, pub_email_mask→maskedEmail`
4. THE User_Service SHALL **严禁**透传以下原始敏感字段（官方响应 `proapi/3.0/index.php` 返回的个人隐私字段）至前端：
   - **联系方式**：`pub_mobile`、`pub_email`
   - **支付账号**：`alipay`、`wechat`、`weibo`
   - **住址与位置**：`address`、`homepage`、`location`、`location_show`、`location_birth`、`location_birth_show`、`location_link`、`location_link_show`
   - **生日与风水**：`birthday`、`birthday_show`、`star`、`calendar_type`
   - **人口信息**：`gender`、`gender_show`、`blood_type`、`blood_type_show`、`is_marry`、`is_marry_show`、`height`、`weight`
   - **职业与收入**：`education`、`job`、`job_show`、`salary`
   - **兴趣爱好**：`like_celeb`、`like_music`、`like_animal`、`like_book`、`like_video`、`interest`
   - **其他隐私**：`user_name_checking`、`user_name_prepub`、`qrcode_url`、`share_url`、`user_public`（权限配置对象）
   SHALL 在归一化层过滤或脱敏，严禁将 `data` 对象原样透传
5. THE User_Service SHALL 处理 `vip.115.com/?ct=order&ac=get_yun_card` 响应字段：`{ num, url, vip_count, space_count, forever_count, sqcode_count, sqcode_cancel_count, 00_balance }`；其中 `00_balance`（以数字开头）需通过 `data["00_balance"]` 索引访问，归一化为 `balance`
6. THE User_Service SHALL 仅在 `docs/115-api-docs.md` 明确采集到空间详情字段时展示空间占用格式化字段；当前 `webapi.115.com/files/index_info` 仅采集 `data: object`，因此 `CloudDriveSpaceInfoDto` SHALL 标注 `schemaStatus='not_collected'`，不得伪造 `size_format`、总空间、已用空间或分类占比字段
7. IF 任一子接口失败，THEN 对应子模块设为 `null`，其他正常返回（错误隔离）
8. THE WebApi_Layer SHALL 提供细粒度端点：`GET /api/user/info`/`GET /api/user/vip`/`GET /api/user/balance`/`GET /api/user/storage`
9. IF Cookie 无效，THEN 返回 HTTP 401 ProblemDetails

### 需求 24：API 频控与限流

**用户故事：** 作为系统管理员，我希望控制对 115 API 的调用频率避免触发风控。

#### 验收标准

1. THE Rate_Limiter SHALL 对所有发往 115 API 的请求进行频率控制
2. THE Rate_Limiter SHALL 按场景分类设置限流规则（文件浏览/媒体扫描/后台批量）
3. WHEN 请求频率超限时，THE Rate_Limiter SHALL 排入队列或返回 HTTP 429 ProblemDetails
4. IF 115 API 返回频控响应（HTTP 429 或 errNo 匹配频控语义），THEN THE Cloud_Drive_Service SHALL 执行受限次数退避重试
5. THE Cloud_Drive_Service 与 Rate_Limiter SHALL 记录请求排队、拒绝、限流信号、退避重试的可观测日志
6. THE Rate_Limiter SHALL 与 Metadata_Cache 协同，通过缓存降低重复调用

### 需求 25：统一错误响应（ProblemDetails）

**用户故事：** 作为前端工程师，我希望后端统一使用 ProblemDetails 返回错误。

> **重要**：115 网页端各接口错误字段**不统一**，经自动化采集验证至少有 **9 种变体**（包括字段名大小写、类型差异、同时出现双字段等），`ExceptionHandlingMiddleware` 需按接口来源**逐 URL 路径**适配。

#### 验收标准

1. THE WebApi_Layer SHALL 使用 `application/problem+json` 返回所有错误响应
2. THE ExceptionHandlingMiddleware SHALL 按 **URL 路径逐项匹配**以下网页端错误字段变体：

   | # | 端点 | state 类型 | 错误码字段（类型） | 错误消息字段 |
   |---|------|----------|------------------|------------|
   | 1 | `webapi.115.com/files` | boolean | `errNo`（number） | `error` |
   | 2 | `webapi.115.com/files/search` | boolean | **`errCode`**（number） | `error` |
   | 3 | `webapi.115.com/files/video` | boolean | **`errno`**（小写，number） | `error` |
   | 4 | `webapi.115.com/movies/subtitle` | boolean | **`errNo`**（大写 N，number） | `error` |
   | 5 | `webapi.115.com/rb` | boolean | *（无）* | `error` |
   | 6 | `webapi.115.com/share/slist` | boolean | `errno`（小写，number） | `error` |
   | 7 | `webapi.115.com/share/send`、`/share/receive` | boolean | **同时返回 `errNo` 和 `errno`** 两字段（优先读 `errNo`，fallback `errno`） | `error` |
   | 8 | `webapi.115.com/user/vip_limit`、`/user/fingerprint` | boolean | **`code`（string）** | `message` |
   | 9 | `webapi.115.com/files/index_info` | boolean | `code`（number） | `error` |
   | 10 | `my.115.com/proapi/*` | boolean | `code`（number） | `message` |
   | 11 | `vip.115.com/` | boolean | *（无）* | *（无）* |
   | 12 | **`pay.115.com/`** | boolean | `code`（number） | **`msg`**（非 `message`） |
   | 13 | `act.115.com/*` | **number** | `code`（number） | `message` |

   > **指针**：实现时在 `ExceptionHandlingMiddleware` 中定义 `ErrorFieldAdapter` 注册表，按 URL 路径 hostname + path 精确匹配；若新增接口未注册则 fallback 到通用 `{ state, error?, code?, message? }` 解析器，并记录 Warning 日志。

3. THE ExceptionHandlingMiddleware SHALL 按请求 URL 路径匹配对应的字段适配器
4. THE ExceptionHandlingMiddleware SHALL 按 errNo/code 归类为统一错误类别并映射 HTTP 状态码：
   - 未登录 → 401 Unauthorized
   - 频控 → 429 Too Many Requests
   - 文件不存在 → 404 Not Found
   - 分享过期 → 410 Gone
   - 网络异常 → 502 Bad Gateway
   - 未知业务错误 → 502 Bad Gateway
5. THE ProblemDetails SHALL 包含标准字段 `type`/`title`/`status`/`detail`/`instance`，以及自定义 `extensions.upstreamCode`（115 errNo/code）/`extensions.upstreamMessage`（115 error/message）
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
3. THE Cookie_Manager SHALL 在日志中仅记录 Cookie 前 4 位 + `****`
4. THE Signature_Service SHALL 不记录完整 UID 和签名结果
5. THE Gateway_Layer SHALL 记录 API 调用概要：`endpoint`/`method`/`httpStatus`/`upstreamCode`/`latencyMs`（**不记录**原始请求/响应体）
6. THE LoggingBehavior SHALL 对 5xx 错误记录 `Error` 级别，对 4xx 错误记录 `Warning` 级别

### 需求 27：元数据缓存与目录刷新

**用户故事：** 作为用户，我希望浏览响应稳定快速，降低频控风险。

#### 验收标准

1. THE Metadata_Cache SHALL 按用户 + 115 账号 + 目录维度对文件列表元数据使用短 TTL 缓存，默认 TTL 不超过 30 秒，支持配置调低或关闭
2. THE Metadata_Cache SHALL 缓存用户基础信息 5 分钟（`user:info`）
3. THE Metadata_Cache SHALL 缓存 VIP 状态 5 分钟（`user:vip`）
4. THE Metadata_Cache SHALL 缓存存储空间 2 分钟（`user:storage`）
5. THE Metadata_Cache SHALL NOT 对文件列表使用长 TTL 或跨写操作复用旧缓存；手动刷新与写操作成功后必须绕过或失效目录列表短 TTL 缓存
6. WHEN 写操作成功后（上传/移动/复制/删除/重命名/还原/清空），THE File_Service SHALL 使受影响目录缓存失效（通过领域事件 `DirectoryContentChanged`）
7. WHEN 用户主动刷新目录时，THE File_Service SHALL 绕过或重建缓存
8. THE Metadata_Cache SHALL 只缓存 115 实际返回的数据，不得伪造字段
9. THE CloudDrive_Module SHALL 通过 Application 层抽象 `ICloudDriveMetadataCache`（`Application/Abstractions/`）封装缓存访问，Infrastructure 层基于 `IDistributedCache` 实现

### 需求 28：长耗时任务管理

**用户故事：** 作为用户，我希望大文件上传、批量操作、清空回收站等长耗时操作能后台执行并可查询进度。

#### 验收标准

1. THE File_Service 与 Offline_Service SHALL 支持提交 Background_Job：大文件上传、主动视频转码（已采集：`GET webapi.115.com/files/video_push?pickcode=...&op=vip_push`，见需求 16 第 6a 条），并可为批量复制/移动/删除、清空回收站、云下载清空任务等未采集写操作创建任务占位；未采集写操作的任务执行结果 SHALL 返回 `cloud_drive_api_not_collected`，直到 `docs/115-api-docs.md` 补充真实 115 网页端接口证据
2. THE Background_Job SHALL 记录任务类型、关联资源标识、状态、进度、起止时间、错误信息
3. THE Cloud_Drive_Service SHALL 提供 `GET /api/jobs/{jobId}` 供客户端轮询
4. WHEN 操作被受理为 Background_Job 时，THE WebApi_Layer SHALL 返回 HTTP 202 Accepted 并携带 `jobId`
5. IF 操作支持安全重试，THEN THE Background_Job SHALL 保留可恢复状态（如上传的 `TempFilePath`/`pickCode`），支持从安全检查点继续
6. THE Cloud_Drive_Service SHALL 为 Background_Job 保留审计日志，不含 Cookie 或签名参数
7. IF 应用进程在任务完成前重启，THEN THE Cloud_Drive_Service SHALL 启动后根据持久化任务记录协调任务状态；可恢复任务尝试断点续传，不可恢复任务标记为失败

### 需求 29：业务模块组织结构

**用户故事：** 作为开发者，我希望 115 网盘业务按 DDD 分层组织在 net10 框架内。

#### 验收标准

1. THE CloudDrive_Module SHALL 位于 `src/Application/CloudDrive/`，按子领域（Auth/Files/Search/Video/RecycleBin/Offline/Share/User）组织 Commands/Queries/Handlers/DTO/Validators
2. THE Infrastructure_Layer SHALL 在 `src/Infrastructure/CloudDrive/` 实现所有 Gateway（`FileGateway`/`VideoGateway`/`RecycleBinGateway`/`OfflineGateway`/`ShareGateway`/`UserGateway`）及 `CookieManager`/`SignatureService`
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
8. THE WebApi_Layer SHALL NOT 在公开前端端点中要求或暴露 `CloudAccountUid`；单账号上下文由后端根据已配置 Cookie 会话解析。`CloudAccountUid` 仅允许出现在受保护的运维端点、内部实体、缓存键、限流键和审计记录中。

## 非功能性需求

### 性能与响应

1. 文件列表查询首屏 P95 响应时间 ≤ 1.5 秒（缓存命中场景 ≤ 300ms）
2. 视频播放端点响应 P95 ≤ 2 秒；成功播放地址 schema 未采集时该指标仅覆盖错误/不可用空态返回
3. 用户聚合接口 P95 ≤ 2 秒（并行子调用）
4. 单个 API 调用默认超时 10 秒，批量上传/清空任务等长耗时操作不受限
5. Polly 策略：瞬时错误重试 3 次 + 指数退避 (1s/2s/4s)，超过阈值熔断 30 秒

### 安全

1. **Cookie 加密存储**：使用 ASP.NET Core Data Protection，`IDataProtector` purpose: `CloudDrive.Cookie.v1`
2. **Cookie 热加载**：通过 `IOptionsMonitor<CookieOptions>` 监听配置变更
3. **敏感字段归一化层过滤**：用户信息聚合端点严禁透传原始 `pub_mobile`/`pub_email` 等字段
4. **日志脱敏**：Cookie 前 4 位 + `****`；签名 `sign` 完全脱敏
5. **CORS 白名单**：仅允许显式配置的前端域名
6. **速率限制**：基于 ASP.NET Core `RateLimiter` 中间件按场景分类限流

### 可观测性

1. 结构化日志（Serilog）：记录 request_id/endpoint/method/status/upstream_code/latency_ms
2. OpenTelemetry traces：覆盖 HTTP 入站请求 → Application Handler → Gateway → 115 API 全链路
3. Metrics：`cloud_drive.requests.total`、`cloud_drive.requests.latency_ms`、`cloud_drive.errors.total{category}`
4. 健康检查：`/health/live`（应用存活）、`/health/ready`（Cookie 有效 + 数据库可达）

### 部署与运维

1. Docker 镜像化部署；支持 Kubernetes 多副本横向扩展
2. Data Protection key ring 持久化到共享存储（Redis/文件系统）以支持多实例
3. `appsettings.json` + 环境变量覆盖机制；生产环境 Cookie 通过 Secret 注入
4. Quartz.NET `JobStore` 使用 SqlSugar 持久化，支持任务跨实例协调
5. 数据库迁移通过 SqlSugar CodeFirst 或独立脚本执行

### 测试覆盖

1. Domain 层单元测试覆盖率 ≥ 90%
2. Application 层 Handler 单元测试覆盖所有 Success / ValidationFailure / UpstreamError 路径
3. Gateway 层使用 `HttpMessageHandler` mock 测试 115 API 调用与归一化
4. WebApi 层集成测试覆盖 ProblemDetails、CORS、认证失败、频控 6 个关键场景
5. 契约测试：OpenAPI schema 与前端 TypeScript 类型定义对齐
