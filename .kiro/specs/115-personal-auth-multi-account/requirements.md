# 需求文档：个人认证与多 115 账号

## 简介

为个人使用场景提供本地数据库用户、JWT 登录会话和多个 115 账号绑定。用户密码只保存密码哈希；115 Cookie 由后端加密保存；普通业务请求从当前用户会话解析当前 115 账号，不要求前端重复提交账号 UID。

## 术语表

- **User**：本地数据库用户，不包含管理员角色语义。
- **UserSession**：本地登录会话，JWT 仅携带用户 ID、会话 ID 与生命周期字段。
- **CookieSession**：用户与 115 账号的绑定凭据记录，Cookie 密文由 Data Protection 保护。
- **CurrentAccount**：用户在当前 UserSession 中选择的当前 115 账号；保存在 UserSession 上。

## 需求

### 1. 个人用户认证

### 需求 1.1：注册

WHEN 匿名用户提交唯一用户名和密码 THE SYSTEM SHALL 创建 User 与 UserSession，返回不包含 115 Cookie 或角色的 JWT。

**验收标准：**
- [ ] AC-1：密码字段只保存不可逆哈希，数据库不出现明文密码。
- [ ] AC-2：重复用户名返回冲突错误。

### 需求 1.2：登录

WHEN 用户提交正确用户名和密码 THE SYSTEM SHALL 创建 UserSession 并返回 JWT；密码错误 THE SYSTEM SHALL 返回 401。

**验收标准：**
- [ ] AC-1：正确凭据返回可验证的 Bearer JWT，并创建服务端会话。
- [ ] AC-2：错误凭据不泄露用户名是否存在且返回 401。

### 需求 1.3：会话生命周期

WHEN 用户退出登录 THE SYSTEM SHALL 撤销对应 UserSession；WHEN JWT 会话已撤销或过期 THE SYSTEM SHALL 拒绝受保护 API；WHEN调用 refresh 且会话仍有效 THE SYSTEM SHALL 返回新的 JWT。

**验收标准：**
- [ ] AC-1：注销后原 JWT 访问受保护 API 返回 401。
- [ ] AC-2：有效会话 refresh 返回新 JWT；过期或撤销会话 refresh 返回 401。

### 2. 多账号绑定与选择

### 需求 2.1：账号绑定

WHEN 已登录用户提交 115 账号 UID 与 Cookie 三元组 THE SYSTEM SHALL 创建或更新该用户的 CookieSession，并使用 Data Protection 加密 Cookie。

**验收标准：**
- [ ] AC-1：相同用户和账号 UID 重复绑定执行更新而非产生重复有效绑定。
- [ ] AC-2：持久化值为密文，任何 API 响应均不包含 Cookie 原文。

### 需求 2.2：账号列表与解绑

WHEN 已登录用户查询账号列表 THE SYSTEM SHALL 只返回账号摘要，不返回 Cookie 原文；WHEN 用户解绑账号 THE SYSTEM SHALL 撤销该绑定。

**验收标准：**
- [ ] AC-1：账号列表仅返回 UID、状态和时间等摘要字段。
- [ ] AC-2：解绑后账号状态不可用于普通业务请求。

### 需求 2.3：当前账号

WHEN 用户选择一个已绑定且 Active 的账号 THE SYSTEM SHALL 保存 CurrentAccount；WHEN 普通业务 API 执行 THE SYSTEM SHALL 使用 CurrentAccount 解析 Cookie；多账号未选择当前账号时 THE SYSTEM SHALL 返回明确业务错误。

**验收标准：**
- [ ] AC-1：选择不存在或非 Active 账号返回稳定业务错误。
- [ ] AC-2：多账号未选择时普通业务 API 不向 115 发起请求并返回 `cloud_drive_account_context_ambiguous`。

### 3. API 与前端

### 需求 3.1：公开认证接口

THE SYSTEM SHALL 提供 `POST /api/auth/register`、`POST /api/auth/login`、`POST /api/auth/logout`、`POST /api/auth/refresh`、`GET /api/auth/me`。

**验收标准：**
- [ ] AC-1：路由、认证要求和响应状态符合接口契约。

### 需求 3.2：账号接口

THE SYSTEM SHALL 提供 `GET/POST/DELETE /api/cloud-drive/accounts` 与 `POST /api/cloud-drive/accounts/select`；前端 SHALL 维护 JWT 会话、登录/注册状态、账号列表、绑定表单和账号切换。

**验收标准：**
- [ ] AC-1：受保护账号接口缺少有效 JWT 时返回 401。
- [ ] AC-2：前端可完成账号列表、绑定、选择和解绑请求。

### 需求 3.3：管理员替换

THE SYSTEM SHALL 不再依赖 LocalAdmin 配置密码、Admin 角色或管理员 Cookie 注入流程。

**验收标准：**
- [ ] AC-1：旧管理员 Cookie 注入路由不存在，个人 JWT 认证链路不包含角色要求。

### 4. 数据库兼容性

### 需求 4.1：数据库兼容性

THE SYSTEM SHALL 使用当前 SQLite CodeFirst 初始化 User、UserSession 与 CookieSession 表；业务层不得依赖 SQLite 专有语法，数据库差异仅存在于 Infrastructure 映射与配置。

**验收标准：**
- [ ] AC-1：Testing host 能在隔离 SQLite 数据库中初始化三类表并完成读写。
- [ ] AC-2：Application/Domain 项目不引用 SQLite 专有 SQL 或方言。
