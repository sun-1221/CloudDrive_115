# 115 cloud drive integration API 对齐复查报告

## 审核快照

- **审核时间**：2026-05-04 20:03:02 +08:00
- **审核轮次**：针对 19:27 API 对齐全量复审后的再次复查
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

本轮未发现新的 115 官方文档对齐违规。19:27 对齐复审结论保持有效：

- `/files/video` 仍只按当前错误响应 `{ state, errno, error }` 与未采集成功 schema 处理；旧 `videoUrls/availableDefinitions/vipUpgradeRequired` 口径未回归。
- `/files/search` 仍按 2026-05-04 非空 item schema 映射，排除开放平台 `gte_day/lte_day/fc/file_label` 等参数，并继续把 `ns` 视为不可信 HTML 片段。
- `/files/index_info` 空间详情仍保持 `CloudDriveSpaceInfoDto.schemaStatus='not_collected'`，不伪造总空间、已用空间或分类占比。
- 回收站 `GET /rb` 仍只使用已采集顶层字段，item schema 未采集时返回空态。
- 云下载签名未采集时仍返回 `offline_signature_not_available`；未采集写接口仍返回 `cloud_drive_api_not_collected`。

## API 审核矩阵结果

| 编号 | 端点 | D1 | D2 | D3 | D4 | D5 | D6 | D7 | D8 | D9 | D10 | 问题摘要 |
|---|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:---:|---|
| A01 | `GET webapi.115.com/files` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 无新增 |
| A02 | `GET webapi.115.com/files/search` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 已采集 item schema；开放平台参数排除与 `ns` 安全边界正确 |
| A03 | `POST uplb.115.com/3.0/sampleinitupload.php` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 上传初始化已采集 |
| A04 | `POST OSS PostObject` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 后端代理上传边界正确 |
| A05 | `GET webapi.115.com/rb` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | item schema 未采集空态正确 |
| A06 | `GET webapi.115.com/files/video` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 成功播放 schema 未采集边界正确 |
| A07 | `GET webapi.115.com/files/video_push` / `/movies/transcode` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 转码提交错误 schema 已采集 |
| A08 | `GET/POST webapi.115.com/files/history` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 空数组/对象双形态保留 |
| A09 | `GET webapi.115.com/movies/subtitle` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 字幕 item schema 未采集空态正确 |
| A10 | `115.com/web/lixian task_lists/quota/task_count` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 签名不可用错误码正确 |
| A11 | `webapi.115.com/share/*` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 分享 item schema 未采集空态正确 |
| A12 | 用户聚合接口组 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 敏感字段过滤与空间 not_collected 正确 |
| A13 | 未采集写接口 | N/A | N/A | N/A | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | [复查] | 未发现猜测型 115 path |

## 反向追溯结果

- 未发现 specs 正文把开放平台 `gte_day/lte_day/fc/file_label` 当作网页端搜索参数。
- 未发现 specs 正文把 `limit=1150` 或 `offset+limit≤10000` 当作 115 网页端官方硬限制。
- 未发现未采集写接口出现猜测型真实 115 path。
- 未发现前端 spec 要求直连 115；前端仍只消费后端 `/api/*`。

## 验证命令结果

```text
旧字段残留扫描（spec 正文）：无命中
availableDefinitions / videoUrls / userSelectedDefinition / vipUpgradeRequired / CloudDriveStorageDetailsDto
```

```text
关键对齐字段抽查：
qualityOptions / audioTracks / restrictionReason / usageRestrictionNotice / playbackSchemaStatus
CloudDriveSpaceInfoDto / schemaStatus='not_collected' / itemSchemaStatus='not_collected'
cloud_drive_api_not_collected / offline_signature_not_available
```

## 覆盖率验证

- **实检检查点数**：960 / 960
- **未覆盖端点**：无；官方已排除端点按 requirements 范围排除声明复查
- **结论**：API 对齐门控 ✅

## 剩余风险

- 本报告只审核 specs 与 `docs/115-api-docs.md` 的对齐，不审核 Server 当前实现。
- 后续执行 task `13.4` 时仍需补齐搜索 DTO/Handler、视频播放与播放历史契约测试。
