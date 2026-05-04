# 115 cloud drive 前后端 Specs 修复验证报告

## 审核快照

- **审核时间**：2026-05-04 01:09:47
- **审核轮次**：复查
- **复查范围**：`.kiro/specs/115-cloud-drive-integration` + `.kiro/specs/115-cloud-drive-frontend` 的前后端契约与 115 API 对齐残留
- **权威接口依据**：`docs/115-api-docs.md`
- **上轮审核**：`.kiro/specs/115-cloud-drive-integration/audit-history/2026-05-04-004344-full-frontend-backend.md`
- **架构边界**：前端只访问后端 `/api/*`；后端 integration 访问 115 网页端 API

## 联网复核

联网检索未发现比本地 `docs/115-api-docs.md` 更权威的 115 网页端字段级 API 文档。公开资料只能作为旁证，不作为本轮修复依据：

- GreasyFork 第三方脚本可旁证 `webapi.115.com/files`、`webapi.115.com/files/history`、`115.com/web/lixian/?ct=lixian&ac=task_lists` 与 `sign/time/uid` 形态：https://greasyfork.org/de/scripts/495474-115/code
- `115wangpan` 第三方库文档可旁证 Cookie 持久化、搜索、离线任务等能力存在，但未提供本项目所需字段级契约：https://115wangpan.readthedocs.io/en/latest/tutorial.html

## 修复结果

| 编号 | 上轮问题 | 状态 | 修复位置 |
|---|---|---|---|
| H-01 | `/files/index_info` 错误字段类型写错 | 已修复 | `requirements.md` 需求 25 表拆分为 `/user/vip_limit code string + message` 与 `/files/index_info code number + error`；`design.md`/`tasks.md` 同步 |
| H-02 | 用户聚合引用 docs 不存在的路径 | 已修复 | `design.md`/`tasks.md` 统一为 `my.115.com/proapi/3.0/index.php?method=user_info&uid=...`、`webapi.115.com/user/vip_limit?feature=2`、`webapi.115.com/files/index_info?count_space_nums=1` |
| H-03 | 待补采集写操作写成具体 115 端点 | 已修复 | `tasks.md` 中文件写操作、回收站写操作、云下载写操作均改为占位 Handler/Job，返回 `cloud_drive_api_not_collected`，不得猜测 115 path |
| H-04 | 云下载签名算法存在 MD5 占位冲突 | 已修复 | `requirements.md`/`design.md`/`tasks.md` 统一为签名算法未采集时返回 `offline_signature_not_available`，不生成占位签名 |
| H-05 | 搜索残留开放平台参数 | 已修复 | `design.md` 的 `SearchFileInput` 与 `tasks.md` 的 `GET /api/files/search` 只保留 `search_value/aid/cid/type/date/pick_code/count_folders/source/format/offset/limit` 相关契约 |
| M-01 | integration requirements 缺 `GET /api/recycle` | 已修复 | `requirements.md` 需求 14 显式增加 `GET /api/recycle` |
| M-02 | `POST /api/files/refresh` 孤儿公开端点 | 已修复 | `design.md`/`tasks.md` 明确仅保留内部缓存失效能力，不暴露公开刷新端点 |

## 验证命令

- `rg "user/index_info|webapi\.115\.com/user/info|/user/index_info|sign\s*=\s*md5|NotImplementedException|RefreshDirectoryCommand|RefreshDirectoryInput|errMsg|12 种|12种|requirements\.md:454-470|file_label.*支持|gte_day.*支持|lte_day.*支持|suffix\?}" .kiro/specs/115-cloud-drive-integration/...`
- `rg "files/add|files/edit|files/move|files/copy|rb/delete|rb/revert|rb/clean|files/star|add_task_url|add_task_urls|del_task|task_clear" .kiro/specs/115-cloud-drive-integration/...`
- `rg "webapi\.115\.com|115\.com/web|my\.115\.com|vip\.115\.com|pay\.115\.com|uplb\.115\.com|Cookie|sign" .kiro/specs/115-cloud-drive-frontend/...`

## 复查结论

- integration spec 已按 `docs/115-api-docs.md` 收敛：已采集接口可以直接由后端访问 115；未采集写接口不再写具体 115 路径。
- frontend spec 仍保持正确边界：前端不直连 115，不处理 Cookie、签名、错误字段或短字段归一化，只消费后端 `/api/*`。
- 允许保留的字符串：`POST /api/files/refresh` 仅作为“不暴露公开端点”的说明出现；`CloudDriveFileLabelDto` 是响应 DTO 字段，非搜索请求参数；文件列表 `suffix` 属 `/files` 已采集参数，不属于 `/files/search` 幻影参数。

## 2026-05-04 追加复查修复

用户要求再次检查后，发现并修复 7 个 spec 内部一致性问题：

| 编号 | 问题 | 状态 | 修复位置 |
|---|---|---|---|
| F-01 | 公开输入 DTO 仍继承 `CloudDriveAccountScopedInput` | 已修复 | `design.md` 关键输入 DTO 全部移除该基类；`tasks.md` 6.1 改为公开输入 DTO 不得继承或包含 `CloudAccountUid` |
| F-02 | `VideosController` 引用不存在的 `GetVideoPlayUrlQuery` | 已修复 | `tasks.md` 12.7 改为 `GetVideoPlayUrlCommand`，保留可能触发转码副作用的 CQRS 语义 |
| F-03 | 云下载 URL/BT/parse torrent 命令命名不一致 | 已修复 | `design.md` 说明 URL/BT 均映射 `AddOfflineTaskCommand` + `TaskKind`；parse torrent 未采集时由 Controller 直接返回 `cloud_drive_api_not_collected` |
| F-04 | 用户中心 Controller 引用未定义 Query | 已修复 | `design.md` 补 `GetUserProfileQuery`、`GetUserBalanceQuery`、`CloudDriveUserProfileDto`、`CloudDriveBalanceDto`；`tasks.md` storage 改为 `GetUserSpaceQuery` |
| F-05 | 分享 Controller 使用未定义 `GetShareListQuery` | 已修复 | `tasks.md` 按 `tab` 分派到 `GetMySharesQuery` / `GetShareSendsQuery` / `GetShareReceivesQuery` |
| F-06 | 文件详情任务写入未采集路径 `webapi.115.com/files/file` | 已修复 | `tasks.md` 改为从列表/缓存/已知 DTO 组装，无法取得时返回 ProblemDetails，不猜测 115 path |
| F-07 | `/api/offline/stats` 缺 Handler 任务与 DTO 摘要 | 已修复 | `design.md` 补 `GetOfflineStatsQuery` / `CloudDriveOfflineStatsDto`；`tasks.md` 10.9 补 `GetOfflineStatsQueryHandler` |

追加验证命令：

- `rg "CloudDriveAccountScopedInput|GetVideoPlayUrlQuery|GetShareListQuery|GetUserStorageQuery|AddOfflineUrlTasksCommand|AddOfflineBtTaskCommand|ParseTorrentCommand|webapi\.115\.com/files/file" .kiro/specs/115-cloud-drive-integration/...`
- `rg "user/index_info|webapi\.115\.com/user/info|/user/index_info|sign\s*=\s*md5|NotImplementedException|RefreshDirectoryCommand|RefreshDirectoryInput|errMsg|12 种|12种|requirements\.md:454-470|file_label.*支持|gte_day.*支持|lte_day.*支持" .kiro/specs/115-cloud-drive-integration/...`
