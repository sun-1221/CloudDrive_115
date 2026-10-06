# 115 cloud drive integration 采集刷新对齐复查报告

## 审核快照

- **审核时间**：2026-05-04 15:54:41
- **审核轮次**：collector 最新采集同步后的增量复查
- **官方文档基线**：`docs/115-api-docs.md`（最新自动采集 `runId: 20260504073432-ck05g2`，保留 2026-04-18 上传链路手动补充）
- **Collector 输出**：`collector/output/runs/20260504073432-ck05g2/reports/api-docs.md`
- **Specs 基线**：`.kiro/specs/115-cloud-drive-integration/{requirements,design,tasks}.md`
- **上轮审核**：`.kiro/specs/115-cloud-drive-integration/audit-history/2026-05-04-151853-video-schema-rerun.md`
- **本轮范围**：接口文档刷新、`/files/search` 非空 item schema、`/files/history` 空数组/对象分支、`/files` item schema 回填

## 审核工作量

- 最新自动采集端点数 N = 74
- 手动上传补充端点 = 2
- 本轮重点端点 = 5：`/files`、`/files/search`、`/files/history`、`/files/video_push`、`/movies/transcode`
- 维度数 D = 10
- 细项总数 I = 48
- 本轮实检检查点 = 5 x 48 = 240

## API 审核矩阵结果

| 编号 | 端点 | D1 | D2 | D3 | D4 | D5 | D6 | D7 | D8 | D9 | D10 | 问题摘要 |
|---|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:---:|---|
| A01 | `/files` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [修复] | 文档主干刷新后补回非空列表 item schema；继续标注 `fc/type/sta/m` 值域未采集 |
| A02 | `/files/search` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [文档/修复] | 2026-05-04 raw 中 `search_value=mp4` 采集到 1 条 item，spec 从 `not_collected` 改为有限 schema available |
| A03 | `/files/history` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [文档/修复] | 自动样本为空数组，历史手动样本可能为对象；spec 改为兼容两种形态，不伪造进度字段 |
| A04 | `/files/video_push` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [文档] | 从手动低置信更新为 2026-05-04 自动 medium 样本，响应含 `request` |
| A05 | `/movies/transcode` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [文档] | 从手动低置信更新为 2026-05-04 自动低置信样本，响应含 `request` |

## 修复清单

| 编号 | 修复内容 | 位置 |
|---|---|---|
| C-01 | `docs/115-api-docs.md` 主干同步至 `20260504073432-ck05g2`，保留上传初始化与 OSS 直传手动补充 | `docs/115-api-docs.md` |
| C-02 | 补充 `/files/search` 非空 item schema，并声明 `ns` 为 HTML 片段、`type` 值域未采集 | `docs/115-api-docs.md`、integration requirements/design/tasks |
| C-03 | 前端搜索结果允许展示已采集字段，但禁止直接 `v-html` 渲染 `highlightNameRaw/ns` | frontend requirements/design/tasks |
| C-04 | `/files/history` 改为兼容空数组与历史对象分支 | `docs/115-api-docs.md`、integration requirements/design/tasks |

## 覆盖率验证

- API 对齐实检检查点：240 / 240（100%）
- integration tasks 叶子任务追溯：97 / 97（100%），`TRACE_OK`
- frontend tasks 叶子任务追溯：115 / 115（100%），`TRACE_OK`
- Markdown fence 验证：`docs/115-api-docs.md`、integration design、frontend design 均为偶数
- `git diff --check -- docs/115-api-docs.md .kiro/specs/115-cloud-drive-integration .kiro/specs/115-cloud-drive-frontend`：无 whitespace error；仅 Git 提示 integration 两个 `.md` 下次触碰会 CRLF -> LF

## 结论

本轮已将最新 collector 输出补充到接口文档，并修复搜索结果 item schema 已采集但 specs 仍按 `not_collected` 处理的问题。未采集写接口、视频播放成功 schema、回收站/字幕/分享/离线任务 item schema 仍保持保守空态或明确 ProblemDetails。
