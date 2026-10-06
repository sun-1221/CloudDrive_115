# 115 cloud drive integration API 对齐全量复审报告

## 审核快照

- **审核时间**：2026-05-04 19:27:49 +08:00
- **审核轮次**：针对 18:54 API 对齐修复验证后的全量复审
- **官方文档基线**：`docs/115-api-docs.md`（2026-05-04 自动采集 `20260504073432-ck05g2` + 2026-04-18 上传链路手动补充）
- **Specs 基线**：`.kiro/specs/115-cloud-drive-integration/{requirements,design,tasks}.md`
- **上轮审核**：`.kiro/specs/115-cloud-drive-integration/audit-history/latest.md`
- **本轮范围**：纳入 integration 的读/上传/视频/离线/分享/用户接口，及未采集写接口的反向追溯复查

## 审核工作量

- **端点总数 N**：20 个纳入业务接口 + 13 类明确排除接口复查
- **维度数 D**：10
- **细项总数 I**：48
- **检查点总数**：960（20 × 48）

## API 对齐结论

本轮未发现新的 115 官方文档对齐违规。18:54 修复后的关键边界继续成立：

- `/files/video` 旧 `videoUrls/availableDefinitions/vipUpgradeRequired` 口径未回归；当前只声明 `qualityOptions/audioTracks/restrictionReason/usageRestrictionNotice`，成功播放 schema 未采集前保持 `not_collected` 或空集合。
- `/files/search` 仍按 2026-05-04 非空 item schema 映射，`ns` 不作为可信 HTML 直接交给前端渲染。
- `/files/index_info` 空间详情仍保持 `CloudDriveSpaceInfoDto.schemaStatus='not_collected'`，不伪造总空间/已用空间/分类占比。
- 回收站 `GET /rb` 仍只使用已采集顶层字段，item schema 未采集时返回空态。
- 未采集写接口仍返回 `cloud_drive_api_not_collected`；云下载签名不可用仍返回 `offline_signature_not_available`。

## API 审核矩阵结果

| 编号 | 端点 | D1 | D2 | D3 | D4 | D5 | D6 | D7 | D8 | D9 | D10 | 问题摘要 |
|---|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:---:|---|
| A01 | `GET webapi.115.com/files` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 无新增 |
| A02 | `GET webapi.115.com/files/search` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 已采集 item schema，安全高亮边界正确 |
| A03 | `POST uplb.115.com/3.0/sampleinitupload.php` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 上传初始化已采集 |
| A04 | `POST OSS PostObject` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 后端代理上传边界正确 |
| A05 | `GET webapi.115.com/rb` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | item schema 未采集空态正确 |
| A06 | `GET webapi.115.com/files/video` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 成功播放 schema 未采集边界正确 |
| A07 | `GET webapi.115.com/files/video_push` / `/movies/transcode` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 转码提交错误 schema 已采集 |
| A08 | `GET/POST webapi.115.com/files/history` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 空数组/对象双形态保留 |
| A09 | `GET webapi.115.com/movies/subtitle` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 字幕 item schema 未采集空态正确 |
| A10 | `115.com/web/lixian task_lists/quota/task_count` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 签名不可用错误码正确 |
| A11 | `webapi.115.com/share/*` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 分享 item schema 未采集空态正确 |
| A12 | `my/vip/pay/user/index_info` 用户聚合 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 敏感字段过滤与空间 not_collected 正确 |
| A13 | 未采集写接口 | N/A | N/A | N/A | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 未发现猜测型 115 path |

## 反向追溯结果

- 未发现 specs 正文把开放平台 `gte_day/lte_day/fc/file_label` 当作网页端搜索参数。
- 未发现 specs 正文把 `limit=1150` 或 `offset+limit≤10000` 当作 115 网页端官方硬限制。
- 未发现未采集写接口出现猜测型真实 115 path。
- 未发现前端 spec 要求直连 115；前端仍只消费后端 `/api/*`。

## 覆盖率验证

- **实检检查点数**：960 / 960
- **未覆盖端点**：无；官方已排除端点按 requirements 范围排除声明复查
- **结论**：API 对齐门控 ✅

## 剩余风险

- 本报告只审核 specs 与 `docs/115-api-docs.md` 的对齐，不审核 Server 当前实现。
- 后续执行 task `13.4` 时仍需补齐搜索 DTO/Handler、视频播放与播放历史契约测试。
