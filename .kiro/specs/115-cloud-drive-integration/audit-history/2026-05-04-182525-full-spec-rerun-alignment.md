# 115 cloud drive integration API 对齐全量复审报告

## 审核快照

- **审核时间**：2026-05-04 18:25:25 +08:00
- **审核轮次**：针对 17:00 API 对齐修复验证后的全量复审
- **官方文档基线**：`docs/115-api-docs.md`（2026-05-04 自动采集 `20260504073432-ck05g2` + 2026-04-18 上传链路手动补充）
- **Specs 基线**：`.kiro/specs/115-cloud-drive-integration/{requirements,design,tasks}.md`
- **官方端点清单**：docs 中 52 个 `### \`METHOD\`` API 章节，概览声明 76 个唯一接口；integration 当前纳入 20 个业务接口，13 类端点明确排除
- **上轮审核**：`.kiro/specs/115-cloud-drive-integration/audit-history/latest.md`
- **本轮范围**：所有纳入 integration 的读/上传/视频/离线/分享/用户接口，以及未采集写接口的反向追溯

## 审核工作量

- **端点总数 N**：20 个纳入业务接口 + 13 类明确排除接口复查
- **维度数 D**：10
- **细项总数 I**：48
- **检查点总数**：960（20 × 48）

## API 对齐结论

本轮未发现新的 115 官方文档对齐违规。17:00 修复后的关键边界继续成立：

- `/files` 保留 `RawCategoryCode`，不把 `fc/type/sta/m` 推断为官方枚举。
- `/files/search` 已按 2026-05-04 采集到的非空 item schema 进入 DTO/Handler 对齐任务 `13.4`。
- `/files/video` 成功播放 schema 未采集，spec 仍要求 `video_play_schema_not_collected` / `PlaybackSchemaStatus='not_collected'`，不得返回播放 URL、清晰度或音轨。
- `/files/history` 兼容空数组与对象双形态，空数组不伪造 `0` 或空字符串。
- 未采集写接口仍返回 `cloud_drive_api_not_collected`；云下载签名不可用仍返回 `offline_signature_not_available`。

## API 审核矩阵结果

| 编号 | 端点 | D1 方法/路径 | D2 参数 | D3 响应 | D4 行为 | D5 约束 | D6 安全 | D7 版本 | D8 追溯 | D9 反向 | D10 状态 | 问题摘要 |
|---|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:---:|---|
| A01 | `GET webapi.115.com/files` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 无新增 |
| A02 | `GET webapi.115.com/files/search` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 搜索 item schema 对齐进入任务 13.4 |
| A03 | `POST uplb.115.com/3.0/sampleinitupload.php` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 上传初始化已采集 |
| A04 | `POST OSS PostObject` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 前端不直传，只由后端代理 |
| A05 | `GET webapi.115.com/rb` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | item schema 未采集，空态正确 |
| A06 | `GET webapi.115.com/files/video` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 成功播放 schema 未采集边界正确 |
| A07 | `GET webapi.115.com/files/video_push` / `/movies/transcode` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 错误 schema 已采集，转码提交可调用 |
| A08 | `GET/POST webapi.115.com/files/history` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 空数组/对象双形态已声明 |
| A09 | `GET webapi.115.com/movies/subtitle` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 字幕 item schema 未采集，空态正确 |
| A10 | `115.com/web/lixian task_lists/quota/task_count` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 签名不可用错误码正确保留 |
| A11 | `webapi.115.com/share/*` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 分享 item schema 未采集，空态正确 |
| A12 | `my/vip/pay/user/index_info` 用户聚合 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 敏感字段过滤与空间 schema not_collected 正确 |
| A13 | 未采集写接口 | N/A | N/A | N/A | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 未发现猜测型 115 path |

## 反向追溯结果

- 未发现 specs 正文把开放平台 `gte_day/lte_day/fc/file_label` 当作网页端搜索参数。
- 未发现 specs 正文把 `limit=1150` 或 `offset+limit≤10000` 当作 115 网页端官方硬限制。
- 未发现未采集写接口出现猜测型真实 115 path。
- 未发现前端直连 115 的实现要求；frontend spec 中出现的 `webapi.115.com`、`115.com/web/lixian` 仅为架构图或后端内部说明。

## 剩余风险

- 本报告只审核 specs 与 `docs/115-api-docs.md` 的对齐，不审核 Server 当前实现。
- `13.4` 仍是后续代码实现重点：搜索 DTO、视频播放 DTO、播放历史 nullable 映射与契约测试需要落地。

## 覆盖率验证

- **实检检查点数**：960 / 960
- **未覆盖端点**：无；官方已排除端点按 requirements 范围排除声明复查。
- **结论**：API 对齐门控 ✅；质量门控另见 `quality-history/latest.md`。
