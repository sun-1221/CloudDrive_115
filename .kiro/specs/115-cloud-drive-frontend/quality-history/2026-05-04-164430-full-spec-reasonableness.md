# 115 cloud drive frontend 全量 spec 合理性审核报告

## 审核快照

- **审核时间**：2026-05-04 16:44:30 +08:00
- **审核轮次**：collector 刷新后的 frontend 全量合理性复查
- **Specs 基线**：`.kiro/specs/115-cloud-drive-frontend/{requirements,design,tasks}.md`
- **后端契约依据**：`.kiro/specs/115-cloud-drive-integration/*` 与 `docs/115-api-docs.md`
- **项目规范依据**：`.windsurf/rules/frontend-development-standards.md`、`.windsurf/rules/spec-format.md`
- **上轮质量基线**：`.kiro/specs/115-cloud-drive-frontend/quality-history/latest.md`
- **本轮范围**：requirements / design / tasks 全量合理性、前后端契约边界、搜索与未采集 schema 空态

## 审核工作量

- 需求模块数 R = 19（需求 10 已删除，18 个有效模块）
- 设计三级标题数 D = 41
- 叶子任务数 T = 115
- 任务追溯机械校验：115 / 115 叶子任务均包含 `_需求:` 与 `_设计:`

## 结论

frontend spec 的大方向合理：前端不直连 115，只消费后端 `/api/*`；搜索结果已跟随 2026-05-04 采集 schema 更新；`highlightNameRaw/ns` 禁止直接 `v-html`；视频播放、字幕、回收站、分享、云下载等未采集明细都有空态约束。

需要修的主要是局部文案和任务状态：上传已经是后端代理上传可实现能力，但任务仍写成占位；云下载自动刷新在 requirements 中无条件触发，和 design 的 schema available 条件不一致；视频/字幕“已采集”的说法需要更精确，避免误读为成功播放 URL 或字幕 item schema 已采集。

## Specs 质量审核矩阵（问题对象汇总）

| 编号 | 对象 | 类型 | Q4 覆盖 | Q7 追溯 | Q8 颗粒 | Q9 一致 | Q10 状态 | Q12 依据 | 问题摘要 |
|---|---|---|:--:|:--:|:--:|:--:|:---:|:--:|---|
| FQ-01 | UploadZone / 文件操作任务 | 任务 | ✅ | ✅ | ✅ | ❌ | [新] | ✅ | 上传在 requirements 中已可用，但任务仍标“占位/等待后端就绪” |
| FQ-02 | 云下载自动刷新 | 需求/设计 | ✅ | - | ✅ | ❌ | [新] | ✅ | requirements 无条件 10 秒刷新，design 要求 schema available 且有进行中任务才刷新 |
| FQ-03 | 视频/字幕采集措辞 | 设计/任务 | ✅ | ✅ | ✅ | ⚠️ | [新] | ✅ | “视频播放、字幕已采集”容易被误读为成功 schema/item schema 已采集 |
| FQ-04 | 搜索安全与空态 | 需求/设计/任务 | ✅ | ✅ | ✅ | ✅ | [修复] | ✅ | 搜索 item schema 和 `highlightNameRaw/ns` 安全边界已合理 |

## 关键问题

### FQ-01. 中优先级：上传任务状态与 requirements 不一致

- **requirements 位置**：`.kiro/specs/115-cloud-drive-frontend/requirements.md:111-116`
- **tasks 位置**：`.kiro/specs/115-cloud-drive-frontend/tasks.md:489-495`
- **差异说明**：requirements 明确上传链路已采集，前端调用 `POST /api/files/upload`；tasks 中 `UploadZone（占位）` 和“等待后端 `POST /api/files/upload` 就绪”仍是旧口径。
- **影响**：实现者可能跳过上传 UI，或把可实现功能当作占位。
- **建议**：将 UploadZone 任务改为正式实现项；只把下载、文件增删改、移动、复制、星标等仍未采集能力保留为 ProblemDetails fallback。

### FQ-02. 中优先级：云下载自动刷新条件不一致

- **requirements 位置**：`.kiro/specs/115-cloud-drive-frontend/requirements.md:293-303`
- **design 位置**：`.kiro/specs/115-cloud-drive-frontend/design.md:560-572`
- **差异说明**：requirements 要求“任务进行中时自动每 10 秒刷新列表”，但当前云下载 task item schema 未采集，前端无法可靠判断“进行中”；design 已正确收敛为仅 `itemSchemaStatus='available'` 且存在进行中任务时轮询。
- **建议**：requirements 同步补充条件；schema 未采集时仅允许手动刷新或低风险的页面级刷新，不基于伪造状态轮询。

### FQ-03. 低到中优先级：视频/字幕“已采集”措辞容易过度承诺

- **位置**：
  - `.kiro/specs/115-cloud-drive-frontend/design.md:1424`
  - `.kiro/specs/115-cloud-drive-frontend/tasks.md:868`
- **差异说明**：当前确实采集了 `/files/video` 错误 schema、`/movies/subtitle` 顶层空数组、播放历史读写端点；但成功播放 URL/清晰度/VIP 字段和字幕 item schema 未采集。
- **建议**：改成“视频播放端点错误 schema、字幕顶层数组、播放历史读写端点已采集；成功播放/字幕 item schema 未采集时按 not_collected 空态处理”。

## 通过项

- 搜索结果展示已从“明细不可用”调整为有限 schema available，且保留 `not_collected` fallback。
- `highlightNameRaw/ns` 明确禁止直接 `v-html`，符合安全边界。
- 回收站、分享、云下载任务、字幕等未采集明细均要求空态，不伪造项目字段。
- 前端只通过后端 `/api/*`，不处理 Cookie、不生成 115 签名、不直连 115。

## 验证记录

- frontend 叶子任务追溯机械校验：`leaf=115 missing=0`
- `git diff --check -- docs/115-api-docs.md .kiro/specs/115-cloud-drive-integration .kiro/specs/115-cloud-drive-frontend`：无 whitespace error；仅 Git 提示 integration `design.md` / `tasks.md` 下次触碰会 CRLF -> LF

## 建议修复顺序

1. 先把上传任务从占位改为正式实现。
2. 同步修正云下载自动刷新条件。
3. 精确化“视频/字幕已采集”的措辞，避免把错误 schema / 顶层空数组误读成成功明细 schema。
