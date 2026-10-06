# 115 cloud drive 前后端 Specs 全量审计报告

## 审核快照

- **审核时间**：2026-05-04 00:43:44
- **本轮范围**：`.kiro/specs/115-cloud-drive-integration` + `.kiro/specs/115-cloud-drive-frontend` 全量交叉审计
- **权威接口依据**：`docs/115-api-docs.md`（2026-04-08 自动采集 + 2026-04-18 手动补充）
- **架构方案依据**：后端访问 115 网页端 API；前端只访问后端 `/api/*`
- **Specs 基线**：
  - frontend `requirements.md` 30921 bytes / `design.md` 61544 bytes / `tasks.md` 43073 bytes
  - integration `requirements.md` 44926 bytes / `design.md` 89758 bytes / `tasks.md` 87034 bytes
- **联网交叉核对**：公开搜索未找到比本地抓包文档更权威的 115 网页端 API 文档；第三方资料仅能佐证 `/files/video`、`/files/history` 等网页端接口形态，不作为本轮判定依据。

## 总结论

架构边界已对齐：**前端不直连 115，后端 integration 通过 Cookie 会话访问 115 网页端 API，再向前端暴露标准化 `/api/*`**。

阻塞问题不在架构方向，而在 integration spec 的接口映射细节与任务口径漂移：部分 design/tasks 使用了 `docs/115-api-docs.md` 不存在的 115 路径，部分 tasks 又把 requirements 明确标为「待补采集」的写操作写成了具体 115 端点。

## API 对齐矩阵摘要

| 范围 | 结论 | 说明 |
|---|---|---|
| 前端访问边界 | 通过 | frontend requirements/design 明确只访问 `/api/*`，不处理 Cookie/UID/CID/USSID/sign |
| 后端数据源边界 | 通过 | integration requirements/design 明确使用 `webapi.115.com`、`uplb.115.com`、`115.com/web/lixian` 等网页端 API |
| 文件列表 `/files` | 通过 | `aid=1`、`cid`、`offset/limit`、`show_dir`、`star`、`format=json` 等主参数已覆盖 |
| 文件搜索 `/files/search` | 部分失败 | requirements 已按网页端参数修正；tasks 仍残留开放平台参数 |
| 上传 `sampleinitupload` + OSS | 通过 | requirements/design/tasks 已统一为后端代理上传 |
| 视频播放/转码/历史 | 基本通过 | `GET /files/video`、`video_push`、`files/history` 已对齐；第三方联网资料也仅作旁证 |
| 回收站列表 `/rb` | 通过 | `aid=7/cid=0/format=json` 与动态键说明已覆盖 |
| 分享 `/share/*` | 通过 | `format=json` 与错误字段变体已覆盖 |
| 云下载读取 `/web/lixian` | 部分失败 | `ct=lixian&ac=*` 与 body 字段已覆盖；签名算法描述仍有占位冲突 |
| 用户聚合接口 | 失败 | design/tasks 使用了本地文档不存在的 `webapi.115.com/user/info` 与 `/user/index_info` |
| 错误字段适配 | 失败 | `/files/index_info` 的 `code` 类型在 spec 中写错 |
| 待补采集写操作 | 失败 | tasks 多处写成具体 115 端点，违反 docs 权威边界 |
| 前后端 `/api/*` 契约 | 部分通过 | 主消费端点一致；`GET /api/recycle` 在 integration requirements 中缺显式 WebApi 端点；`POST /api/files/refresh` 是孤儿端点 |

## 问题清单

### H-01 `/files/index_info` 错误字段类型写错

- **specs 位置**：
  - `.kiro/specs/115-cloud-drive-integration/requirements.md:471`
  - `.kiro/specs/115-cloud-drive-integration/design.md:543`
  - `.kiro/specs/115-cloud-drive-integration/tasks.md:192`
- **接口依据**：
  - `docs/115-api-docs.md:336`
  - `docs/115-api-docs.md:354`
  - `docs/115-api-docs.md:358`
  - `docs/115-api-docs.md:382`
- **差异**：docs 中 `/files/index_info` 与 `/files/index_info?count_space_nums=1` 的 `code` 是 `number`，错误消息字段是 `error`；spec 把 `/files/index_info` 合并进 `/user/vip_limit` 的 `code string + message` 规则。
- **影响**：`ErrorFieldAdapter` 会按错误类型解析 `/files/index_info`，导致错误归类与 ProblemDetails 映射不可靠。
- **修复**：拆分错误变体表：
  - `/user/vip_limit`：`code string + message`
  - `/files/index_info`：`code number + error`

### H-02 用户聚合 design/tasks 引用了 docs 不存在的 115 路径

- **specs 位置**：
  - `.kiro/specs/115-cloud-drive-integration/design.md:399`
  - `.kiro/specs/115-cloud-drive-integration/design.md:543`
  - `.kiro/specs/115-cloud-drive-integration/tasks.md:342`
  - `.kiro/specs/115-cloud-drive-integration/tasks.md:607`
  - `.kiro/specs/115-cloud-drive-integration/tasks.md:613`
  - `.kiro/specs/115-cloud-drive-integration/tasks.md:617`
- **接口依据**：
  - `docs/115-api-docs.md:340`
  - `docs/115-api-docs.md:362`
  - `docs/115-api-docs.md:1308`
  - integration requirements 正确引用见 `.kiro/specs/115-cloud-drive-integration/requirements.md:416` 与 `requirements.md:420`
- **差异**：docs 采集到的是 `my.115.com/proapi/3.0/index.php?method=user_info&uid=...` 和 `webapi.115.com/files/index_info?count_space_nums=1`；design/tasks 写成 `webapi.115.com/user/info` 与 `webapi.115.com/user/index_info`。
- **影响**：实现阶段会调用不存在或未采集的路径，违背“按 docs 可以直接访问 115”的要求。
- **修复**：统一改为 requirements 中的真实路径：
  - 基础信息：`GET my.115.com/proapi/3.0/index.php?method=user_info&uid=...`
  - 存储空间：`GET webapi.115.com/files/index_info?count_space_nums=1`

### H-03 tasks 对「待补采集」写操作给出了具体 115 端点

- **specs 位置**：
  - requirements 总边界：`.kiro/specs/115-cloud-drive-integration/requirements.md:17`
  - 待补采集声明：`requirements.md:168`, `184`, `199`, `213`, `227`, `241`, `256`, `269`, `383`
  - tasks 漂移：`.kiro/specs/115-cloud-drive-integration/tasks.md:385`, `392`, `428`, `461`, `467`, `473`, `479`, `484`, `499`, `504`, `506`, `590`, `591`, `592`
- **差异**：requirements 明确文件增删改、下载地址、回收站还原/彻底删除/清空、云下载创建/删除/清空等仍待补采集；tasks 却写了 `webapi.115.com/files/move`、`files/copy`、`rb/delete`、`rb/revert`、`rb/clean`、`add_task_url`、`task_clear` 等具体路径。
- **影响**：执行任务时会把猜测端点当成可实现端点，产生无法按 `docs/115-api-docs.md` 直接访问 115 的实现。
- **修复**：待补采集能力的 Handler 只能返回明确 ProblemDetails（如 `cloud_drive_api_not_collected` / HTTP 501 或 503），不得在 tasks 中写具体 115 路径；除非先补充采集证据到 `docs/115-api-docs.md`。

### H-04 云下载签名算法存在占位冲突

- **specs 位置**：
  - `.kiro/specs/115-cloud-drive-integration/requirements.md:372`
  - `.kiro/specs/115-cloud-drive-integration/design.md:186`
  - `.kiro/specs/115-cloud-drive-integration/tasks.md:68`, `176`, `593`, `704`
- **接口依据**：
  - `docs/115-api-docs.md:1217`
  - `docs/115-api-docs.md:1234`
- **差异**：docs 只证明 `task_lists` body 需要 `page/stat/uid/sign/time`，没有给出签名算法；requirements 写了 `sign = md5(uid + time + ...)` 占位，但 design/tasks 又要求 `ICookieSignatureStrategy` 未实现时返回 `offline_signature_not_available`。
- **影响**：实现者可能落地一个无依据的 MD5 签名，导致真实访问 115 失败。
- **修复**：删除 MD5 占位表达。统一为：签名算法未采集时，`ICookieSignatureStrategy` 默认返回 `offline_signature_not_available`；只有补采集并验证后才能实现真实签名。

### H-05 文件搜索 tasks 残留开放平台参数

- **specs 位置**：
  - `.kiro/specs/115-cloud-drive-integration/requirements.md:125`
  - `.kiro/specs/115-cloud-drive-integration/tasks.md:490`
- **接口依据**：
  - `docs/115-api-docs.md:390`
  - `docs/115-api-docs.md:404`
- **差异**：requirements 已说明网页端采集中未出现 `gte_day/lte_day/fc/suffix/file_label`，本系统暂不采用；tasks 又要求支持这些参数。
- **影响**：会重新引入幻影参数，前后端 DTO 与真实 115 网页端搜索接口不一致。
- **修复**：tasks 只保留 `search_value/aid/cid/type/date/pick_code/count_folders/source/format/offset/limit`。

### M-01 integration requirements 缺显式 `GET /api/recycle` 公开端点

- **specs 位置**：
  - frontend 要求：`.kiro/specs/115-cloud-drive-frontend/requirements.md:166`
  - integration tasks：`.kiro/specs/115-cloud-drive-integration/tasks.md:673`
  - integration requirements 列表逻辑：`.kiro/specs/115-cloud-drive-integration/requirements.md:273`
- **差异**：前端调用 `GET /api/recycle`，integration tasks 也暴露该端点，但 integration requirements 的需求 14 只写了 File_Service 调用 `GET webapi.115.com/rb`，没有明确 `THE WebApi_Layer SHALL provide GET /api/recycle`。
- **影响**：requirements -> design/tasks 追溯不完整。
- **修复**：在需求 14 增加 `THE WebApi_Layer SHALL provide GET /api/recycle`，参数 `offset/limit`。

### M-02 `POST /api/files/refresh` 是孤儿公开端点

- **specs 位置**：
  - `.kiro/specs/115-cloud-drive-integration/design.md:240`
  - `.kiro/specs/115-cloud-drive-integration/tasks.md:668`
- **差异**：integration design/tasks 暴露 `RefreshDirectoryCommand` / `POST /api/files/refresh`，但 integration requirements 与 frontend spec 均没有对应用户需求或消费路径。
- **影响**：公开 API 面增加无需求来源的端点，后续实现与测试没有验收依据。
- **修复**：优先移除公开端点，仅保留内部缓存失效能力；若确需前端刷新绕过缓存，则在前后端 requirements 中补明确需求。

## 已通过项

- 前端不直连 `webapi.115.com` / `uplb.115.com` / `115.com/web/lixian`，只消费 `/api/*`。
- 前端不处理 Cookie、UID、CID、USSID、sign、`CloudAccountUid`。
- 上传链路已统一为后端代理：`POST /api/files/upload` -> `uplb.115.com/3.0/sampleinitupload.php` -> OSS PostObject。
- 视频播放主端点已统一：前端 `GET /api/videos/{pickCode}/play`，后端 `GET webapi.115.com/files/video?pickcode=...&share_id=0&local=1&format=json`。
- 主动转码已独立为 `POST /api/videos/{pickCode}/transcode`。
- 播放历史已按 `GET/POST webapi.115.com/files/history` 进入已采集契约。
- 前端旧 OAuth/PKCE/多账号/`/api/cloud-drive` 口径只作为迁移边界说明出现，未作为目标实现。

## 修复顺序

1. **先修 H-01/H-02**：这是会导致直接访问 115 路径或错误解析失败的硬错误。
2. **再修 H-03/H-05**：清理 tasks 中所有未采集端点和幻影参数，保证执行任务不会偏离 `docs/115-api-docs.md`。
3. **再修 H-04**：统一云下载签名策略，禁止无依据 MD5 占位实现。
4. **最后修 M-01/M-02**：补齐前后端公开 `/api/*` 契约追溯，删除或显式定义孤儿端点。

## 建议修复原则

- `docs/115-api-docs.md` 已采集的接口：后端 SHALL 直接按文档访问 115，不得继续标“待联调”。
- `docs/115-api-docs.md` 未采集的接口：spec 不得写具体 115 path；后端公开端点可以预留，但必须返回明确 ProblemDetails。
- frontend spec 只描述后端 `/api/*` 契约，不应出现任何需要前端理解 115 Cookie、签名、错误字段或原始短字段的实现要求。
