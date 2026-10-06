# 115 cloud drive integration 残留 schema 对齐复查报告

## 审核快照

- **审核时间**：2026-05-04 14:35:46
- **审核轮次**：2026-05-04 全量复查后的残留 schema 复查与修复
- **官方文档基线**：`docs/115-api-docs.md`（采集到 51 个 HTTP 端点标题）
- **Specs 基线**：
  - `.kiro/specs/115-cloud-drive-integration/requirements.md`（48088 bytes）
  - `.kiro/specs/115-cloud-drive-integration/design.md`（93981 bytes）
  - `.kiro/specs/115-cloud-drive-integration/tasks.md`（92017 bytes）
  - `.kiro/specs/115-cloud-drive-frontend/requirements.md`（32689 bytes）
  - `.kiro/specs/115-cloud-drive-frontend/design.md`（65667 bytes）
  - `.kiro/specs/115-cloud-drive-frontend/tasks.md`（44287 bytes）
- **上轮审核**：`.kiro/specs/115-cloud-drive-integration/audit-history/2026-05-04-133757-fix-verification.md`，已修复 `/rb`、分页硬上限、`fc`、runtime 域名、quota 端点和任务追溯问题
- **本轮范围**：复查上轮修复后残留的未采集 item/success schema、空间详情 schema、前端搜索消费契约、任务追溯

## 审核工作量

- 端点总数 N = 51
- 本轮高风险复查端点 = 8：`/files`、`/files/search`、`/files/video`、`/movies/subtitle`、`/rb`、`/share/slist|send|receive`、`/web/lixian/?ac=task_lists`、`/files/index_info`
- 维度数 D = 10
- 细项总数 I = 48
- 本轮实检检查点 = 8 × 48 = 384
- 额外质量检查：integration tasks 97 个叶子任务、frontend tasks 115 个叶子任务

## API 审核矩阵结果

| 编号 | 端点 | D1 | D2 | D3 | D4 | D5 | D6 | D7 | D8 | D9 | D10 | 问题摘要 |
|---|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:---:|---|
| A01 | `/files` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [修复] | `type` 值域未采集，已从 1-7 官方枚举降级为透传参数；`fc/sta` 等值域仍要求样本验证后派生 |
| A02 | `/files/search` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [修复] | item schema 未采集，已统一返回 `CloudDriveSearchListDto { Items=[], ItemSchemaStatus="not_collected" }` |
| A03 | `/files/video` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [修复] | 成功播放 schema 未采集，继续返回 `video_play_schema_not_collected` / `PlaybackSchemaStatus="not_collected"` |
| A04 | `/movies/subtitle` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [修复] | `autoload/list` item shape 为 null，字幕 DTO 已收敛为空占位 |
| A05 | `/rb` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [修复] | 回收站 item DTO 已收敛为空占位；只映射顶层 `count/rb_pass/offset/page_size/order/is_asc` |
| A06 | `/share/slist|send|receive` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [修复] | 分享 item/success list schema 未采集，分享 item DTO 已收敛为空占位 |
| A07 | `/web/lixian/?ac=task_lists` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [修复] | `tasks: object` item schema 未采集，任务 item DTO 已保持空占位 |
| A08 | `/files/index_info` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [修复] | `data: object` 内部字段未采集，`CloudDriveSpaceInfoDto` 已收敛为 `SchemaStatus` 占位，不伪造空间总量字段 |

## 本轮修复清单

| 编号 | 问题 | 修复位置 | 结果 |
|---|---|---|---|
| H-05 | `/files/search` 未采集 item schema，但 frontend 仍以 `CloudDriveFileDto[]` 展示 | frontend `requirements.md`、`design.md`、`tasks.md` | 改为消费 `CloudDriveSearchListDto`；`itemSchemaStatus='not_collected'` 时显示搜索明细不可用 |
| H-06 | `/movies/subtitle` / `/rb` / `/share/*` 的占位 DTO 仍列出未采集字段 | frontend `design.md`、integration `design.md` | 占位 item DTO 仅保留 `schemaStatus`，字段待 docs 补采集后再添加 |
| H-07 | `/files/index_info` 空间详情只有 `data: object`，但 specs 仍暗示可映射总量字段 | frontend `requirements.md`、`design.md`、`tasks.md`；integration `design.md` | 空间 DTO 仅保留 `schemaStatus`；前端显示“空间明细暂不可用” |
| H-08 | `/files` 与 `/files/search` 的 `type` 值域未采集，前端/后端曾写死 1-7 / 1-5 语义 | integration `requirements.md`；frontend `requirements.md`、`design.md` | 改为以后端 OpenAPI/采集结果为准，当前不硬编码枚举语义 |
| Q-03 | integration 检查点叶子任务缺 `_需求:` + `_设计:` 双向追溯 | integration `tasks.md` | 3、5、11、14 检查点均补齐追溯；脚本验证 `TRACE_OK` |

## 官方依据

| 能力 | 官方文档位置 | 结论 |
|---|---|---|
| `/files/search` | `docs/115-api-docs.md:386-431` | 响应仅采集顶层字段与 `data: object`，没有 item schema |
| `/files/video` | `docs/115-api-docs.md:435-463` | 当前只采集错误响应 `{ state, errno, error }` |
| `/movies/subtitle` | `docs/115-api-docs.md:495-533` | `autoload/list` 为数组，但 `_itemShape: null` |
| `/rb` | `docs/115-api-docs.md:535-565` | 仅采集回收站顶层字段，未采集项目明细 |
| `/share/slist` | `docs/115-api-docs.md:633-668` | `list` item shape 为 null |
| `/web/lixian/?ac=task_lists` | `docs/115-api-docs.md:1213-1259` | `tasks` 仅为 object，未采集 task item schema |
| `/files/index_info` | `docs/115-api-docs.md:336-384` | `data` 仅为 object，未采集空间详情字段 |

## 覆盖率验证

- API 对齐实检检查点：384 / 384（100%）
- integration tasks 叶子任务追溯：97 / 97（100%）
- frontend tasks 叶子任务追溯：115 / 115（100%）
- 旧契约关键词扫描：未发现有效回归；剩余命中均为 DTO 类型名或“不得将 `limit=1150` / `offset+limit≤10000` 当官方硬上限”的否定性说明
- Markdown fence 验证：integration `design.md` 58 个 fence，frontend `design.md` 76 个 fence，均为偶数
- `git diff --check`：无 whitespace error；仅 Git 提示 integration 两个 `.md` 下次触碰会 CRLF→LF

## 历史反思

- H-05/H-06/H-07 属于上轮“仅修复回收站 item schema”时的同类遗漏：审核注意力集中在 `/rb`，没有把同样 `_itemShape:null` 或 `data: object` 的端点统一归类。
- H-08 属于参数值域细项遗漏：确认了参数存在，但没有继续追问 docs 是否采集到枚举语义。
- 后续审计规则：凡是官方 schema 出现 `object`、`_itemShape:null`、仅错误响应样本，spec 必须默认 `not_collected`，不得在 DTO 中预铺可展示字段。

## 结论

本轮新增残留已修复。当前 integration spec 继续保持边界：backend 才访问 115 Web APIs；frontend 不直连 115，只消费后端 `/api/*`；未采集写接口返回 `cloud_drive_api_not_collected`，离线签名未提供时返回 `offline_signature_not_available`，未采集明细 schema 返回 `not_collected` 空态而非伪造数据。
