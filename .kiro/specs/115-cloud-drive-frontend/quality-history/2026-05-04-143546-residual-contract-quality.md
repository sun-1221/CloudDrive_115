# 115 cloud drive frontend Specs 残留契约质量复查报告

## 审核快照

- **审核时间**：2026-05-04 14:35:46
- **审核轮次**：2026-05-04 integration 残留 schema 修复后的 frontend 质量复查
- **Specs 基线**：
  - `.kiro/specs/115-cloud-drive-frontend/requirements.md`（32689 bytes）
  - `.kiro/specs/115-cloud-drive-frontend/design.md`（65667 bytes）
  - `.kiro/specs/115-cloud-drive-frontend/tasks.md`（44287 bytes）
  - `.kiro/specs/115-cloud-drive-integration/requirements.md`
  - `.kiro/specs/115-cloud-drive-integration/design.md`
  - `.kiro/specs/115-cloud-drive-integration/tasks.md`
- **项目规范依据**：`.windsurf/rules/spec-format.md`
- **上轮质量基线**：`.kiro/specs/115-cloud-drive-frontend/quality-history/2026-05-04-133757-fix-verification.md`
- **本轮范围**：Q4 设计覆盖、Q7 任务追溯、Q9 前后端术语/DTO 一致性、Q12 指定依据（`docs/115-api-docs.md`）影响的前端消费契约

## 审核工作量

- 需求条目数 R = 31（前后端 requirements 标题合计）
- 设计组件数 D = 180（前后端 design 三级/四级标题合计）
- 任务数 T = 140（frontend tasks），叶子任务 115
- 维度数 Q = 12
- 本轮重点检查点：搜索、文件筛选、视频播放、字幕、回收站、云下载、分享、用户空间详情

## Specs 质量审核矩阵

| 编号 | 对象 | 类型 | Q4 覆盖 | Q7 追溯 | Q9 一致 | Q10 状态 | Q12 依据 | 问题摘要 |
|---|---|---|:--:|:--:|:--:|:---:|:--:|---|
| R5 | 搜索 | 需求 | ✅ | - | ✅ | [修复] | ✅ | 搜索结果从 `CloudDriveFileDto[]` 改为 `CloudDriveSearchListDto`；未采集 item schema 时显示明细不可用 |
| D§SearchStore | 搜索 Store | 设计 | ✅ | - | ✅ | [修复] | ✅ | 状态新增 `searchList`，`results` 仅在 item schema available 时展示 |
| T15.2 | 搜索结果视图 | 任务 | - | ✅ | ✅ | [修复] | ✅ | 不再要求每项显示伪造路径；schema 未采集时显示空态 |
| R2/R5 | 文件类型筛选 | 需求 | ✅ | - | ✅ | [修复] | ✅ | `type` 值域未采集，前端不硬编码 1=文档/图片等枚举 |
| D§DTO | 占位 item DTO | 设计 | ✅ | - | ✅ | [修复] | ✅ | 字幕/回收站/云下载/分享占位 DTO 仅保留 `schemaStatus` |
| R17 | 用户空间详情 | 需求 | ✅ | - | ✅ | [修复] | ✅ | `schemaStatus='not_collected'` 时显示“空间明细暂不可用”，不构造总量/已用/分类占比 |
| T8.1/T11.2/T25.5 | 空间摘要/详情 | 任务 | - | ✅ | ✅ | [修复] | ✅ | `spaceSummary` 仅在 schema available 时写入；Sidebar/StorageDetailsCard 支持 unavailable 空态 |
| TAll | 任务追溯 | 任务 | - | ✅ | ✅ | [修复] | N/A | frontend 115 个叶子任务全部包含 `_需求:` + `_设计:` |

## 修复结果

| 编号 | 问题 | 状态 | 修复摘要 |
|---|---|---|---|
| FQ-06 | 搜索前端仍把后端结果当 `CloudDriveFileDto[]` | ✅ 已修复 | `SearchStore` 改为 `CloudDriveSearchListDto`；`not_collected` 显示“搜索结果明细暂不可用” |
| FQ-07 | 搜索/文件类型筛选写死未采集枚举 | ✅ 已修复 | `FileFilter` / `SearchFilters` 改为 `string | number` 透传；文案明确值域以后端 OpenAPI/补采集为准 |
| FQ-08 | 字幕/回收站/云下载/分享占位 DTO 暗示可展示字段 | ✅ 已修复 | 占位 item DTO 仅保留 `schemaStatus`，字段待 docs 补采集后再添加 |
| FQ-09 | 空间详情在 `data: object` 未采集时仍暗示可展示容量字段 | ✅ 已修复 | `CloudDriveStorageDetailsDto` 仅保留 `schemaStatus`；AuthStore/Sidebar/UserCenter 都支持空间明细不可用空态 |
| FQ-10 | integration 检查点追溯影响前后端执行一致性 | ✅ 已修复 | integration 检查点 3/5/11/14 补齐追溯，前后端 tasks 校验均 `TRACE_OK` |

## 验证命令

- 叶子任务追溯脚本：
  - `.kiro/specs/115-cloud-drive-integration/tasks.md`：`TRACE_OK`
  - `.kiro/specs/115-cloud-drive-frontend/tasks.md`：`TRACE_OK`
- 关键词反向扫描：未发现有效旧契约残留；剩余命中为 `CloudDriveOfflineTaskDto[]` / `CloudDriveShareDto[]` 类型数组或否定性说明。
- Markdown fence 验证：integration `design.md` 58 个 fence，frontend `design.md` 76 个 fence，均通过。
- `git diff --check -- .kiro/specs/115-cloud-drive-integration .kiro/specs/115-cloud-drive-frontend`：无 whitespace error；Git 仅提示 integration 两个 `.md` 将在下次触碰时 CRLF→LF。

## 结论

frontend spec 已重新对齐 integration contract：前端只调用后端 `/api/*`，不直连 115；对搜索、字幕、回收站、云下载、分享、空间详情等未采集明细 schema，一律展示明确的不可用空态，不伪造字段、不硬编码未采集枚举。
