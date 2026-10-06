# 决策日志：115-cloud-drive-frontend

> 后端 capability 状态以 integration spec、`docs/115-api-docs.md` 与 2026-08-09 已验收脱敏网页契约为准。不创建任何审核历史或证据历史目录。

| ID | 问题 | 当前决策 | 决策时间 | 影响范围 | 状态 |
|---|---|---|---|---|---|
| D32 | 未采集能力入口 | 搜索、上传、移动、删除、回收站写、播放进度写、主动转码、云下载写与 send/receive 在 HTTP 成功契约验收前均 disabled，并在 Api/Service/Store/UI/快捷键形成零调用闭环 | 2026-08-09 | requirements/design/tasks | 已决 |
| D33 | 新增 collected 能力 | 下载、新建目录、单项复制、重命名、星标、回收站明细、视频基础字段、播放历史读取与 slist item 按新契约启用 | 2026-08-09 | requirements/design/tasks | 已决 |
| D34 | Schema 状态 | `not_collected` 表示明细不可用；`available` 且空数组才表示真实无数据 | 2026-08-09 | requirements/design/tasks | 已决 |
| D35 | 用户中心 | 基础资料保持 `null`；只展示已采集 VIP、余额与空间。金额只驻留页面内存，不进入日志或持久化 | 2026-08-09 | requirements/design/tasks | 已决 |
| D36 | 任务完成语义 | 只有已启用操作得到确认成功才展示 Completed；受理、占位、unknown 或 disabled 不创建轮询且不显示完成 | 2026-08-09 | requirements/design/tasks | 已决 |
| D37 | 敏感值策略 | Cookie、签名、账号标识、文件名、pickCode、临时 URL 与金额值均完整排除，不采用部分脱敏示例 | 2026-08-09 | requirements/design/tasks | 已决 |
