# 需求文档：115 网盘前端展示

## 简介

本功能为 115 网盘 Web 客户端的前端展示层，通过**后端 integration 服务**提供的 `/api/*` REST 端点消费 115 网盘数据，构建现代化 Web 前端界面。视觉风格采用暗色主题（Dark Mode）结合 Glassmorphism（毛玻璃效果）。前端技术栈基于 Vue 3 + TypeScript，使用 Video.js 实现视频播放，支持响应式布局与键盘快捷键操作。

> **架构说明：** 前端不直连 115 网页端 API，也不参与 Cookie、签名、频控、数据归一化等底层细节。所有业务逻辑由后端 `.kiro/specs/115-cloud-drive-integration/` 定义的 .NET 10 DDD 服务处理，前端仅消费标准化的 REST 响应。

> **鉴权说明：** 采用 Cookie-based 认证（由后端统一注入），前端**无需处理** Cookie、UID、CID、USSID、签名等敏感参数。登录态通过 `GET /api/auth/check` 端点查询。单账号模型，无多账号切换。

> **Cookie 安全边界：** Cookie-based 仅说明会话承载方式，不代表自动具备安全防护。后端 integration 服务 SHALL 对会话 Cookie 设置 `HttpOnly`、`Secure` 与明确的 `SameSite` 策略，并对状态变更请求实施 CSRF 防护；前端不得读取、写入或覆盖会话 Cookie。

> **API 契约权威性：** 本 spec 依赖的后端 `/api/*` 端点契约以 `.kiro/specs/115-cloud-drive-integration/requirements.md` 与后端 OpenAPI 规范（`/openapi/v1.json`）为准；integration 服务访问 115 的外部接口以 `docs/115-api-docs.md`（2026-04-08 自动采集 + 2026-04-18 手动补充）为依据。前端 DTO SHALL 与 OpenAPI 规范同步生成 TypeScript 类型。

> **文档版本基线：** 本需求文档对齐后端 integration spec 的 30 条需求。后续增量审核以此基线为锚。

## 术语表

### UI 组件与服务（前端）

- **Frontend_App**：115 网盘前端应用，负责用户界面渲染与交互
- **Backend_API**：后端 integration 服务提供的 `/api/*` REST 端点
- **File_Browser**：文件浏览器组件，展示文件列表、网格视图、面包屑导航
- **Upload_Zone**：拖拽上传区域组件 _（后端已按 `docs/115-api-docs.md` 采集上传链路；前端只调用 `/api/files/upload`）_
- **Context_Menu**：右键上下文菜单组件
- **Search_Bar**：搜索栏组件
- **Recycle_Bin_View**：回收站视图
- **Image_Preview**：图片预览灯箱组件
- **Video_Player**：视频播放器组件（基于 Video.js）
- **Document_Preview**：文档预览组件
- **Offline_Task_Panel**：云下载任务面板（位于云下载页面）
- **Share_View**：分享管理视图
- **User_Center_View**：用户中心视图
- **Toast_Notification**：全局消息提示组件
- **Confirm_Dialog**：确认对话框组件
- **Breadcrumb**：面包屑导航组件
- **Theme_Controller**：主题切换控制器
- **Keyboard_Manager**：键盘快捷键管理器
- **Skeleton_Loader**：骨架屏加载组件
- **Error_State**：错误状态组件
- **AuthGuard**：布局层守卫组件，根据登录态渲染主应用或登录引导页
- **TemporaryResourceService**：前端临时资源服务，处理图片/视频 URL 过期与一次透明重试

### 后端 DTO 与响应契约

> 以下 DTO 由后端 integration 服务定义，前端通过 OpenAPI 自动生成 TypeScript 类型，字段详见 `.kiro/specs/115-cloud-drive-integration/requirements.md` 与 design.md §6.1。

- **ProblemDetails**：后端返回的 RFC 7807 标准错误响应格式（`application/problem+json`），含 `type/title/status/detail/instance/extensions`
- **CloudDriveFileDto**：文件/文件夹 DTO，含 `fileId/fileName/category/size/pickCode/thumb/originalUrl` 等
- **CloudDriveFileListDto**：文件列表响应封装，含 `items/path/count/offset/limit/hasMore`
- **CloudDriveBreadcrumbDto**：面包屑节点，含 `cid/pid/name`
- **CloudDriveRecycleItemDto**：回收站项目 DTO
- **CloudDriveVideoPlayDto**：视频播放信息 DTO，含 `videoUrls/availableDefinitions/status/vipUpgradeRequired`
- **CloudDriveSubtitleDto**：字幕信息 DTO（合并 `autoload` 与 `list`）
- **CloudDriveOfflineTaskDto**：云下载任务 DTO（`status` 归一化为字符串枚举）
- **CloudDriveOfflineQuotaDto**：云下载配额 DTO
- **CloudDriveShareDto**：分享信息 DTO
- **CloudDriveUserProfileDto**：用户聚合信息 DTO，含 `userInfo/vip/balance/storage` 四子模块
- **CloudDriveUserInfoDto**：用户基本信息 DTO（敏感字段已后端脱敏）
- **JobStatusDto**：后台任务状态 DTO，含 `jobId/type/status/progress`

### 通用缩写

- **jobId**：后端返回的长耗时任务标识（对应 `JobStatusDto.jobId`），用于 202 Accepted 响应的进度轮询
- **VIP**：115 网盘付费会员体系（原石会员/月费 VIP/年费 VIP/...）
- **HLS**：HTTP Live Streaming 视频流协议
- **CORS**：Cross-Origin Resource Sharing 跨域资源共享
- **Glassmorphism**：毛玻璃视觉效果，通过 CSS `backdrop-filter: blur()` 实现

## 需求

### 1. 登录态检测与会话管理

**用户故事：** 作为用户，我希望应用能够自动检测我的 115 登录状态，以便在已登录时直接使用网盘功能，未登录时获得引导提示。

#### 验收标准

1. WHEN 应用启动或刷新页面时，THE Frontend_App SHALL 调用 `GET /api/auth/check` 检测登录态
2. IF 响应返回 `{ isAuthenticated: true }`，THEN THE Frontend_App SHALL 调用 `GET /api/user/info` 获取当前用户昵称、头像等信息，并在顶栏展示
3. IF 响应返回 `{ isAuthenticated: false, failureReason: 'cookie_invalid' }` 或 `'network'`，THEN THE Frontend_App SHALL 展示引导页面，提示用户联系系统管理员更新 Cookie 或检查网络
4. WHILE 用户已登录时，THE Frontend_App SHALL 在顶栏或侧边栏展示当前用户头像与昵称
5. WHILE 用户已登录时，THE Frontend_App SHALL 展示存储空间用量摘要（已用/总空间），数值以 `GET /api/user/storage` 返回为准
6. IF 任何 API 请求返回 HTTP 401 ProblemDetails，THEN THE Frontend_App SHALL 清除敏感内存状态并跳转引导页，不展示 Toast，由 AuthGuard 统一处理

### 2. 文件浏览器

**用户故事：** 作为用户，我希望能够以列表或网格视图浏览网盘中的文件和文件夹，以便根据不同场景（密集列表 vs 可视化缩略图）高效浏览内容。

#### 验收标准

1. THE File_Browser SHALL 调用 `GET /api/files` 获取文件列表，支持 query 参数 `cid`/`offset`/`limit`/`o`/`asc`/`type`/`showDir`/`star`/`natsort`/`snap`/`countFolders`
2. THE File_Browser SHALL 将响应的 `CloudDriveFileDto[]` 渲染为文件/文件夹卡片或行
3. THE File_Browser SHALL 支持列表视图和网格视图切换，视图偏好持久化至 localStorage
4. THE File_Browser SHALL 展示文件名、大小、更新时间、缩略图（从 `thumb` 字段读取）、文件类型图标
5. THE File_Browser SHALL 提供面包屑导航，数据源于 `CloudDriveFileListDto.path`（即后端归一化的 `CloudDriveBreadcrumbDto[]`）
6. THE File_Browser SHALL 支持排序：按名称、大小、修改时间（升序/降序），通过 `o` 和 `asc` 参数传递
7. THE File_Browser SHALL 支持分页加载，`limit` 默认 50，不超过后端硬上限（由后端返回 400 时前端展示友好错误）
8. WHEN 用户双击文件夹时，THE File_Browser SHALL 进入子目录
9. WHEN 用户双击图片/视频/文档时，THE Frontend_App SHALL 打开对应预览器
10. WHEN 用户单击选择文件时，THE File_Browser SHALL 切换选中状态，支持 Ctrl/Shift 多选
11. WHEN 用户按下 `Ctrl+A` 时，THE File_Browser SHALL 全选当前视图内文件
12. WHEN 用户执行全选时，THE File_Browser SHALL 仅选择当前已加载项；若未完整加载，提示"已选择当前已加载项目"
13. WHEN 用户选中一个或多个文件时，THE File_Browser SHALL 在工具栏展示批量操作按钮（移动、复制、删除）
14. WHEN 用户选中的项目全部为文件（不含文件夹）时，THE File_Browser SHALL 在工具栏额外展示"逐个下载"按钮

### 3. 文件操作

**用户故事：** 作为用户，我希望能够对网盘文件进行上传、下载、新建文件夹、重命名、移动、复制和删除操作，以便高效管理我的网盘内容。

> **说明：** 上传链路已在 `docs/115-api-docs.md` 中完成采集，前端可通过 `POST /api/files/upload` 交给后端代理上传。下载地址、文件增删改、移动、复制、星标等 115 写操作仍以 integration spec 的采集状态为准；未实现时后端返回明确 ProblemDetails，前端展示统一错误提示。

#### 验收标准

1. THE Upload_Zone SHALL 支持拖拽上传，调用 `POST /api/files/upload`
2. THE Upload_Zone SHALL 支持点击选择文件上传
3. THE Frontend_App SHALL 提供"新建文件夹"按钮，调用 `POST /api/files/folders` _（待后端 API 就绪）_
4. WHEN 用户单击重命名时，THE Frontend_App SHALL 展示输入框，调用 `PATCH /api/files/{fileId}` _（待后端 API 就绪）_
5. WHEN 用户单击移动/复制时，THE Frontend_App SHALL 展示目录选择器，调用 `POST /api/files/move` 或 `POST /api/files/copy` _（待后端 API 就绪）_
6. WHEN 用户单击删除时，THE Confirm_Dialog SHALL 要求确认后调用 `DELETE /api/files`（请求体含 `fileIds[]`）_（待后端 API 就绪）_
7. WHEN 用户单击下载时，THE Frontend_App SHALL 调用 `GET /api/files/{pickCode}/download` 获取临时 URL，通过 `window.location` 或 `a.href` 触发浏览器下载
8. IF 后端返回 HTTP 202 Accepted（大文件异步上传），THEN THE Frontend_App SHALL 展示进度查询入口，并以 2 秒间隔调用 `GET /api/jobs/{jobId}` 轮询，直到任务进入 `completed` 或 `failed` 终态
9. IF 用户对多个文件执行下载，THEN THE Frontend_App SHALL 逐个顺序触发浏览器下载
10. IF 浏览器阻止连续下载（触发弹出拦截或批量下载保护），THEN THE Frontend_App SHALL 通过 Toast_Notification 提示"浏览器已阻止连续下载，请允许或手动下载"
11. IF 任一文件操作返回非 2xx ProblemDetails，THEN THE Frontend_App SHALL 通过 Toast_Notification 展示 `detail` 字段作为错误信息

### 4. 右键上下文菜单

**用户故事：** 作为用户，我希望能够通过右键菜单快速访问文件操作，以便减少鼠标移动并提升操作效率。

#### 验收标准

1. WHEN 用户在文件/文件夹上右键时，THE Context_Menu SHALL 在鼠标位置显示菜单
2. THE Context_Menu SHALL 根据对象类型展示相应操作：打开/重命名/移动/复制/删除/下载/分享/详情/星标
3. WHEN 菜单展开后距视口右/下边缘 <50px 时，THE Context_Menu SHALL 自动翻转至鼠标左/上侧显示，避免超出视口
4. THE Context_Menu SHALL 支持键盘方向键导航和 Enter 确认
5. IF 当前选中多个文件，THEN THE Context_Menu SHALL 展示批量操作项
6. WHEN 点击外部或按 Esc 时，THE Context_Menu SHALL 关闭
7. WHEN 点击"详情"时，THE Frontend_App SHALL 调用 `GET /api/files/{fileId}` 并展示返回字段；未返回字段以占位符展示

### 5. 文件搜索

**用户故事：** 作为用户，我希望能够搜索网盘中的文件，以便在海量文件中快速定位所需内容。

#### 验收标准

1. THE Search_Bar SHALL 位于工具栏中，提供搜索输入框
2. WHEN 用户输入关键词并回车时，THE Frontend_App SHALL 调用 `GET /api/files/search` 展示结果
3. WHILE 展示搜索结果时，THE File_Browser SHALL 显示"搜索结果：[关键词]"标识和"返回浏览"按钮
4. THE File_Browser SHALL 以与普通列表相同的视图模式展示搜索结果，每项额外显示文件所在路径
5. THE Search_Bar SHALL 支持按文件类型筛选搜索结果；后端 `type` 参数透传（1=文档, 2=图片, 3=音乐, 4=视频, 5=压缩包）
6. THE Search_Bar SHALL 支持按日期范围筛选，对应后端 `date` 参数（`YYYY-MM-DD` 格式）
7. THE Search_Bar SHALL 支持在当前目录或全局搜索切换，对应后端 `cid` 参数
8. WHEN 用户输入关键词长度 ≥2 字符时，THE Search_Bar SHALL 在 200ms 防抖后展示匹配的搜索历史记录与建议，历史记录存储于 localStorage 最多保留 20 条
9. IF 搜索关键词为空，THEN THE Search_Bar SHALL 提示"请输入搜索关键词"并阻止提交
10. IF 后端返回 HTTP 400（offset+limit 超限或关键词空白），THEN THE Frontend_App SHALL 通过 Toast_Notification 展示错误
11. WHEN 点击"返回浏览"时，THE File_Browser SHALL 恢复搜索前的目录状态

### 6. 回收站管理

**用户故事：** 作为用户，我希望能够查看和管理回收站中的文件，以便按需还原误删项目或彻底清理。

> **说明：** 回收站列表查询 API 已就绪；还原/彻底删除/清空操作依赖后端待补采集 API。

#### 验收标准

1. THE Recycle_Bin_View SHALL 调用 `GET /api/recycle` 展示回收站项目；响应为 `CloudDriveRecycleItemDto[]`
2. THE Recycle_Bin_View SHALL 展示文件名、大小、原始目录、删除时间（对应 `fileName`/`fileSize`/`originalParentName`/`deletedTime`）
3. WHEN 用户单击还原时，THE Frontend_App SHALL 调用 `POST /api/recycle/restore` _（待后端 API 就绪）_
4. WHEN 用户单击彻底删除时，THE Confirm_Dialog SHALL 要求确认后调用 `DELETE /api/recycle/{id}` _（待后端 API 就绪）_
5. WHEN 用户单击清空回收站时，THE Confirm_Dialog SHALL 要求确认后调用 `DELETE /api/recycle` _（待后端 API 就绪；响应约定：HTTP 202 Accepted + `{ jobId }` 用于后续轮询）_
6. THE Recycle_Bin_View SHALL 支持分页加载（`offset` + `limit`，最大 200）
7. IF 操作失败，THEN THE Frontend_App SHALL 通过 Toast_Notification 展示 ProblemDetails `detail` 字段

### 7. 图片预览

**用户故事：** 作为用户，我希望能够以灯箱模式预览图片文件，以便快速浏览图片而无需下载到本地。

#### 验收标准

1. WHEN 用户双击图片文件时，THE Image_Preview SHALL 以全屏灯箱展示，URL 源自 `CloudDriveFileDto.originalUrl` 或 `GET /api/files/{pickCode}/preview`
2. THE Image_Preview SHALL 支持 JPG/JPEG/PNG/GIF/BMP/WEBP 格式
3. THE Image_Preview SHALL 提供前后切换按钮和键盘左右方向键导航（同目录下的图片文件）
4. THE Image_Preview SHALL 支持缩放（鼠标滚轮或 +/- 键，范围 0.25x-5x，步进 0.25）和拖拽平移
5. THE Image_Preview SHALL 在顶部展示当前图片索引（如 "3 / 15"）和文件名
6. WHEN 按 Esc 或点击背景时，THE Image_Preview SHALL 关闭
7. WHEN 按 Space 键时，THE Image_Preview SHALL 切换全屏模式
8. IF 图片加载失败，THEN THE Image_Preview SHALL 自动重试一次（因 URL 可能过期）
9. IF 重试后仍失败，THEN THE Image_Preview SHALL 展示错误占位图和"重试"按钮

### 8. 视频播放器

**用户故事：** 作为用户，我希望能够在线播放视频文件，支持清晰度切换和字幕，以便在不同网络条件下获得最佳观看体验。

#### 验收标准

1. WHEN 用户点击视频文件时，THE Video_Player SHALL 以全屏模态展示播放器
2. THE Video_Player SHALL 基于 Video.js 实现，优先复用内置 HLS 支持
3. THE Video_Player SHALL 提供标准控制栏：播放/暂停、进度条、音量、全屏、播放速度（0.5x/1x/1.25x/1.5x/2x）
4. THE Video_Player SHALL 调用 `GET /api/videos/{pickCode}/play` 获取 `CloudDriveVideoPlayDto`，含 `videoUrls` 数组和 `availableDefinitions`
5. THE Video_Player SHALL 根据 `availableDefinitions` 提供清晰度选择菜单；枚举：SD=1/HD=2/FHD=3/FullHD=4/UHD4K=5/Original=100
6. WHEN 切换清晰度时，THE Video_Player SHALL 保持播放进度
7. THE Video_Player SHALL 调用 `GET /api/videos/{pickCode}/subtitles` 获取字幕列表，展示 `language/title`；支持切换
8. IF 无可用字幕，THEN THE Video_Player SHALL 隐藏字幕控件或展示"无字幕"，不视为错误
9. IF 后端返回 `status: 'transcoding'`，THEN THE Video_Player SHALL 展示"视频转码中，请稍后刷新"提示和"刷新状态"按钮
10. IF 后端返回 `vipUpgradeRequired: true`，THEN THE Video_Player SHALL 展示"升级 VIP 以观看此清晰度"引导
11. WHEN 用户按 Esc 或关闭播放器时，THE Video_Player SHALL 停止播放并关闭
12. WHEN `GET /api/videos/{pickCode}/history` 返回有效播放进度时，THE Video_Player SHALL 从上次位置继续观看
13. IF 无播放历史记录或 API 返回 404，THEN THE Video_Player SHALL 从 00:00 开始播放
14. THE Frontend_App SHALL 每 30 秒调用 `POST /api/videos/{pickCode}/history` 保存播放进度

### 9. 文档预览

**用户故事：** 作为用户，我希望能够在线预览 PDF 和文本文件，以便快速阅读文档而无需下载。

#### 验收标准

1. WHEN 用户点击 PDF 或 TXT 文件时，THE Document_Preview SHALL 调用 `GET /api/documents/{pickCode}/preview` 获取预览地址
2. IF 响应 `renderMode: 'Direct'`（PDF/TXT），THEN THE Document_Preview SHALL 使用 `<iframe>` 或内置 PDF.js 渲染
3. IF 响应 `renderMode: 'ThirdParty'`（Office 格式），THEN THE Document_Preview SHALL 集成第三方渲染库（待配置）
4. IF 响应 `renderMode: 'NotAvailable'`，THEN THE Document_Preview SHALL 展示"预览服务未配置，请下载后查看"
5. IF 文档加载失败，THEN THE Document_Preview SHALL 自动重试一次
6. IF 重试后仍失败，THEN THE Document_Preview SHALL 展示"重试"和"下载"按钮

### 10. [已删除] 全局任务面板

> 根据架构决策，全局任务面板（Task_Panel）、JobPollingService、JobStore 概念已完全移除。云下载任务仍在云下载页面（需求 15）展示；大文件上传、清空回收站等后台任务通过 `GET /api/jobs/{jobId}` 按需轮询。

### 11. 暗色主题与 Glassmorphism

**用户故事：** 作为用户，我希望界面采用现代化暗色主题和毛玻璃效果设计，以便获得视觉舒适且符合审美预期的使用体验。

#### 验收标准

1. THE Frontend_App SHALL 使用暗色主题作为默认主题
2. THE Frontend_App SHALL 对关键卡片、模态框、面包屑使用 Glassmorphism 效果（`backdrop-filter: blur()`）
3. THE Frontend_App SHALL 在 `:root` 定义统一的 CSS 变量色板，至少包含：`--bg-base`、`--bg-surface`、`--text-primary`、`--text-secondary`、`--border-default`、`--success`、`--warning`、`--error`、`--info`（完整清单以 design §4.10 为准）
4. THE Frontend_App SHALL 在交互元素上使用平滑过渡动画（不超过 300ms）
5. THE Theme_Controller SHALL 支持主题切换（暗色/亮色），偏好保存至 localStorage
6. THE Frontend_App SHALL 对文件类型使用差异化颜色编码图标
7. IF 用户启用减少动态效果偏好（`prefers-reduced-motion`），THEN THE Frontend_App SHALL 降级或禁用非必要动画

### 12. 响应式布局

**用户故事：** 作为用户，我希望界面在不同屏幕尺寸上正常使用，以便在桌面、平板和移动设备间无缝切换。

#### 验收标准

1. THE Frontend_App SHALL 在桌面（≥1024px）、平板（768-1023px）、移动端（<768px）均可使用
2. THE File_Browser SHALL 根据屏幕尺寸自动调整列表/网格布局（调整策略详见 12.5）
3. WHEN 屏幕宽度 <1024px 时，THE Frontend_App SHALL 折叠侧边栏为抽屉形式
4. THE Context_Menu SHALL 在移动端以底部弹出菜单代替悬浮菜单
5. THE File_Browser SHALL 在网格视图下自动调整每行卡片数（桌面 ≥5 列、平板 3-4 列、移动 2 列）
6. WHILE 屏幕宽度 <1024px 时，THE Image_Preview、Video_Player、Document_Preview SHALL 以全屏模态展示，忽略桌面断点的尺寸约束

### 13. 键盘快捷键

**用户故事：** 作为用户，我希望通过键盘快捷键快速操作，以便在熟练使用后大幅提升操作效率。

#### 验收标准

1. THE Keyboard_Manager SHALL 支持以下全局快捷键：`Ctrl+F` 聚焦搜索、`Ctrl+A` 全选、`Delete` 删除、`F2` 重命名、`Esc` 关闭模态框
2. THE Keyboard_Manager SHALL 支持文件浏览器方向键导航（上下左右选择文件，Enter 打开）
3. THE Keyboard_Manager SHALL 支持 Backspace 返回上级目录
4. THE Image_Preview SHALL 支持左右方向键切换图片
5. THE Video_Player SHALL 支持以下快捷键：Space 播放/暂停；左/右方向键快退/快进 10 秒；上/下方向键调节音量（每次 ±5%）
6. IF 处于可编辑状态，THEN THE Keyboard_Manager SHALL 暂停文件操作快捷键
7. IF File_Browser 未获焦或焦点在输入控件内，THEN THE Keyboard_Manager SHALL 禁用文件浏览器快捷键，并阻止 Backspace 触发浏览器后退

### 14. 加载状态与错误处理

**用户故事：** 作为用户，我希望在加载和出错时看到友好提示，以便清楚了解系统状态并做出相应操作。

#### 验收标准

1. WHILE 数据加载中，THE Skeleton_Loader SHALL 展示骨架屏
2. THE Frontend_App SHALL 使用 Toast_Notification 展示非阻断性提示（成功/错误/警告/信息）
3. THE Frontend_App SHALL 支持取消请求（`AbortController`），切换页面或关闭组件时取消未完成请求
4. IF API 返回 HTTP 5xx ProblemDetails，THEN THE Frontend_App SHALL 展示 Error_State 组件和"重试"按钮
5. IF 网络异常（请求超过 30 秒 / 30000ms 未响应、HTTP 无响应或 CORS 错误），THEN THE Frontend_App SHALL 展示"网络异常，请检查连接"并提供重试入口
6. THE Frontend_App SHALL 在页面切换时保留搜索关键词、视图模式、排序方式等用户偏好
7. THE Frontend_App SHALL 在登录态过期（HTTP 401）时全局拦截，跳转引导页并清除敏感内存状态
8. WHILE 文件操作执行中，THE Frontend_App SHALL 展示进度指示
9. IF 有前台进行中操作（如上传）且用户尝试离开页面，THEN THE Frontend_App SHALL 展示离开保护提示

### 15. 云下载管理

**用户故事：** 作为用户，我希望查看、创建和管理云下载任务，以便将大文件或种子链接直接下载到网盘。

#### 验收标准

1. THE Offline_Task_Panel SHALL 调用 `GET /api/offline/tasks?page=1` 展示任务列表
2. THE Offline_Task_Panel SHALL 调用 `GET /api/offline/quota` 展示配额信息（`count/surplus/maxSize/used`）
3. THE Offline_Task_Panel SHALL 调用 `GET /api/offline/stats` 展示任务统计（`total/active/completed/failed`）
4. THE Offline_Task_Panel SHALL 渲染 `CloudDriveOfflineTaskDto[]`，展示任务名、大小、进度、状态（后端已归一化为 `'failed'|'allocating'|'downloading'|'success'`）、添加时间、清晰度（视频任务）
5. WHEN 用户添加 URL 任务时，THE Frontend_App SHALL 调用 `POST /api/offline/tasks/urls`（请求体 `{ urls: string[], saveDirectoryId? }`）_（待后端 API 就绪）_
6. WHEN 用户添加 BT 种子任务时，THE Frontend_App SHALL 先上传种子调用 `POST /api/offline/parse-torrent`，然后调用 `POST /api/offline/tasks/bt`（请求体 `{ infoHash, wanted, savePath, torrentSha1, pickCode, saveDirectoryId? }`）_（待后端 API 就绪）_
7. WHEN 用户删除任务时，THE Frontend_App SHALL 调用 `DELETE /api/offline/tasks/{infoHash}` _（待后端 API 就绪）_
8. WHEN 用户清空任务时，THE Frontend_App SHALL 调用 `POST /api/offline/tasks:clear`，后端返回 202 + jobId，前端轮询进度 _（待后端 API 就绪）_
9. THE Offline_Task_Panel SHALL 在任务进行中时自动每 10 秒刷新列表
10. WHEN 任务操作成功后，THE Offline_Task_Panel SHALL 刷新任务列表和统计
11. THE Offline_Task_Panel SHALL 在任务列表为空时展示空状态，并在接口失败时展示错误和重试入口

### 16. 分享管理

**用户故事：** 作为用户，我希望查看分享、发送和接收记录，以便追踪已创建的分享和他人发来的文件。

#### 验收标准

1. THE Share_View SHALL 提供三个 Tab：我的分享、已发送、已接收
2. WHEN 用户切换 Tab 时，THE Frontend_App SHALL 调用 `GET /api/shares?tab={slist|send|receive}&offset&limit`
3. THE Share_View SHALL 渲染 `CloudDriveShareDto[]`，展示分享名称、数量、创建时间、状态、有效期
4. THE Share_View SHALL 支持分页加载（`offset` + `limit`）
5. IF 后端返回 400 错误（tab 参数不支持等），THEN THE Share_View SHALL 参考 R18 通用错误处理策略展示 ProblemDetails `detail` 字段
6. IF 返回可复制的 shareUrl 或提取码，THEN THE Share_View SHALL 提供复制操作
7. THE Share_View SHALL 在无数据时展示空状态，并在接口失败时展示错误和重试入口

### 17. 用户中心

**用户故事：** 作为用户，我希望查看个人信息、VIP 状态、账户余额和存储空间详情，以便了解账号状态并规划存储使用。

#### 验收标准

1. THE Frontend_App SHALL 在侧边栏或顶栏用户头像处提供"用户中心"入口
2. THE User_Center_View SHALL 调用 `GET /api/user/profile` 聚合获取 `CloudDriveUserProfileDto`，或按需调用细粒度端点（`/info`/`/vip`/`/balance`/`/storage`）
3. THE User_Center_View SHALL 展示用户基本信息（昵称、头像、UID、脱敏手机/邮箱），从 `CloudDriveUserInfoDto` 读取
4. THE User_Center_View SHALL 展示 VIP 状态信息（VIP 等级、有效期、剩余空间）
5. THE User_Center_View SHALL 展示账户余额（云币、优惠券等，从后端归一化结果读取）
6. THE User_Center_View SHALL 展示存储空间详情（已用/总空间/各类型占比）
7. WHILE 展示空间占用时，THE User_Center_View SHALL 优先使用后端返回的格式化字段（如 `sizeFormat`）而非字节数值 `size` 自行格式化
8. THE Frontend_App SHALL **严禁**在用户中心展示后端已过滤的敏感字段；若发现后端响应意外包含此类字段，前端 SHALL 在开发环境记录告警
9. IF 聚合接口的任一子模块失败（后端返回对应字段为 `null`），THEN THE User_Center_View SHALL 在对应区域展示错误提示和重试按钮，不影响其他区域展示

### 18. 通用错误处理策略

**用户故事：** 作为用户，我希望在出现系统性错误时获得有指引性的提示而非跳变式信息，以便在错误发生时知晓如何处理而不感到困惑。

#### 验收标准

1. THE Frontend_App SHALL 统一消费后端 `application/problem+json` ProblemDetails 格式响应
2. THE Frontend_App SHALL 解析 ProblemDetails 字段：`title`（用于 Toast 标题）、`detail`（用于详细描述）、`status`（用于分级）、`extensions.upstreamCode`（开发环境调试）
3. IF HTTP 401（登录态过期），THEN THE Frontend_App SHALL 跳转引导页，不展示 Toast（由 AuthGuard 统一处理）
4. IF HTTP 404，THEN THE Frontend_App SHALL 展示"资源不存在"或具体 `detail` 信息
5. IF HTTP 410 Gone（分享过期），THEN THE Frontend_App SHALL 展示"分享已失效"
6. IF HTTP 429 Too Many Requests，THEN THE Frontend_App SHALL 展示"请求过于频繁，请稍后重试"
7. IF HTTP 5xx，THEN THE Frontend_App SHALL 展示通用"服务异常，请稍后重试"，不暴露 `extensions.upstreamCode`
8. IF 网络异常或 CORS 错误，THEN THE Frontend_App SHALL 展示"网络异常，请检查连接"
9. THE Frontend_App SHALL 对所有错误路径记录结构化日志（时间、接口、状态码、`extensions.upstreamCode`）
10. THE Frontend_App SHALL 对结构化日志中涉及个人敏感信息（手机号、邮箱、Cookie、令牌）的字段严禁写入，即使开发环境也不例外
11. THE Frontend_App SHALL 对同一业务操作的重试次数进行限制（默认 1 次），避免无限循环重试放大错误

### 19. 前端架构与契约边界

**用户故事：** 作为用户，我希望前端仅承担展示与交互职责，并稳定依赖后端 integration 服务提供的统一契约，以便前后端边界清晰、实现一致且易于维护。

#### 验收标准

1. THE Frontend_App SHALL 基于 `Vue 3 + TypeScript + Vite` 构建，并使用 `Pinia`、`Vue Router`、`Naive UI`、`Tailwind CSS`、`Axios`、`Video.js` 与 `PDF.js` 作为当前版本基础设施
2. THE Frontend_App SHALL 仅通过后端 integration 服务提供的 `/api/*` REST 端点消费数据，不直连 115 网页端原生 API
3. THE Frontend_App SHALL 将 Cookie 注入、签名生成、数据归一化、频控与错误字段适配完全交由后端 integration 服务处理
4. THE Frontend_App SHALL 使用单账号模型，不提供多账号切换、OAuth、PKCE 或前端代理层，也不得向 Cloud Drive API 显式传递 `CloudAccountUid`
5. THE Frontend_App SHALL 以 `.kiro/specs/115-cloud-drive-integration/requirements.md` 与 `/openapi/v1.json` 作为 API 契约权威来源，并自动生成与之同步的 TypeScript 类型
6. THE Frontend_App SHALL 仅消费后端归一化后的 camelCase DTO 与 ProblemDetails 错误响应，不解析 115 网页端短字段或错误格式变体
7. THE Backend_API SHALL 对会话 Cookie 设置 `HttpOnly`、`Secure` 与 `SameSite=Lax` 或 `SameSite=Strict`；IF 后端因明确跨站部署场景使用 `SameSite=None`，THEN THE Backend_API SHALL 同时设置 `Secure` 并在 integration spec 中记录原因
8. THE Backend_API SHALL 对 `POST`、`PUT`、`PATCH`、`DELETE` 等状态变更请求实施 CSRF 防护（如后端校验 CSRF Token、`Origin`/`Referer` 或 Fetch Metadata）；THE Frontend_App SHALL 仅按 `/openapi/v1.json` 暴露的 CSRF 契约携带凭据，不自造 CSRF Token 或读取会话 Cookie

## 范围排除

以下能力不纳入当前迭代，后续版本视需求补充：

| 能力 | 排除原因 |
|------|----------|
| 视频多音轨切换 | 后端 API 未采集，低频功能 |
| 视频记忆清晰度/旋转/翻转 | 后端暂不存储用户偏好 |
| 搜索按文件/文件夹筛选（`fc` 参数） | 后端 integration spec 未启用 |
| 搜索按文件标签（`file_label`） | 标签功能整体列入后续迭代 |
| 搜索 type=6 应用筛选 | 低频类型 |
| 标签管理 | 后端 `/label/list` 端点未纳入 integration spec |
| 设置页 | M8 模块，后续迭代 |
| 消息/活动/通讯录 | M9 模块，已在 integration spec 排除声明中显式排除 |

> **后端 API 范围排除**：前端不应调用 integration spec 中已显式排除的端点（`webapi.115.com/label/list`、`webapi.115.com/user/fingerprint`、`act.115.com/*`、`msg.115.com/*` 等 13 个）。详见 `.kiro/specs/115-cloud-drive-integration/requirements.md` 的"官方 API 范围排除声明"。
