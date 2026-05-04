# 115 cloud drive integration 修复验证报告

## 审核快照

- **审核时间**：2026-05-04 13:37:57
- **审核轮次**：2026-05-04 全量复审问题修复验证
- **官方文档基线**：`docs/115-api-docs.md`
- **Specs 基线**：
  - `.kiro/specs/115-cloud-drive-integration/requirements.md`
  - `.kiro/specs/115-cloud-drive-integration/design.md`
  - `.kiro/specs/115-cloud-drive-integration/tasks.md`
  - `.kiro/specs/115-cloud-drive-frontend/requirements.md`
  - `.kiro/specs/115-cloud-drive-frontend/design.md`
  - `.kiro/specs/115-cloud-drive-frontend/tasks.md`
- **上轮审核**：`.kiro/specs/115-cloud-drive-integration/audit-history/2026-05-04-115009-full-rerun.md`
- **本轮范围**：只复查并修复上轮 H-01、H-02、H-03、H-04、M-01、Q-01，以及修复过程中发现的前后端回收站 DTO 命名不一致。

## 修复结果

| 编号 | 上轮问题 | 状态 | 修复摘要 |
|---|---|---|---|
| H-01 | `/rb` 回收站响应字段与 docs 不对齐 | ✅ 已修复 | integration requirements/design/tasks 改为只映射 `/rb` 顶层字段；item schema 未采集时返回 `CloudDriveRecycleListDto { items = [], itemSchemaStatus = "not_collected" }`；不再定义 `tid/status/type` 语义 |
| H-02 | `/files` 与 `/files/search` 推断分页硬上限被当成硬拦截 | ✅ 已修复 | 移除 `limit=1150`、`offset+limit≤10000` 作为 115 官方硬限制的验收；只允许系统自有 `CloudDrive:MaxPageSize` 保护 |
| H-03 | `fc` 文件/目录值域跨 spec 冲突 | ✅ 已修复 | 改为 `fc → rawCategoryCode`；`category/isDirectory/uploadStatus` 仅在真实网页端样本验证后派生，未知返回 `category='unknown'` / `isDirectory=null` |
| H-04 | `passportapi.115.com` 被 requirements 排除但 design 列为运行域名 | ✅ 已修复 | runtime 域名分组改为 6 组；移除 `passportapi.115.com` 运行域名；登录态探测只使用 `GET webapi.115.com/files?cid=0&limit=1` |
| M-01 | 云下载 quota 任务保留“等效端点”松口径 | ✅ 已修复 | 固定为 `GET 115.com/web/lixian/?ct=lixian&ac=get_quota_package_info` |
| Q-01 | 11 个 integration 属性测试叶子任务缺 `_设计:` | ✅ 已修复 | 11 个属性测试任务均补齐 `_需求:` + `_设计:` 双向追溯 |
| Q-02 | frontend 叶子任务 6.4 追溯校验异常 | ✅ 已修复 | `ErrorHandlerService` 保留完整 `_设计: §ErrorHandlerService, §7. 错误处理策略_` 追溯；全量 leaf task 校验通过 |
| C-01 | 前后端回收站 item DTO 命名不一致 | ✅ 已修复 | frontend 统一为 `CloudDriveRecycleBinItemDto`，与 integration `CloudDriveRecycleListDto.items` 契约一致 |

## 官方依据

| 能力 | 官方文档位置 | 对齐结论 |
|---|---|---|
| 文件列表 `/files` | `docs/115-api-docs.md:48-128` | 文档采集 `offset/limit`，未采集 `limit=1150` 作为网页端硬上限 |
| 文件搜索 `/files/search` | `docs/115-api-docs.md:386-431` | 文档采集 `offset/limit/search_value/date/aid/cid/pick_code/type/count_folders/source/format`，未采集 `offset+limit≤10000` |
| 回收站 `/rb` | `docs/115-api-docs.md:535-565` | 文档仅采集顶层 `count/rb_pass/state/error/offset/page_size/order/is_asc`，未采集 item schema |
| 云下载 quota | `docs/115-api-docs.md:1183-1189` | 已采集精确端点 `ct=lixian&ac=get_quota_package_info` |
| passportapi | `docs/115-api-docs.md:1934-1949` | 属于账号安全预警端点，已在 cloud drive 范围中排除 |

## 验证命令

- `rg` 反向扫描旧问题关键词：未发现会重新引入旧语义的命中；仅保留“不得将 1150/10000 当官方限制”等否定性说明。
- integration / frontend `tasks.md` 叶子任务追溯脚本：`TRACE_OK`。
- `git diff --check`：无 whitespace error；PowerShell 提示部分 `.md` 下次 Git 触碰时会从 CRLF 归一为 LF。

## 结论

上轮阻塞实现的问题 H-01、H-02、H-03、Q-01 已修复；H-04、M-01、C-01 也已收敛。当前 spec 明确保持架构边界：frontend 只访问后端 `/api/*`，backend integration 根据 `docs/115-api-docs.md` 访问 115 网页端 API。未采集的 115 写接口继续返回 `cloud_drive_api_not_collected`，不猜测真实路径。
