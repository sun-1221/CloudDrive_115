# 115 cloud drive integration 视频 schema 残留对齐复查报告

## 审核快照

- **审核时间**：2026-05-04 15:18:53
- **审核轮次**：2026-05-04 residual schema 复查后的增量复查
- **官方文档基线**：`docs/115-api-docs.md`（采集到 51 个 HTTP 端点标题）
- **Specs 基线**：
  - `.kiro/specs/115-cloud-drive-integration/requirements.md`（48309 bytes）
  - `.kiro/specs/115-cloud-drive-integration/design.md`（94676 bytes）
  - `.kiro/specs/115-cloud-drive-integration/tasks.md`（92330 bytes）
  - `.kiro/specs/115-cloud-drive-frontend/requirements.md`（32754 bytes）
  - `.kiro/specs/115-cloud-drive-frontend/design.md`（65842 bytes）
  - `.kiro/specs/115-cloud-drive-frontend/tasks.md`（44467 bytes）
- **上轮审核**：`.kiro/specs/115-cloud-drive-integration/audit-history/2026-05-04-143546-residual-schema-rerun.md`，残留 schema 问题已修复
- **本轮范围**：复查未采集成功 schema 的视频播放端点、搜索 type 值域残留、任务追溯与 markdown 格式

## 审核工作量

- 端点总数 N = 51
- 本轮高风险复查端点 = 3：`/files/video`、`/files/search`、`/movies/subtitle`
- 维度数 D = 10
- 细项总数 I = 48
- 本轮实检检查点 = 3 x 48 = 144
- 额外质量检查：integration tasks 97 个叶子任务、frontend tasks 115 个叶子任务

## API 审核矩阵结果

| 编号 | 端点 | D1 | D2 | D3 | D4 | D5 | D6 | D7 | D8 | D9 | D10 | 问题摘要 |
|---|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:---:|---|
| A01 | `/files/video` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [新/修复] | 旧文案仍暗示可获取播放地址、多清晰度、4K/原画受限字段；已收敛为当前仅处理错误响应 schema，成功播放 schema 补采集前返回 `video_play_schema_not_collected` |
| A02 | `/files/search` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [新/修复] | frontend 范围排除表残留 `type=6` 具体值；已改为不绑定具体数值的专项筛选排除项 |
| A03 | `/movies/subtitle` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 仍保持 `itemSchemaStatus='not_collected'` 空态，不伪造字幕明细 |

## 本轮修复清单

| 编号 | 问题 | 修复位置 | 结果 |
|---|---|---|---|
| V-01 | `/files/video` 官方只采集 `{ state, errno, error }`，但 integration 仍有“获取播放地址与元数据”“视频播放地址获取”等积极表述 | `.kiro/specs/115-cloud-drive-integration/requirements.md:309`, `:583`; `.kiro/specs/115-cloud-drive-integration/design.md:42`, `:70`, `:1348` | 改为“视频播放端点响应”；成功播放地址 schema 未采集时只返回不可用空态 |
| V-02 | 视频 DTO 与任务预留清晰度、音轨、受限字段时未全部强调未采集前不得映射 | `.kiro/specs/115-cloud-drive-integration/design.md:898`; `.kiro/specs/115-cloud-drive-integration/tasks.md:275`, `:313`, `:545`, `:579` | 补充 `not_collected` 保护语，未采集前字段保持 `null` / 空集合 |
| V-03 | frontend 范围排除表残留 `搜索 type=6`，暗示未采集值域有具体语义 | `.kiro/specs/115-cloud-drive-frontend/requirements.md:377` | 改为“搜索特定 `type` 值专项筛选”，不绑定具体数值 |

## 官方依据

| 能力 | 官方文档位置 | 结论 |
|---|---|---|
| `/files/video` | `docs/115-api-docs.md:435-463` | 当前响应体 schema 只有 `state/errno/error`，未采集播放 URL、清晰度、音轨或 VIP 受限字段 |
| `/files/search` | `docs/115-api-docs.md:386-431` | `type` 参数存在，但样本值为空，未采集具体枚举语义 |
| `/movies/subtitle` | `docs/115-api-docs.md:495-533` | `autoload/list` 数组存在，但 `_itemShape: null`，不可展示字幕 item 字段 |

## 覆盖率验证

- API 对齐实检检查点：144 / 144（100%）
- integration tasks 叶子任务追溯：97 / 97（100%），`TRACE_OK`
- frontend tasks 叶子任务追溯：115 / 115（100%），`TRACE_OK`
- Markdown fence 验证：integration `design.md` 58 个 fence，frontend `design.md` 76 个 fence，均为偶数
- `git diff --check -- .kiro/specs/115-cloud-drive-integration .kiro/specs/115-cloud-drive-frontend`：无 whitespace error；仅 Git 提示 integration 两个 `.md` 下次触碰会 CRLF -> LF
- 旧契约关键词扫描：未发现有效回归；剩余命中为 DTO 数组类型名、否定性说明或 schema available 后的条件路径

## 历史反思

- V-01 属于上轮同类遗漏：上轮重点修复 item schema 和空间 schema，未把“仅错误响应样本”的 `/files/video` 成功路径也纳入统一 `not_collected` 规则。
- V-03 属于非主流程表格残留：正文已说明 `type` 值域未采集，但范围排除表仍保留具体 `type=6`。
- 后续审计规则：凡是官方响应只有错误 schema、`data: object` 或 `_itemShape:null`，requirements/design/tasks/测试名都必须以 `not_collected` 空态为默认，不得通过标题或任务名暗示当前可展示字段。

## 结论

本轮新增残留已修复。当前 integration spec 保持边界：backend 才访问 115 Web APIs；frontend 只调用后端 `/api/*`；未采集写接口返回 `cloud_drive_api_not_collected`；离线签名不可用返回 `offline_signature_not_available`；未采集成功/明细 schema 返回 `not_collected` 或明确 ProblemDetails，不伪造数据。
