# 115 cloud drive integration 全量 spec API 对齐复查报告

## 审核快照

- **审核时间**：2026-05-04 16:44:30 +08:00
- **审核轮次**：collector 刷新后的全量合理性支撑复查
- **官方文档基线**：`docs/115-api-docs.md`（最新自动采集 `20260504073432-ck05g2` + 2026-04-18 上传链路手动补充）
- **Specs 基线**：`.kiro/specs/115-cloud-drive-integration/{requirements,design,tasks}.md`
- **本轮范围**：API 证据边界复核，不重复审查全部 76 个端点的每个字段；重点核对 `/files`、`/files/search`、`/files/history`、`/files/video`、`/movies/subtitle`、`/files/video_push`、`/movies/transcode`、上传链路、未采集写接口边界

## 审核工作量

- 文档端点总数 N = 76（74 自动 + 2 上传手动补充）
- 本轮重点端点 = 9
- 维度数 D = 10
- 细项总数 I = 48
- 重点检查点 = 9 x 48 = 432

## API 对齐结论

API 对齐层面总体 **可接受但需修正任务/代码基线状态**：

- `/files/search` 已按 2026-05-04 非空样本更新为有限 item schema available，spec 未继续误标为完全未采集。
- `/files/history` 已兼容 2026-05-04 空数组和 2026-04-18 对象分支，未伪造历史字段。
- `/files/video` 成功播放 schema 未采集的边界在 integration requirements/design 中清楚保留。
- 未采集写接口仍以 `cloud_drive_api_not_collected` 表达，离线签名缺失仍以 `offline_signature_not_available` 表达。
- 主要风险来自任务状态和现有代码基线，不是 API 文档字段本身。

## API 审核矩阵结果（重点端点）

| 编号 | 端点 | D1 | D2 | D3 | D4 | D5 | D6 | D7 | D8 | D9 | D10 | 问题摘要 |
|---|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:---:|---|
| A01 | `GET /files` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ⚠️ | [复查] | spec 已声明 `fc/type/sta/m` 不得推断；tasks 局部仍有 `fc → IsDirectory` 推断表述 |
| A02 | `GET /files/search` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [修复] | docs/spec 已更新非空 item schema 与 `ns` 安全边界 |
| A03 | `GET/POST /files/history` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ⚠️ | [修复/风险] | spec 已对齐；当前代码基线仍使用非空 DTO 字段和 0 fallback |
| A04 | `GET /files/video` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ⚠️ | [复查] | spec 边界正确；当前代码基线尚缺 `PlaybackSchemaStatus` |
| A05 | `GET /movies/subtitle` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 顶层数组已采集，item schema 仍 not_collected，frontend 空态合理 |
| A06 | `GET /files/video_push` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 自动 medium 样本已纳入主动转码说明 |
| A07 | `GET /movies/transcode` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 低置信备选路径已保持保守 |
| A08 | 上传初始化 + OSS PostObject | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 2026-04-18 手动高置信链路保留，frontend 应视为可实现上传 |
| A09 | 未采集写接口 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 没有重新写入猜测型 115 path；保持 ProblemDetails |

## 对齐问题

### A-01. 中优先级：`/files` 字段推断在 tasks 中有残留

- **API 依据**：`docs/115-api-docs.md:174`
- **spec 正向依据**：`.kiro/specs/115-cloud-drive-integration/requirements.md:112`
- **残留位置**：`.kiro/specs/115-cloud-drive-integration/tasks.md:425`
- **差异说明**：API 文档与 requirements 均要求不得把 `fc/type/sta/m` 推断为官方枚举；tasks 中仍有“`fc` 值域转换 → `IsDirectory`”表述。

### A-02. 高优先级：API 对齐后的 DTO 契约尚未反映到当前代码基线

- **搜索依据**：`docs/115-api-docs.md:471-560`；spec 位置 `.kiro/specs/115-cloud-drive-integration/design.md:988-1028`
- **播放历史依据**：`docs/115-api-docs.md:323-388`；spec 位置 `.kiro/specs/115-cloud-drive-integration/design.md:1283-1291`
- **视频依据**：`docs/115-api-docs.md:562-590`；spec 位置 `.kiro/specs/115-cloud-drive-integration/design.md:1231-1244`
- **差异说明**：API/spec 已更新为更保守契约，但当前代码基线仍是旧 DTO/旧返回包装。该问题应进入后续实现或任务状态修复。

## 覆盖率验证

- 本轮重点端点检查点：432 / 432
- integration 叶子任务追溯机械校验：`leaf=97 missing=0`
- frontend 叶子任务追溯机械校验：`leaf=115 missing=0`
- `git diff --check`：无 whitespace error；仅 Git CRLF warning
