# 设计文档：个人认证与多 115 账号

## 概述

新增 User 与 UserSession 领域模型，使用 CookieSession 保存用户与 115 账号的绑定凭据。Application 通过 CQRS Handler 编排注册、登录、注销、刷新、账号绑定与选择；Infrastructure 使用 SqlSugar CodeFirst、PBKDF2 和 ASP.NET Core Data Protection；WebApi 仅通过 ISender 进入用例，并在边界签发不含角色或 Cookie 的 JWT。

## 系统架构

WebApi Controller -> MediatR -> Application Handler -> Domain Repository -> Infrastructure(SqlSugar/DataProtection)。

JWT 仅包含 sub、NameIdentifier、sid、jti、iat/exp。115 Cookie 原文只在受保护的解密管道内短暂存在，永不进入 JWT、日志、ProblemDetails 或前端响应。普通业务请求根据当前 JWT 对应的 UserSession.ActiveCloudAccountUid 解析账号，不要求前端重复提交 UID。

## 组件设计

## User/UserSession

User 保存用户名与密码哈希，不包含管理员或角色字段。UserSession 保存用户 ID、会话 ID、过期/撤销状态和当前账号。当前账号命名映射如下：

| 语义 | HTTP/API | 领域属性 | 数据库列 |
|---|---|---|---|
| 115 账号 UID | cloudAccountUid | CloudAccountUid | cloud_account_uid |
| 当前会话账号 | cloudAccountUid | UserSession.ActiveCloudAccountUid | active_cloud_account_uid |

_覆盖需求: 1.1, 1.2, 1.3, 2.3, 4.1_

## PersonalAuthHandlers

实现注册、登录、注销、刷新和当前用户查询。密码通过 PBKDF2 哈希验证；登录成功后创建或恢复 UserSession，签发只含用户/会话标识和生命周期字段的 Bearer JWT。refresh 只接受仍有效的服务端会话，不新增明文 refresh token 表。_覆盖需求: 1.1, 1.2, 1.3, 3.1_

## CookieSessionManager + AccountContextResolver

CookieSessionManager 按 UserId + CloudAccountUid 创建、更新、撤销和读取 CookieSession。UID/CID/USSID 使用 IDataProtector 加密持久化；解密值只用于构造上游请求头，任何响应和日志均使用摘要。AccountContextResolver 读取当前 UserSession.ActiveCloudAccountUid：单个 Active 账号可自动选中；多个 Active 账号且未选择时返回 cloud_drive_account_context_ambiguous；不存在或非 Active 账号返回稳定业务错误。_覆盖需求: 2.1, 2.2, 2.3_

## AuthCheckHandler

GET /api/auth/check 是允许匿名访问的上游登录态探测端点。请求带有效 JWT 时按当前用户和 ActiveCloudAccountUid 探测对应 CookieSession；无 JWT、无绑定、冷却中或账号上下文歧义时短路，不发起 115 请求。响应为 CloudDriveAuthCheckDto，字段使用 camelCase，并保持 isAuthenticated === (isLoggedIn === true)。_覆盖需求: 3.1, 3.2_

## AuthController / CloudDriveAccountsController

AuthController 暴露 /api/auth/register、/login、/logout、/refresh、/me 和 /check；账号控制器暴露 GET/POST/DELETE /api/cloud-drive/accounts 与 POST /api/cloud-drive/accounts/select。除公开认证端点外均要求有效 JWT；控制器不声明 Admin 角色，也不提供 LocalAdmin Cookie 注入路由。_覆盖需求: 3.1, 3.2, 3.3_

## Pbkdf2PasswordHasher / SqlSugar 映射

PBKDF2 参数和密码哈希只由 Infrastructure 实现；SqlSugar CodeFirst 映射 users、user_sessions、cloud_drive_cookie_sessions。Application/Domain 不引用 SQLite 方言。_覆盖需求: 1.1, 4.1_

## 数据模型与接口

| 表 | 关键字段 | 说明 |
|---|---|---|
| users | id,user_name,password_hash | 个人用户 |
| user_sessions | id,user_id,active_cloud_account_uid,expires_at,revoked_at | JWT 会话和当前账号 |
| cloud_drive_cookie_sessions | user_id,cloud_account_uid,encrypted_cookie_payload,status | 用户账号绑定与密文凭据 |

认证响应返回 accessToken/tokenType/expiresAt/userId；账号 API 只返回 cloudAccountUid/status/createdAt/lastCheckedAt 等摘要；/api/auth/check 返回 isAuthenticated/isLoggedIn/lastCheckedAt/cooldownUntil?/failureReason?，不返回 Cookie。

## 关键时序

~~~mermaid
sequenceDiagram
    participant User
    participant Frontend
    participant Backend
    participant Database
    User->>Frontend: 提交用户名和密码
    Frontend->>Backend: POST /api/auth/login
    Backend->>Database: 验证 User，创建 UserSession
    Backend-->>Frontend: Bearer JWT(UserId, SessionId, exp)
    Frontend->>Backend: GET /api/auth/check
    Backend->>Database: 读取 UserSession.ActiveCloudAccountUid
    Backend-->>Frontend: AuthCheckDto（仅安全状态）
~~~

## 错误处理

密码错误、会话失效或缺少 JWT 映射为 401；重复用户名映射为 409；账号不存在或非 Active 映射为 404/业务错误；多账号未选择返回稳定业务码 cloud_drive_account_context_ambiguous；/api/auth/check 对无用户、无绑定、冷却或网络异常返回安全的 failureReason，不泄露 Cookie。

## 测试策略

## 测试策略

覆盖密码哈希不可逆与验证、注册/登录/注销/刷新 Handler、JWT 不含 Cookie/Role、/api/auth/check 的匿名/当前会话/歧义分支、账号绑定密文与摘要脱敏、ActiveCloudAccountUid 命名映射，以及 WebApi 401/409/404 契约；前端覆盖 token 拦截器、登录注册状态和账号切换。
