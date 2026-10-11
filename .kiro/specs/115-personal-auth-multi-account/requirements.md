# 需求文档：个人认证与多 115 账号

## 简介

为个人使用场景提供本地数据库用户、JWT 登录会话和多个 115 账号绑定。用户密码只保存密码哈希；115 Cookie 由后端使用 ASP.NET Core Data Protection 加密保存；普通业务请求从当前用户的 `UserSession.ActiveCloudAccountUid` 解析当前 115 账号，不要求前端重复提交账号 UID。

## 术语表

- **User**：本地数据库用户，不包含管理员或角色语义。
- **UserSession**：本地登录会话，保存撤销状态、过期时间和 `ActiveCloudAccountUid`。
- **CookieSession**：用户与 115 账号的绑定凭据记录，Cookie 三元组由 Data Protection 保护。
- **CloudAccountUid**：115 账号 UID；绑定/选择 API 使用 `cloudAccountUid`，领域与缓存使用 `CloudAccountUid`。
- **CurrentAccount**：当前 UserSession 选择的账号，物理字段为 `UserSession.ActiveCloudAccountUid`，数据库列为 `active_cloud_account_uid`。

## 需求

### 需求 1：个人用户认证

1. WHEN 匿名用户提交唯一用户名和密码 THE SYSTEM SHALL 创建 `User` 与 `UserSession`，并返回不包含 115 Cookie 或角色的 Bearer JWT。
2. WHEN 用户提交正确用户名和密码 THE SYSTEM SHALL 创建 `UserSession` 并返回 JWT；IF 密码错误 THEN THE SYSTEM SHALL 返回 HTTP 401 且不泄露用户名是否存在。
3. WHEN 用户退出登录 THE SYSTEM SHALL 撤销对应 `UserSession`；WHEN JWT 会话已撤销或过期 THE SYSTEM SHALL 拒绝受保护 API；WHEN 有效会话调用 refresh THE SYSTEM SHALL 返回新的 JWT。

**验收标准：**
- [ ] AC-1：密码字段只保存不可逆哈希，数据库不出现明文密码。
- [ ] AC-2：注销后原 JWT 访问受保护 API 返回 401；有效会话 refresh 成功，过期或撤销会话 refresh 返回 401。`r`n- [ ] AC-3：重复用户名返回冲突错误。

### 需求 2：多账号绑定与选择

1. WHEN 已登录用户提交 `CloudAccountUid` 与 UID/CID/USSID Cookie 三元组 THE SYSTEM SHALL 创建或更新该用户的 `CookieSession`，并使用 Data Protection 加密 Cookie。
2. WHEN 已登录用户查询或解绑账号 THE SYSTEM SHALL 仅返回账号摘要并撤销目标绑定；任何响应不得包含 Cookie 原文。
3. WHEN 用户选择已绑定且 Active 的账号 THE SYSTEM SHALL 将 `CloudAccountUid` 写入当前 `UserSession.ActiveCloudAccountUid`；IF 多账号未选择当前账号 THEN 普通业务 API SHALL 返回 `cloud_drive_account_context_ambiguous` 且不发起 115 请求。

**验收标准：**
- [ ] AC-1：相同用户和账号 UID 重复绑定执行更新而非产生重复有效绑定。
- [ ] AC-2：账号列表仅返回 UID、状态和时间等摘要；选择不存在或非 Active 账号返回稳定业务错误。

### 需求 3：认证与登录态检测 API

1. THE SYSTEM SHALL 提供 `POST /api/auth/register`、`POST /api/auth/login`、`POST /api/auth/logout`、`POST /api/auth/refresh`、`GET /api/auth/me` 以及 `GET /api/auth/check`。
2. WHEN `/api/auth/check` 收到请求 THE SYSTEM SHALL 按可选 JWT 的当前用户和 `UserSession.ActiveCloudAccountUid` 探测 CookieSession，返回 camelCase 的 `isAuthenticated`、`isLoggedIn`、`lastCheckedAt`、可选 `cooldownUntil` 与安全的 `failureReason`，并始终满足 `isAuthenticated === (isLoggedIn === true)`。
3. THE SYSTEM SHALL 不再依赖 LocalAdmin 配置密码、Admin 角色或管理员 Cookie 注入流程；个人 JWT 认证链路不得要求角色声明。

**验收标准：**
- [ ] AC-1：受保护账号接口缺少有效 JWT 时返回 401；`/api/auth/check` 无当前用户、无 Active CookieSession、冷却中或上下文歧义时短路且不发起 115 请求。
- [ ] AC-2：旧管理员 Cookie 注入路由不存在，JWT 不携带 Cookie 原文或角色。

### 需求 4：数据库兼容性

1. THE SYSTEM SHALL 使用当前 SQLite CodeFirst 初始化 `User`、`UserSession` 与 `CookieSession` 表；业务层不得依赖 SQLite 专有语法，数据库差异仅存在于 Infrastructure 映射与配置。

**验收标准：**
- [ ] AC-1：Testing host 能在隔离 SQLite 数据库中初始化三类表并完成读写。
- [ ] AC-2：Application/Domain 项目不引用 SQLite 专有 SQL 或方言。
