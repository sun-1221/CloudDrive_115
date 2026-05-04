# 115 cloud drive frontend 采集刷新契约质量复查报告

## 审核快照

- **审核时间**：2026-05-04 15:54:41
- **审核轮次**：collector 最新采集同步后的 frontend 契约复查
- **Specs 基线**：`.kiro/specs/115-cloud-drive-frontend/{requirements,design,tasks}.md`
- **接口依据**：`docs/115-api-docs.md`（2026-05-04 最新自动采集 + 2026-04-18 上传链路手动补充）
- **上轮质量基线**：`.kiro/specs/115-cloud-drive-frontend/quality-history/2026-05-04-151853-video-schema-quality.md`
- **本轮范围**：搜索 DTO、SearchStore、搜索结果视图、安全渲染边界

## Specs 质量审核矩阵

| 编号 | 对象 | 类型 | Q4 覆盖 | Q7 追溯 | Q9 一致 | Q10 状态 | Q12 依据 | 问题摘要 |
|---|---|---|:--:|:--:|:--:|:---:|:--:|---|
| R5 | 搜索结果 | 需求 | ✅ | - | ✅ | [修复] | ✅ | 搜索 item schema 已采集，前端从明细不可用空态改为展示已采集字段 |
| D§SearchStore | 搜索状态设计 | 设计 | ✅ | - | ✅ | [修复] | ✅ | `searchList/results` 与 integration DTO 对齐，保留 `not_collected` fallback |
| D§SearchDTO | 搜索 DTO | 设计 | ✅ | - | ✅ | [修复] | ✅ | 增加 `CloudDriveSearchResultDto` 已采集字段与 `highlightNameRaw` 安全说明 |
| T15.2 | 搜索结果视图 | 任务 | - | ✅ | ✅ | [修复] | ✅ | 任务改为展示文件名、大小、目录显示名等已采集字段，并禁止直接渲染 HTML 片段 |

## 验证命令

- integration tasks 叶子任务追溯：`TRACE_OK leaf=97`
- frontend tasks 叶子任务追溯：`TRACE_OK leaf=115`
- Markdown fence 验证：`docs/115-api-docs.md`、integration design、frontend design 均为偶数
- `git diff --check -- docs/115-api-docs.md .kiro/specs/115-cloud-drive-integration .kiro/specs/115-cloud-drive-frontend`：无 whitespace error；仅 Git CRLF 提示

## 结论

frontend spec 已跟随最新采集结果更新搜索结果契约：正常搜索结果可展示已采集字段，异常或 schema 退化时仍保留 `itemSchemaStatus='not_collected'` 空态；`ns/highlightNameRaw` 不得直接作为可信 HTML 渲染。
