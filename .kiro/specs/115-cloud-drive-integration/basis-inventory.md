# 依据盘点：115-cloud-drive-integration（脱敏网页契约）

| ID | 来源 | 位置 | 条件/触发 | 行为/规则 | 类型 | 是否必须迁入 | requirements/bugfix/spec | design | tasks | 状态 |
|----|------|------|-----------|-----------|------|--------------|--------------------------|--------|-------|------|
| B01 | 已验收结构化采集结果 | `capture-result.json` SHA-256 `1f1cfdf92f8c355e6b5979e0829e8b691d034e5a341f0bb1131395a1bfe39c60` | workflow `115-web-contract-20260809-019fe56c` 第一阶段完成 | 只允许读取 result 中列出的 artifacts/evidence；旧 manual-capture 不得作为依据 | 边界/证据 | 是 | 已映射 | 已映射 | 已映射 | 已覆盖 |
| B02 | 总脱敏契约证据清单 | artifact SHA-256 `5dfa9f316ea091961ecd4223b0d81b8203d708cd1f19a7d4671fcb41ff3f5421` | 文件写操作、下载、回收站、分享、视频、云下载 | collected/low-confidence/not-collected 分级；observed 参数不得推断必填性 | 方法/路径/字段/状态 | 是 | R4、R8–R22、R25 | Gateway/DTO/错误适配 | T2–T8 | 已映射 |
| B03 | 会员账号脱敏契约清单 | artifact SHA-256 `e8d23827a17d01f996e3463a7cd45fc5f41858ddf55e97e9e2709cc0ae94105b` | 余额、VIP、空间、视频、播放进度 | 成功字段只按脱敏 schema 映射；基础资料、清晰度、音轨、字幕 item 不得猜测 | 字段/隐私 | 是 | R16–R18、R23、R26 | Video/User DTO | T4、T5、T8 | 已映射 |
| B04 | 云下载补采证据清单 | artifact SHA-256 `ff3d569288a283f186891956dc98002ed3debcb9990999d347938f69c3f3ed8a` | URL/BT 创建与删除网页场景 | UI 成功不等于 HTTP 契约；签名算法、成功 body、非空 task item 与清空保持未采集 | 场景/边界 | 是 | R20–R21 | Offline | T5.1、T6.3 | 已映射 |
| B05 | 遗漏契约补采增量 | artifact SHA-256 `8e210c5f26ad0dde9a25c9775ba83c3e21926b1f7211a5d6717f7ef5d06f7cf9` | 回收站/分享非空、字幕抽样、永久删除场景 | 只提升明确字段的置信度；未暴露 XHR body 的写操作仍不得启用 | 字段/边界 | 是 | R14、R17、R22 | Recycle/Share | T3.3、T4.2、T5.2、T6.2 | 已映射 |
| B06 | 仓库 Spec 格式 | `.agents/skills/115web-project-rules/references/rules/spec-format.md` | 修改 requirements/design/tasks | EARS、章节、叶子任务双向追溯与父任务打标 | 格式/追溯 | 是 | 已应用 | 已应用 | 已应用 | 已验证 |
| B07 | frontend 消费契约 | `.kiro/specs/115-cloud-drive-frontend/` | capability 状态跨 spec 暴露 | integration 是 `/api/*` capability owner；未采集能力必须在前端禁用 | owner/consumer | 是 | 已映射 | 已映射 | 已映射 | 已覆盖 |
