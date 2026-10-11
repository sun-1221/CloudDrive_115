# 需求文档：115 网盘前端

## 简介

本前端仅消费 `115-cloud-drive-integration` 暴露的后端 API。用户通过本地 JWT 注册/登录维持 UserSession；115 上游 Cookie 由后端按当前用户的 CookieSession 管理，浏览器端不接触 Cookie、签名或账号凭据。

契约依据仅为父阶段于 2026-08-09 验收的脱敏网页端采集结果及 `docs/115-api-docs.md`。前端必须遵循能力闭集：`collected` 可实现，`low-confidence` 与 `disabled` 不得提供可触发入口。后端端点存在但返回占位错误，不等于能力可用。

## 能力矩阵

| 前端能力 | 状态 | UI 规则 |
|---|---|---|
| JWT 注册、登录、刷新、退出与当前用户 | available | 只保存 JWT 会话状态，不保存 Cookie 原文 |
| 115 账号列表、绑定、选择、解绑 | available | 只展示摘要，当前账号来自 UserSession.ActiveCloudAccountUid |
| 文件列表、详情派生、下载、新建目录、单项复制、重命名、星标 | collected | 可实现并正常展示 |
| 回收站列表及非空 item | collected | 可展示；写按钮禁用 |
| 视频基础播放信息、字幕空态、播放历史读取 | collected/partial | 基础字段可用；清晰度/音轨/字幕 item 不推断 |
| 云下载列表 envelope、统计、配额 | collected/partial | 可展示 envelope；任务 item 为空态 |
| 我的分享 `slist` 及 item | collected | 只启用“我的分享” |
| VIP、余额、空间 | collected | 可展示；金额不得进入日志 |
| 用户基础资料 | disabled | 不请求、不展示头像/昵称占位推断 |
| 上传、搜索、移动、删除、回收站写 | disabled | 无可触发入口 |
| 播放进度写、主动转码 | disabled | 无可触发入口 |
| 云下载写、分享 send/receive | disabled | 无可触发入口 |

## 需求

### 需求 1：个人 JWT 会话与多账号上下文

**用户故事：** 作为个人用户，我希望注册/登录后管理多个 115 账号，并在当前 UserSession 中选择要使用的账号。

#### 验收标准

1. WHEN 用户注册或登录成功 THEN 前端 SHALL 保存 Bearer JWT 及过期时间，并支持 `POST /api/auth/refresh`、`POST /api/auth/logout` 和 `GET /api/auth/me`。
2. WHEN 应用启动或 JWT 会话仍有效 THEN 前端 SHALL 调用 GET /api/auth/check 获取当前 CookieSession 登录态，并区分本地 JWT 会话与上游账号状态。
3. WHEN 用户打开账号管理 THEN 前端 SHALL 调用 `GET /api/cloud-drive/accounts`，仅展示账号 UID、状态和时间摘要，不读取或持久化 Cookie 原文。
4. WHEN 用户绑定、选择或解绑账号 THEN 前端 SHALL 分别调用 `POST /api/cloud-drive/accounts`、`POST /api/cloud-drive/accounts/select` 和 `DELETE /api/cloud-drive/accounts/{cloudAccountUid}`；绑定表单的 Cookie 值只存在于请求内存。
5. WHEN 当前 UserSession 未选择账号且存在多个 Active 账号 THEN 前端 SHALL 展示明确的账号选择提示，不发起普通 115 业务请求；后端返回 `cloud_drive_account_context_ambiguous` 时不得自动重试。
6. WHEN JWT 会话失效或退出 THEN 前端 SHALL 清理内存中的会话和账号摘要，并导航到登录页；不得展示、请求或存储任何上游 Cookie 字段。
7. WHEN 用户基础资料不可用 THEN 前端 SHALL 不调用 `/api/user/info`，并使用中性账号图标而非伪造昵称或头像。

### 需求 2：文件浏览器

**用户故事：** 作为用户，我希望按目录浏览已采集的文件列表，以便查看文件和目录。

#### 验收标准

1. WHEN 用户进入目录 THEN 前端 SHALL 通过后端文件列表 API 传递 `cid/offset/limit/type/asc/o/customOrder/stdir/showDir` 的受支持子集并渲染返回条目。
2. WHEN 列表返回面包屑 path THEN 前端 SHALL 使用后端归一化字段渲染层级导航，不自行拼接上游标识。
3. IF item 字段未形成可证明语义 THEN 前端 SHALL 显示中性值或省略字段，不推断分类枚举。
4. WHEN 用户刷新 THEN 前端 SHALL 重新请求当前目录且保留当前分页、排序和视图上下文。

### 需求 3：文件操作

**用户故事：** 作为用户，我希望只看到有已验收契约支撑的操作，以免触发未知上游行为。

#### 验收标准

1. WHEN 用户选择文件 THEN 前端 SHALL 仅启用下载、单项复制、重命名和星标，并按后端成功结果刷新受影响目录。
2. WHEN 用户位于目录 THEN 前端 SHALL 允许新建文件夹并执行前端名称基础校验。
3. IF 用户选择多项 THEN 前端 SHALL 禁用复制，直至批量契约获得独立验收证据。
4. WHERE 上传、移动或删除能力为 disabled THEN 前端 SHALL 不渲染可触发按钮、快捷键或上下文菜单项。
5. WHEN 后端返回同步成功 THEN 前端 SHALL 将操作标为完成；IF 仅返回受理、占位或未知结果 THEN 前端 SHALL 不显示完成状态。

### 需求 4：上下文菜单与批量工具栏

**用户故事：** 作为用户，我希望上下文操作与当前能力一致，以便不会误触未支持功能。

#### 验收标准

1. WHEN 打开单项上下文菜单 THEN 前端 SHALL 根据 capability matrix 只显示可用操作。
2. WHEN 选中多项 THEN 前端 SHALL 仅展示确有批量契约的操作；当前不得把单项复制扩展为批量复制。
3. WHERE 操作被禁用 THEN 前端 SHALL 从键盘、移动端动作面板和工具栏同时移除其触发路径。

### 需求 5：文件搜索

**用户故事：** 作为用户，我希望搜索入口不会在上游契约未知时产生误导。

#### 验收标准

1. WHERE 搜索契约未包含在验收证据中 THEN 前端 SHALL 禁用搜索栏、`Ctrl+F` 搜索动作和搜索 API 调用。
2. WHEN 页面需要表达搜索状态 THEN 前端 SHALL 显示“搜索暂不可用”，不得用文件列表过滤冒充上游搜索。

### 需求 6：回收站管理

**用户故事：** 作为用户，我希望查看回收站内容，同时避免执行未经验证的写操作。

#### 验收标准

1. WHEN 打开回收站 THEN 前端 SHALL 展示已采集 item 的文件名、大小、原目录、删除时间、状态和类型原始值。
2. WHERE 还原、彻底删除和清空契约未采集 THEN 前端 SHALL 禁用这些按钮、快捷键和批量动作。
3. IF 列表为空 THEN 前端 SHALL 显示真实空态，不显示 `not_collected` 占位。

### 需求 7：图片预览

**用户故事：** 作为用户，我希望安全预览图片，以便查看内容且不泄露临时资源。

#### 验收标准

1. WHEN 用户预览图片 THEN 前端 SHALL 使用后端基于已采集下载契约返回的临时资源。
2. WHERE 临时 URL 被消费 THEN 前端 SHALL 不写入持久化 Store、日志、埋点或错误消息。
3. WHEN 预览关闭或过期 THEN 前端 SHALL 清理内存引用并允许用户重新获取。

### 需求 8：视频播放器

**用户故事：** 作为用户，我希望播放界面只消费已采集的视频字段，以便区分真实可用与未知能力。

#### 验收标准

1. WHEN `playbackSchemaStatus='available'` THEN 前端 SHALL 消费基础视频字段并按可用 URL 状态决定是否播放。
2. WHERE `qualityOptions` 或 `audioTracks` 为空 THEN 前端 SHALL 隐藏清晰度和音轨选择器，不构造默认选项。
3. WHERE 字幕 item schema 为 `not_collected` THEN 前端 SHALL 显示字幕明细不可用且不伪造语言条目。
4. WHEN 播放历史读取成功 THEN 前端 SHALL 可恢复 `time/watchEnd`；WHERE 进度写入未采集 THEN 前端 SHALL 不发送保存请求。
5. WHERE 主动转码契约未采集 THEN 前端 SHALL 不显示转码按钮、不轮询转码 Job，也不把 `video_push` 非 JSON 结果视为成功。

### 需求 9：文档预览

**用户故事：** 作为用户，我希望通过受控临时资源预览支持的文档。

#### 验收标准

1. WHEN 文档类型可由浏览器或已配置渲染器处理 THEN 前端 SHALL 使用后端临时资源并显示加载/失败状态。
2. IF 类型不支持 THEN 前端 SHALL 提供下载操作或明确不可预览提示。
3. WHERE 临时资源被使用 THEN 前端 SHALL 遵循需求 7 的不持久化与清理规则。

### 需求 10：后台任务完成语义

**用户故事：** 作为用户，我希望任务状态真实反映已确认结果。

#### 验收标准

1. WHEN 后端返回 `Completed` THEN 前端 SHALL 仅在对应已启用操作的上游成功已确认时展示“已完成”。
2. IF 能力为 disabled、结果仅被受理或状态未知 THEN 前端 SHALL 不创建轮询任务，也不展示完成提示。

### 需求 11：主题与视觉一致性

**用户故事：** 作为用户，我希望界面在明暗主题下都清晰一致。

#### 验收标准

1. WHEN 用户切换主题 THEN 前端 SHALL 在不改变 capability 状态的前提下更新颜色、对比度和表面样式。
2. WHERE 控件被禁用或隐藏 THEN 前端 SHALL 保持一致的可访问语义，不能只依赖颜色表达状态。

### 需求 12：响应式布局

**用户故事：** 作为用户，我希望在桌面、平板和移动端安全使用相同能力闭集。

#### 验收标准

1. WHEN 视口变化 THEN 前端 SHALL 在侧栏、底部导航、列表/网格和全屏预览之间响应式切换。
2. WHERE 某能力 disabled THEN 前端 SHALL 在所有断点保持禁用，不得因移动端动作面板而重新暴露。

### 需求 13：键盘与可访问性

**用户故事：** 作为键盘用户，我希望可用操作具备一致快捷键且禁用操作无法触发。

#### 验收标准

1. WHEN 焦点位于文件浏览器 THEN 前端 SHALL 为打开、重命名、单项复制、下载和选择提供可访问键盘路径。
2. WHERE 搜索、上传、移动或删除 disabled THEN 前端 SHALL 不注册对应快捷键。
3. WHEN 模态框打开 THEN 前端 SHALL 管理焦点陷阱、Esc 关闭和焦点恢复。

### 需求 14：加载、空态与局部失败

**用户故事：** 作为用户，我希望每个区域独立表达加载、空态和失败，以便理解当前状态。

#### 验收标准

1. WHEN 请求进行中 THEN 前端 SHALL 在对应区域显示加载状态并防止重复提交。
2. IF 子模块请求失败 THEN 前端 SHALL 保留其他成功区域并提供局部重试。
3. IF schema 明确为 `not_collected` THEN 前端 SHALL 使用“明细暂不可用”而非“无数据”；IF schema available 且数组为空 THEN SHALL 使用真实空态。

### 需求 15：云下载管理

**用户故事：** 作为用户，我希望查看已采集的云下载统计与配额，同时不触发未知写操作。

#### 验收标准

1. WHEN 打开云下载页 THEN 前端 SHALL 加载任务列表 envelope、任务统计和配额。
2. WHERE task item schema 为 `not_collected` THEN 前端 SHALL 不渲染任务字段、不按状态自动轮询。
3. WHERE URL/BT 新增、删除和清空能力 disabled THEN 前端 SHALL 不显示可触发入口。

### 需求 16：分享管理

**用户故事：** 作为用户，我希望查看已采集的“我的分享”列表。

#### 验收标准

1. WHEN 打开分享页 THEN 前端 SHALL 只请求并展示 `slist`，其 item schema 状态为 `available`。
2. WHERE `send` 与 `receive` 只有错误样本 THEN 前端 SHALL 隐藏对应标签页并不发起请求。
3. WHERE item 包含临时或敏感值 THEN 前端 SHALL 不写入日志或持久化存储。

### 需求 17：用户中心

**用户故事：** 作为用户，我希望查看已采集的会员、余额和空间信息。

#### 验收标准

1. WHEN 打开用户中心 THEN 前端 SHALL 并行加载 VIP、余额与空间，且子模块可独立失败。
2. WHEN 空间 schema available THEN 前端 SHALL 展示 total/use/remain 与已返回的分类统计，保留后端字段语义。
3. WHERE 用户基础资料 schema 为 `not_collected` THEN 前端 SHALL 不调用资料端点、不渲染伪造昵称、头像或联系方式。
4. WHERE 余额字段被消费 THEN 前端 SHALL 仅用于当前视图展示，不进入日志、埋点或持久化缓存。

### 需求 18：通用错误处理策略

**用户故事：** 作为用户，我希望错误信息稳定、可恢复且不泄露上游细节。

#### 验收标准

1. WHEN 后端返回 ProblemDetails THEN 前端 SHALL 优先解析根级 `code`，兼容读取 `extensions.code`，并显示安全的 `detail`。
2. IF HTTP 400 包含 `errors` THEN 前端 SHALL 优先展示字段级错误；否则显示 `detail`。
3. IF code 为 `cloud_drive_api_not_collected` THEN 前端 SHALL 将对应能力视为 disabled，而不是自动重试。
4. IF HTTP 401/403/429/404/410/502/503 THEN 前端 SHALL 按稳定错误策略导航、提示或局部刷新，且不得展示 `upstreamMessage`、Cookie、签名或临时 URL。

### 需求 19：前端架构与契约边界

**用户故事：** 作为维护者，我希望前端只依赖后端公开契约，以便上游网页接口和敏感凭据不泄漏到浏览器。

#### 验收标准

1. WHERE 前端访问数据 THEN 前端 SHALL 只调用 `/api/*` 后端端点，不直接访问 115 域名。
2. WHEN API 类型生成 THEN 前端 SHALL 以当前后端 OpenAPI 为类型权威，并用最小业务扩展表达 capability 状态。
3. WHERE capability 为 disabled THEN Api、Service、Store、组件、快捷键和路由入口 SHALL 形成一致的零调用闭环。
4. WHEN 记录日志、埋点或错误 THEN 前端 SHALL 完整排除 Cookie、签名、账号标识、文件名、pickCode、临时 URL 与金额值。
5. WHEN 新证据被验收 THEN 前端 SHALL 先同步 integration spec、API 文档和 OpenAPI，再启用对应 UI。
