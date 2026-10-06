# 决策记录：个人认证与多账号

| ID | 问题 | 当前决策 | 依据 | 状态 |
|---|---|---|---|---|
| D01 | 是否继续使用管理员表/角色 | 否；个人用户表直接承载账号，JWT 不携带角色 | 用户明确要求 | 已决策 |
| D02 | 115 Cookie 如何持久化 | 继续使用现有 `CookieSession` 聚合作为用户-115账号绑定凭据记录，密文由 Data Protection 保护 | 现有代码与用户要求 | 已决策 |
| D03 | 当前账号保存位置 | 保存于 `user_sessions.active_cloud_account_uid`；没有选择时单个 Active 账号自动使用，多账号要求显式选择 | 用户会话级选择，支持同一用户不同会话使用不同 115 账号 | 已决策 |
| D04 | Refresh Token 形态 | 本期使用仍有效的 JWT 调用 refresh，服务端会话记录支持撤销；不新增明文 refresh token 表 | 用户未要求独立 refresh token 轮换 | 已决策 |
| D05 | 验收证据边界 | 构建、静态检查和单元测试只能证明对应层级；注册/登录/撤销/刷新及多账号隔离必须通过 Testing host + SQLite 的 HTTP/DB 闭环验收 | 当前 WebApi 集成测试主要验证授权和 fail-closed，尚未覆盖个人认证闭环 | 已决策 |
