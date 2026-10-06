# 115 cloud drive integration 全量 spec 合理性审核报告

## 审核快照

- **审核时间**：2026-05-04 16:44:30 +08:00
- **审核轮次**：2026-05-04 collector 刷新后的全量合理性复查
- **Specs 基线**：`.kiro/specs/115-cloud-drive-integration/{requirements,design,tasks}.md`
- **API 依据**：`docs/115-api-docs.md`（2026-05-04 自动采集 `20260504073432-ck05g2` + 2026-04-18 上传链路手动补充，共 76 个唯一接口）
- **项目规范依据**：`.windsurf/rules/` 下 13 个规则文件；重点引用 `spec-format.md`、`ddd-layer-architecture.md`、`cqrs-application-patterns.md`
- **上轮质量基线**：`.kiro/specs/115-cloud-drive-integration/quality-history/latest.md`
- **本轮范围**：requirements / design / tasks 全量合理性、任务追溯语义、设计与当前代码基线轻量对齐、关键 API 证据边界

## 审核工作量

- 需求条目数 R = 31（需求 1-30 + 13a）
- 设计三级标题数 D = 33
- 叶子任务数 T = 97
- 维度数 Q = 12
- 细项总数 I = 49
- 任务追溯机械校验：97 / 97 叶子任务均包含 `_需求:` 与 `_设计:`

## 结论

当前 spec 的 API 证据边界比上一轮明显更合理：搜索 item schema、播放历史空数组/对象分支、视频成功 schema 未采集、未采集写接口返回 ProblemDetails 等关键约束基本收敛。

但从“可以直接作为执行计划继续实现”的角度，结论是 **NeedFix**。主要阻塞不是 API 文档本身，而是 `tasks.md` 的需求编号语义漂移，以及 `design.md` 更新后的 DTO/状态契约与现有代码基线不一致。若继续按当前 `[x]` 任务状态推进，后续实现者容易以为搜索、视频播放历史、视频播放 schema 已完成到新契约。

## Specs 质量审核矩阵（问题对象汇总）

| 编号 | 对象 | 类型 | Q4 覆盖 | Q7 追溯 | Q8 颗粒 | Q9 一致 | Q10 状态 | Q11 基线 | Q12 依据 | 问题摘要 |
|---|---|---|:--:|:--:|:--:|:--:|:---:|:--:|:--:|---|
| T-Trace | 任务需求编号语义 | 任务 | ✅ | ❌ | ✅ | ❌ | [新] | - | ✅ | `_需求:` 标记存在，但多处引用到错误需求编号 |
| D/T-Search | 搜索 DTO 与任务状态 | 设计/任务 | ✅ | ✅ | ✅ | ⚠️ | [新] | ❌ | ✅ | design 要求 `CloudDriveSearchListDto`，当前代码仍为 `PagedResultDto<CloudDriveSearchResultDto>` |
| D/T-Video | 视频播放与播放历史 DTO | 设计/任务 | ✅ | ✅ | ✅ | ⚠️ | [新] | ❌ | ✅ | design 已改为 `PlaybackSchemaStatus` / nullable history，当前代码仍按旧形态 |
| D-Refs | 设计内旧编号/推断表述 | 设计 | ✅ | - | ✅ | ❌ | [新] | ⚠️ | ✅ | 局部说明仍引用旧需求编号或暗示未采集字段可直接推断 |
| R20/R23 | 云下载读取、用户中心聚合 | 需求 | ✅ | - | ❌ | ✅ | [遗留] | - | ✅ | 需求颗粒度仍偏粗，但不阻塞 API 安全边界 |

## 关键问题

### P1. 高优先级：任务追溯语义漂移

- **位置**：
  - `.kiro/specs/115-cloud-drive-integration/tasks.md:429-433`：新建文件夹任务追溯 `_需求: 7, 27_`，但需求 7 是上传，需求 9 才是新建文件夹
  - `.kiro/specs/115-cloud-drive-integration/tasks.md:455-460`：下载任务追溯 `_需求: 6_`，但需求 6 是文件详情，需求 8 才是文件下载
  - `.kiro/specs/115-cloud-drive-integration/tasks.md:462-478`：移动/复制/删除分别追溯到 8/9/10，当前应对应 10/11/12；其中 `_需求: 21_` 被用作后台任务，但长耗时任务实际是需求 28
  - `.kiro/specs/115-cloud-drive-integration/tasks.md:682-691`：Preview/Documents/Videos Controller 追溯到 14/16/15，与当前图片预览 15、视频 16/17、文档预览 19 不一致
  - `.kiro/specs/115-cloud-drive-integration/tasks.md:750-767`：缓存/限流/装配验证中出现需求 17、21 等无关引用
- **依据**：`.windsurf/rules/spec-format.md` 要求追溯引用真实存在且语义正确的需求编号。
- **影响**：机械校验会通过，但实现者点击追溯时会读到错误需求，导致实现范围和测试范围偏移。
- **建议**：批量按当前 `requirements.md` 重新校正 `_需求:`，尤其把“后台任务”统一追溯到需求 28，而不是需求 21。

### P2. 高优先级：搜索契约与当前代码基线不一致

- **spec 位置**：`.kiro/specs/115-cloud-drive-integration/design.md:988-1028`
- **代码基线**：
  - `Server/src/Application/CloudDrive/Files/SearchFilesQuery.cs:11-12` 仍返回 `IQuery<PagedResultDto<CloudDriveSearchResultDto>>`
  - `Server/src/Application/CloudDrive/Dtos/CloudDriveSearchResultDto.cs:6-16` 仅继承 `CloudDriveFileDto` 并含 `ParentId/Path`
  - 当前没有 `CloudDriveSearchListDto`
- **差异说明**：spec 已要求 `CloudDriveSearchListDto.ItemSchemaStatus = "available"`、`HighlightNameRaw`、`DirectoryName`、`SchemaStatus` 等新字段；当前代码基线尚未体现。
- **影响**：若继续按 `[x]` 任务状态推进，会误判搜索契约已完成。
- **建议**：要么先把任务状态改回未完成并补实现任务，要么先实现 DTO/Handler/Mapping 后再保留 `[x]`。

### P3. 高优先级：视频播放与播放历史契约与当前代码基线不一致

- **spec 位置**：
  - `.kiro/specs/115-cloud-drive-integration/design.md:1231-1244`
  - `.kiro/specs/115-cloud-drive-integration/design.md:1283-1291`
- **代码基线**：
  - `Server/src/Application/CloudDrive/Dtos/CloudDriveVideoPlayDto.cs:11-21` 默认 `Status = Ready`，没有 `PlaybackSchemaStatus`
  - `Server/src/Application/CloudDrive/Dtos/CloudDriveVideoPlayProgressDto.cs:11-31` 的 `FileId/FileName/Hash/PlayedSeconds` 仍为非空/非可空字段，且没有 `SchemaStatus`
  - `Server/src/Application/CloudDrive/Video/CloudDriveVideoSupport.cs:184-193` 空历史会落为 `string.Empty` 和 `0`
- **差异说明**：spec 要求成功播放 schema 未采集前返回 `video_play_schema_not_collected` / `PlaybackSchemaStatus = "not_collected"`；播放历史空数组时不得伪造字段。
- **影响**：这会直接影响前端空态和测试预期。
- **建议**：在继续实现前，将 DTO 与 mapping 任务恢复为待办，或同步实现 nullable/history schema 与 playback status。

### P4. 中优先级：局部设计说明仍有旧编号和推断口径

- **位置**：
  - `.kiro/specs/115-cloud-drive-integration/design.md:266`：说明中新建文件夹写“对应需求 7”、重命名写“对应需求 12”，当前应为需求 9 和需求 13
  - `.kiro/specs/115-cloud-drive-integration/tasks.md:425`：写“`fc` 值域转换 → `IsDirectory`”，但 `.kiro/specs/115-cloud-drive-integration/requirements.md:112` 明确要求不得把 `fc/type/sta/m` 推断为官方枚举
- **建议**：统一改为“仅在真实样本验证后派生；未知时保持 unknown/null”。

### P5. 中优先级遗留：部分需求颗粒度仍偏粗

- **位置**：
  - `.kiro/specs/115-cloud-drive-integration/requirements.md:368-384`：需求 20 同时覆盖任务列表、配额、统计、签名脱敏
  - `.kiro/specs/115-cloud-drive-integration/requirements.md:417-430`：需求 23 聚合多个用户中心来源与敏感字段过滤
- **状态**：这是上轮质量报告已识别的遗留项。当前 API 边界已足够保守，不属于立即阻塞，但拆分后更利于任务追溯和测试。

## 通过项

- `docs/115-api-docs.md` 已清楚标注 2026-05-04 最新采集与 2026-04-18 上传手动补充。
- `/files/search` 已从“未采集 item schema”修正为有限 schema available，并保留异常 fallback。
- `/files/history` 已兼容 2026-05-04 空数组和 2026-04-18 对象分支。
- `/files/video` 成功播放 schema 未采集的边界在 requirements/design 中基本清楚。
- 未采集写接口整体仍保持 `cloud_drive_api_not_collected`，离线签名不可用仍保持 `offline_signature_not_available`。

## 验证记录

- integration 叶子任务追溯机械校验：`leaf=97 missing=0`
- `git diff --check -- docs/115-api-docs.md .kiro/specs/115-cloud-drive-integration .kiro/specs/115-cloud-drive-frontend`：无 whitespace error；仅 Git 提示 integration `design.md` / `tasks.md` 下次触碰会 CRLF -> LF

## 建议修复顺序

1. 先修 `tasks.md` 的 `_需求:` 语义漂移，尤其文件操作、预览/视频 Controller、后台任务、缓存/限流装配验证。
2. 再决定“spec 驱动代码”还是“spec 回退到现有代码”：搜索与视频 DTO 当前已明显不一致，不能继续把相关任务视为完成。
3. 最后拆分 R20/R23 颗粒度，并把设计中的旧编号和 `fc → IsDirectory` 推断表述清掉。
