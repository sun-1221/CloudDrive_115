# 115 cloud drive frontend spec 修复验证报告

## 审核快照

- **审核时间**：2026-05-04 17:00:27 +08:00
- **审核轮次**：针对 16:44 全量合理性审核的修复验证
- **Specs 基线**：`.kiro/specs/115-cloud-drive-frontend/{requirements,design,tasks}.md`
- **后端契约依据**：`.kiro/specs/115-cloud-drive-integration/*` 与 `docs/115-api-docs.md`
- **本轮范围**：UploadZone 任务状态、云下载自动刷新条件、视频/字幕采集措辞

## 结论

本轮已修复 frontend spec 的三个局部问题。frontend spec 继续保持“只调用后端 `/api/*`、不直连 115”的边界；上传现在被标为正式可实现能力，未采集明细仍使用 `not_collected` 空态。

## 修复矩阵

| 编号 | 上轮问题 | 状态 | 修复说明 |
|---|---|:--:|---|
| FQ-01 | UploadZone 仍写成占位/等待后端上传就绪 | ✅ 已修复 | `UploadZone` 设计和任务改为正式实现，调用 `POST /api/files/upload` multipart，并处理同步/202 job 双态 |
| FQ-02 | 云下载自动刷新无条件触发 | ✅ 已修复 | requirements/design/tasks 统一为仅当 `itemSchemaStatus='available'` 且存在进行中任务时 10 秒刷新 |
| FQ-03 | 视频/字幕“已采集”措辞过宽 | ✅ 已修复 | 改为上传链路、`/files/video` 错误 schema、字幕顶层数组、视频历史读写端点已采集；成功播放与字幕 item schema 仍 `not_collected` |
| FQ-04 | 搜索安全与空态 | ✅ 保持通过 | `highlightNameRaw/ns` 仍禁止直接 `v-html` |

## 验证记录

- frontend 叶子任务追溯机械校验：`leaf=115 missing=0`
- 残留扫描未命中：`UploadZone（占位）`、`大部分占位`、`等待后端 POST /api/files/upload`、过宽的“上传、视频播放、字幕、视频历史已采集”措辞
- `git diff --check`：无 whitespace error；仅 integration CRLF warning

## 剩余风险

- 本轮仅修 spec，不修改前端代码。
- 云下载 item schema 未采集前，前端仍只能展示明细不可用空态，不应实现基于任务状态的自动刷新。
