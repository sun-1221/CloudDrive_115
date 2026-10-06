# 决策日志：115-cloud-drive-integration

> 当前决策只以 2026-08-09 父阶段已验收的脱敏 capture result 为依据。不创建任何审核历史或证据历史目录。

| ID | 问题 | 当前决策 | 决策时间 | 影响范围 | 状态 |
|---|---|---|---|---|---|
| D26 | 证据权威 | 只引用已验收 `capture-result.json` 中列出的 artifact/evidence；旧采集、第三方实现与其他输出不得补齐契约 | 2026-08-09 | requirements/design/tasks/docs | 已决 |
| D27 | 115 接入架构 | 固定为受保护 Cookie 会话 + 115 网页端接口；系统不主动发起授权流程 | 2026-08-09 | requirements/design/tasks | 已决 |
| D28 | Gateway 命名 | 使用 `ICloudDriveWebApiGateway`，表达网页端接口边界 | 2026-08-09 | requirements/design/tasks | 已决 |
| D29 | 能力门禁 | collected 可启用；low-confidence 与 not-collected 均 fail-closed，不得注册上游路径或创建任务 | 2026-08-09 | requirements/design/tasks/frontend | 已决 |
| D30 | 完成语义 | 只有与已采集成功 envelope 匹配的上游成功才表示同步完成；受理、占位、未知或 disabled 不得标 Completed | 2026-08-09 | design/tasks/frontend | 已决 |
| D31 | 脱敏策略 | Cookie、签名、账号标识、文件名、pickCode、临时 URL 与金额值均完整排除，不采用部分脱敏示例 | 2026-08-09 | requirements/design/tasks/docs | 已决 |
| D32 | 云下载入口 | 使用 `POST clouddownload.115.com/web/?ac=...`；三个已采集读操作无 body，URL 创建仅记录失败样本且保持 disabled | 2026-08-09 | requirements/design/tasks/docs | 已决 |
| D33 | 用户聚合降级 | VIP、余额、空间可用；用户基础资料端点 disabled 且 `info=null` | 2026-08-09 | requirements/design/tasks/frontend | 已决 |
