# 需求文档：115 网盘前端展示

## 简介

本功能为 115 网盘集成项目的前端展示层，基于后端已提供的 OAuth 授权、文件管理、预览播放、后台任务等 API 能力，构建一套现代化、酷炫且易操作的 Web 前端界面。视觉风格采用暗色主题（Dark Mode）结合 Glassmorphism（毛玻璃效果），提供沉浸式的文件管理体验。前端技术栈基于 Vue 3 + TypeScript，使用 Video.js 实现视频播放并优先复用其内置 HLS 支持，仅在兼容性或播放器封装场景确有需要时再补充 HLS.js，支持响应式布局与键盘快捷键操作。

> **依赖说明：** 本前端需求文档中所有数据交互均依赖 `.kiro/specs/115-cloud-drive-integration/` 中定义的后端 API 接口，前端不直接调用 115 开放平台 API。

> **能力与资源边界：** 前端仅消费后端返回的官方支持能力与临时授权资源；下载、预览、播放地址均可能过期并需要重新获取；视频清晰度、字幕与转码状态以后台实际返回为准；Office 第三方预览仅在管理员已配置相应服务时可用。

> **账号上下文边界：** 当前后端已提供多账号绑定与已绑定账号列表能力；前端所有授权后的网盘请求均基于当前 `CloudAccountUid` 上下文执行，并在界面中明确展示当前账号；即使账号列表仅返回单个账号，前端仍不得隐式推断默认账号。

> **列表预览边界：** 文件浏览器中的图片直显与视频列表预览均以后端返回的缩略图或其他明确用于列表预览的字段为准；前端不得自行解析原始视频生成封面，也不得为展示缩略图而主动拉取完整原图或原视频资源。

## 术语表

- **Frontend_App**：115 网盘前端应用，负责用户界面渲染与交互
- **File_Browser**：文件浏览器组件，负责展示文件列表、网格视图、面包屑导航
- **Upload_Zone**：拖拽上传区域组件，负责文件拖拽上传交互与进度展示
- **Context_Menu**：右键上下文菜单组件，负责文件操作的快捷菜单
- **Image_Viewer**：图片预览组件，负责灯箱/画廊模式的图片浏览
- **Video_Player**：视频播放器组件，负责多清晰度视频播放与字幕展示
- **Doc_Viewer**：文档预览组件，负责 PDF/TXT 内嵌渲染与 Office 格式第三方预览
- **Task_Panel**：后台任务进度面板组件，负责展示后台任务状态与进度
- **Search_Bar**：搜索栏组件，负责文件搜索交互
- **Recycle_Bin_View**：回收站管理视图，负责回收站文件的展示与操作
- **Auth_Panel**：授权管理面板，负责 OAuth 授权连接与状态展示
- **Toast_Notification**：轻提示通知组件，负责操作结果的即时反馈
- **Skeleton_Loader**：骨架屏加载组件，负责数据加载中的占位展示
- **Error_State**：错误状态组件，负责错误信息的友好展示与重试引导

## 需求

### 需求 1：OAuth 授权连接交互

**用户故事：** 作为用户，我希望能够在前端界面中连接和管理我的 115 网盘授权，以便查看授权状态并随时连接或断开网盘账号。

#### 验收标准

1. WHEN 用户未授权 115 网盘时，THE Auth_Panel SHALL 展示"连接 115 网盘"按钮，按钮使用 Glassmorphism 风格卡片呈现，包含 115 网盘图标和简要说明文字
2. WHEN 用户点击"连接 115 网盘"按钮时，THE Frontend_App SHALL 以 PKCE 会话模式作为默认授权入口：调用后端创建授权会话接口，保存返回的 `pkceSessionId`，并基于返回的授权地址信息将用户重定向至 115 开放平台授权页面；直接获取授权 URL 的接口 MAY 作为兼容能力保留，但不得作为默认流程
3. WHEN 用户完成授权并通过回调返回时，THE Frontend_App SHALL 调用后端令牌交换接口完成授权流程；在默认 PKCE 流程下，前端 SHALL 携带 `code` 与已保存的 `pkceSessionId` 完成交换，并保存返回的 `CloudAccountUid` 作为当前账号上下文，再更新界面为已授权状态
4. WHILE 用户已授权时，THE Auth_Panel SHALL 展示当前所选授权账号的昵称和头像，以及"断开连接"按钮
5. WHEN 用户点击"断开连接"按钮时，THE Frontend_App SHALL 弹出确认对话框，用户确认后调用后端断开接口并将界面恢复为未授权状态
6. IF 授权流程失败（授权码交换失败或回调参数异常），THEN THE Frontend_App SHALL 通过 Toast_Notification 展示包含错误原因的提示信息，并引导用户重试
7. IF 后端返回需要重新授权的提示（令牌失效），THEN THE Frontend_App SHALL 自动进入授权连接界面，并通过 Toast_Notification 提示用户"授权已过期，请重新连接"；该授权连接界面 MAY 通过保持当前 URL 并由布局容器渲染授权引导态承载，不强制要求额外路由跳转
8. IF 后端返回多个已绑定的 115 账号，THEN THE Auth_Panel SHALL 展示当前账号标识与账号切换控件，用户切换后 THE Frontend_App SHALL 使用选中的 `CloudAccountUid` 重新拉取当前账号信息、文件列表与任务数据
9. WHEN 应用启动、刷新页面或进入受保护路由时，THE Frontend_App SHALL 调用后端已绑定账号列表接口恢复可用账号集合；若已保存的 `currentAccountUid` 仍存在，则优先恢复该账号；否则在账号列表非空时选择首个账号并拉取其用户信息；即使账号列表仅返回单个账号，前端仍 SHALL 维护显式的当前 `CloudAccountUid`，并在授权后的接口调用中携带该上下文，不得隐式假定默认账号
10. IF 后端返回 115 网盘功能未配置或已禁用，THEN THE Frontend_App SHALL 在授权入口区域展示不可用状态说明，隐藏或禁用连接操作，并提示用户联系管理员或稍后再试；该状态可由 `Auth_Panel` 或等效的功能不可用状态组件承载
11. WHILE 用户已授权时，THE Frontend_App SHALL 在布局顶栏、侧边栏或等效的账号概览区域展示当前账号的存储空间用量摘要；已用空间、总空间、剩余空间或进度条等展示数值必须以后端返回的空间统计结果为准，不得由前端推算

### 需求 2：文件浏览器

**用户故事：** 作为用户，我希望能够以列表或网格视图浏览网盘中的文件和文件夹，以便直观地查看和管理我的网盘内容。

#### 验收标准

1. THE File_Browser SHALL 支持列表视图和网格视图两种展示模式，用户可通过工具栏中的视图切换按钮自由切换
2. WHILE 处于列表视图时，THE File_Browser SHALL 以表格形式展示预览缩略图（如适用）、文件名称、大小、类型、修改时间，文件夹排列在文件之前
3. WHILE 处于网格视图时，THE File_Browser SHALL 以卡片网格形式展示文件预览图；图片文件应直接展示图片缩略图，视频文件在后端返回可用列表预览时应展示对应缩略图，其他文件类型在无可用预览图时回退为类型图标；卡片使用 Glassmorphism 风格
4. THE File_Browser SHALL 在顶部展示面包屑导航栏，显示当前目录的完整路径层级，每个层级可点击跳转
5. WHEN 用户点击面包屑中的某一层级时，THE File_Browser SHALL 导航至该目录并刷新文件列表
6. WHEN 用户双击文件夹时，THE File_Browser SHALL 进入该文件夹并更新面包屑导航
7. THE File_Browser SHALL 提供按名称、大小、修改时间进行排序的控件
8. WHEN 后端提供服务端排序能力时，THE Frontend_App SHALL 透传排序参数并以服务端返回顺序为准
9. IF 当前接口未提供服务端排序能力，THEN THE File_Browser SHALL 仅对当前已加载结果执行本地排序，不得暗示已对整个目录完成全量排序
10. THE File_Browser SHALL 提供按文件类型（全部、文件夹、图片、视频、文档、音频、其他）进行筛选控件
11. WHEN 后端提供服务端筛选能力时，THE Frontend_App SHALL 以服务端筛选结果为准；否则仅对当前已加载结果进行本地筛选，不得暗示已对整个目录完成全量过滤
12. THE File_Browser SHALL 支持分页加载或无限滚动加载，当文件数量较多时自动加载下一页数据
13. WHILE 使用无限滚动加载时，THE Frontend_App SHALL 对翻页请求实施阈值触发、防抖和重复请求抑制，避免高频重复加载
14. WHILE 文件列表或网格加载图片缩略图与视频列表预览时，THE File_Browser SHALL 仅对进入视口或接近视口的项目采用懒加载与视口优先策略，优先使用后端返回的缩略图或其他明确用于列表预览的字段；无可用预览图时回退为类型图标，不得主动预取完整原图或原视频资源
15. IF 某个图片缩略图或视频列表预览加载失败，THEN THE File_Browser SHALL 仅将该项回退为类型图标或占位图，不得阻塞整个列表渲染；在用户刷新目录后可重新尝试加载
16. WHEN 用户点击工具栏中的"刷新"按钮时，THE File_Browser SHALL 先调用后端目录刷新接口，再重新拉取绕过缓存的当前目录最新列表数据并更新展示
17. THE File_Browser SHALL 支持多选操作：用户可通过 Ctrl+点击选择多个文件，或通过 Shift+点击选择连续范围的文件
18. WHEN 用户执行"全选"类操作时，THE File_Browser SHALL 仅选择当前目录中当前已加载的结果项；若目录尚未完整加载，界面 SHALL 明确提示"已选择当前已加载项目"
19. WHEN 用户选中一个或多个文件时，THE File_Browser SHALL 在工具栏中展示批量操作按钮（移动、复制、删除）；当所选项目均为文件时可额外展示"逐个下载"
20. WHEN 用户点击工具栏中的"新建文件夹"按钮时，THE File_Browser SHALL 在当前目录中展示一个可编辑的新文件夹行/卡片，用户输入名称后按回车确认创建
21. WHEN 用户双击文件名称（或选择重命名操作）时，THE File_Browser SHALL 将文件名称切换为可编辑状态，用户修改后按回车确认或按 Esc 取消
22. IF 新建文件夹或重命名时用户输入的名称为空或包含非法字符，THEN THE Frontend_App SHALL 在输入框下方展示红色错误提示文字，阻止提交
23. WHEN 用户选择移动操作时，THE Frontend_App SHALL 弹出目录选择对话框，用户选择目标目录后确认执行移动
24. WHEN 用户选择复制操作时，THE Frontend_App SHALL 弹出目录选择对话框，用户选择目标目录后确认执行复制
25. WHEN 用户选择删除操作时，THE Frontend_App SHALL 弹出确认对话框，明确提示"文件将移至回收站"，用户确认后执行删除
26. WHEN 文件操作（新建、重命名，以及同步完成的上传、移动、复制、删除）成功后，THE File_Browser SHALL 自动刷新当前目录的文件列表
27. IF 后端对上传、批量移动、批量复制或批量删除返回任务受理状态或 `JobId`，THEN THE Frontend_App SHALL 将该任务加入 Task_Panel、展示"任务已开始"提示，并在任务完成后再刷新受影响目录
28. IF 用户对多个文件执行下载，THEN THE Frontend_App SHALL 逐个调用单文件下载接口并依次触发浏览器下载，不得假定后端提供打包下载能力；若浏览器阻止连续下载，前端 SHALL 给出明确提示
29. IF 文件操作失败，THEN THE Frontend_App SHALL 通过 Toast_Notification 展示包含错误原因的提示信息

### 需求 3：文件操作交互

**用户故事：** 作为用户，我希望能够通过直观的交互方式对网盘文件进行上传、下载、新建文件夹、重命名、移动、复制和删除操作，以便高效管理我的网盘文件。

#### 验收标准

1. THE Upload_Zone SHALL 在文件浏览器区域提供拖拽上传功能：当用户将文件拖入浏览器区域时，展示全屏半透明的拖拽提示覆盖层，包含上传图标和"释放文件以上传"文字
2. WHEN 用户释放拖拽的文件时，THE Upload_Zone SHALL 调用后端上传接口开始上传，并根据后端执行模式展示进度：同步上传显示即时上传进度，后台受理上传任务时在 Task_Panel 中展示任务进度
3. IF 后端对上传请求立即返回 `Accepted` 或 `JobId`，THEN THE Frontend_App SHALL 将上传切换为后台任务展示，并允许用户在任务执行期间继续浏览当前目录
4. THE File_Browser SHALL 在工具栏中提供"上传文件"按钮，点击后打开系统文件选择对话框，支持多文件选择
5. WHEN 用户请求下载单个文件时，THE Frontend_App SHALL 调用后端下载接口获取临时下载 URL 或受控代理下载信息，并触发浏览器下载
6. IF 下载链接因临时资源过期而失效，THEN THE Frontend_App SHALL 重新调用后端下载接口获取最新地址并自动重试一次
7. WHEN 用户点击工具栏中的"新建文件夹"按钮时，THE File_Browser SHALL 在当前目录中展示一个可编辑的新文件夹行/卡片，用户输入名称后按回车确认创建
8. WHEN 用户双击文件名称（或选择重命名操作）时，THE File_Browser SHALL 将文件名称切换为可编辑状态，用户修改后按回车确认或按 Esc 取消
9. IF 新建文件夹或重命名时用户输入的名称为空或包含非法字符，THEN THE Frontend_App SHALL 在输入框下方展示红色错误提示文字，阻止提交
10. WHEN 用户选择移动操作时，THE Frontend_App SHALL 弹出目录选择对话框，用户选择目标目录后确认执行移动
11. WHEN 用户选择复制操作时，THE Frontend_App SHALL 弹出目录选择对话框，用户选择目标目录后确认执行复制
12. WHEN 用户选择删除操作时，THE Frontend_App SHALL 弹出确认对话框，明确提示"文件将移至回收站"，用户确认后执行删除
13. WHEN 文件操作（新建、重命名，以及同步完成的上传、移动、复制、删除）成功后，THE File_Browser SHALL 自动刷新当前目录的文件列表
14. IF 后端对上传、批量移动、批量复制或批量删除返回任务受理状态或 `JobId`，THEN THE Frontend_App SHALL 将该任务加入 Task_Panel、展示"任务已开始"提示，并在任务完成后再刷新受影响目录
15. IF 用户对多个文件执行下载，THEN THE Frontend_App SHALL 逐个调用单文件下载接口并依次触发浏览器下载，不得假定后端提供打包下载能力；若浏览器阻止连续下载，前端 SHALL 给出明确提示
16. IF 文件操作失败，THEN THE Frontend_App SHALL 通过 Toast_Notification 展示包含错误原因的提示信息

### 需求 4：右键上下文菜单

**用户故事：** 作为用户，我希望能够通过右键菜单快速访问文件操作，以便提高操作效率。

#### 验收标准

1. WHEN 用户在文件或文件夹上右键点击时，THE Context_Menu SHALL 展示与该文件类型相关的操作菜单，菜单使用 Glassmorphism 风格半透明背景
2. THE Context_Menu SHALL 根据文件类型动态展示菜单项：文件夹显示"打开、重命名、移动、复制、删除"；图片文件额外显示"预览"；视频文件额外显示"播放"；文档文件额外显示"预览"；所有文件显示"下载、详情"
3. WHEN 用户选中多个文件后右键点击时，THE Context_Menu SHALL 展示批量操作菜单项："移动、复制、删除"；当所选项目均为文件时可额外展示"逐个下载"
4. WHEN 用户在文件浏览器空白区域右键点击时，THE Context_Menu SHALL 展示目录级操作菜单项："新建文件夹、上传文件、刷新、粘贴"；"粘贴"仅在 Frontend_App 内部剪贴板存在由复制或剪切操作产生的待处理项目时显示
5. WHEN 用户点击菜单项时，THE Context_Menu SHALL 关闭菜单并执行对应操作
6. WHEN 用户点击菜单外部区域或按 Esc 键时，THE Context_Menu SHALL 关闭菜单
7. WHEN 用户点击"详情"菜单项时，THE Frontend_App SHALL 打开详情抽屉或模态，展示后端返回的文件元数据；对后端未返回的字段 SHALL 以空值或占位符展示，不得伪造元数据

### 需求 5：文件搜索

**用户故事：** 作为用户，我希望能够通过搜索快速找到网盘中的文件，以便在大量文件中高效定位目标内容。

#### 验收标准

1. THE Search_Bar SHALL 位于文件浏览器工具栏中，提供搜索输入框，支持输入关键词搜索
2. WHEN 用户在搜索框中输入关键词并按回车（或点击搜索按钮）时，THE Frontend_App SHALL 调用后端搜索接口，并将搜索结果展示在文件浏览器区域
3. WHILE 搜索结果展示中，THE File_Browser SHALL 在面包屑导航区域显示"搜索结果：[关键词]"标识，并提供"返回浏览"按钮
4. THE File_Browser SHALL 以与普通文件列表相同的视图模式（列表/网格）展示搜索结果，每个结果项额外显示文件所在路径
5. WHEN 搜索无匹配结果时，THE File_Browser SHALL 展示空状态插图和"未找到匹配文件"提示文字
6. IF 搜索关键词为空时用户提交搜索，THEN THE Frontend_App SHALL 在搜索框下方展示"请输入搜索关键词"提示，阻止提交空搜索
7. WHEN 用户点击"返回浏览"按钮时，THE File_Browser SHALL 恢复搜索前的目录浏览状态

### 需求 6：回收站管理界面

**用户故事：** 作为用户，我希望能够查看和管理回收站中的文件，以便恢复误删文件或彻底清理不需要的内容。

#### 验收标准

1. THE Frontend_App SHALL 在侧边栏或导航区域提供"回收站"入口，点击后进入 Recycle_Bin_View
2. THE Recycle_Bin_View SHALL 以列表形式展示回收站中的文件，包含文件名称、原始路径、删除时间、文件大小
3. THE Recycle_Bin_View SHALL 支持选择单个或多个文件进行还原或彻底删除操作
4. WHEN 用户选择还原操作时，THE Frontend_App SHALL 调用后端还原接口；若后端同步完成，则从回收站列表中移除该文件并通过 Toast_Notification 提示"文件已还原"；IF 后端对大批量还原返回任务受理状态或 `JobId`，THEN THE Frontend_App SHALL 将该任务加入 Task_Panel、展示"任务已开始"提示，并在任务完成后刷新回收站列表
5. WHEN 用户选择彻底删除操作时，THE Frontend_App SHALL 弹出确认对话框，明确提示"文件将被永久删除且无法恢复"，用户确认后执行删除；IF 后端对大批量彻底删除返回任务受理状态或 `JobId`，THEN THE Frontend_App SHALL 将该任务加入 Task_Panel、展示"任务已开始"提示，并在任务完成后刷新回收站列表
6. THE Recycle_Bin_View SHALL 在工具栏中提供"清空回收站"按钮，点击后弹出确认对话框，用户确认后调用后端清空接口；该操作由后台任务执行
7. WHEN 回收站为空时，THE Recycle_Bin_View SHALL 展示空状态插图和"回收站为空"提示文字
8. WHEN 后端返回清空回收站任务的 `JobId` 时，THE Frontend_App SHALL 将该任务加入 Task_Panel、展示"回收站清理已开始"提示，并在任务完成后刷新回收站列表
9. IF 回收站操作失败，THEN THE Frontend_App SHALL 通过 Toast_Notification 展示包含错误原因的提示信息

### 需求 7：图片预览

**用户故事：** 作为用户，我希望能够以灯箱/画廊模式预览网盘中的图片文件，以便无需下载即可浏览图片内容。

#### 验收标准

1. WHEN 用户点击图片文件（或选择预览操作）时，THE Image_Viewer SHALL 以全屏灯箱模式展示图片，背景使用深色半透明遮罩
2. THE Image_Viewer SHALL 支持左右箭头切换当前目录中当前已加载结果内的上一张/下一张图片；若产品实现跨分页切换，THE Frontend_App SHALL 按需增量加载相邻分页数据，不得假定前端已持有整个目录全部图片
3. THE Image_Viewer SHALL 支持图片缩放操作：鼠标滚轮缩放、双击放大/还原、捏合缩放（触屏设备）
4. THE Image_Viewer SHALL 支持图片拖拽平移：当图片放大超出视口时，用户可拖拽查看图片不同区域
5. THE Image_Viewer SHALL 在底部展示当前目录中当前已加载图片的缩略图导航条，并高亮当前查看项；若继续浏览触达未加载图片，THE Frontend_App SHALL 按需请求相邻分页缩略图数据
6. THE Image_Viewer SHALL 在顶部工具栏展示文件名称，以及"下载"和"关闭"按钮；若前端可从已加载预览资源可靠获取实际尺寸，则 MAY 附加展示图片尺寸信息
7. WHEN 用户按 Esc 键或点击关闭按钮时，THE Image_Viewer SHALL 关闭灯箱并返回文件浏览器
8. WHILE 图片加载中，THE Image_Viewer SHALL 展示加载动画占位符
9. IF 图片预览因临时地址过期或授权失效而加载失败，THEN THE Frontend_App SHALL 自动重新调用后端预览接口获取新地址并重试一次
10. IF 图片在重试后仍加载失败，THEN THE Image_Viewer SHALL 展示错误占位图和"图片加载失败"提示，并提供"重试"按钮

### 需求 8：视频播放器

**用户故事：** 作为用户，我希望能够在线播放网盘中的视频文件，并在后端提供对应能力时支持多清晰度切换和字幕显示，以便获得良好的观看体验。

#### 验收标准

1. WHEN 用户点击视频文件（或选择播放操作）时，THE Video_Player SHALL 以全屏模态或独立页面形式展示视频播放器，背景使用深色遮罩
2. THE Video_Player SHALL 基于 Video.js 或同等播放器实现后端返回的官方播放能力，并优先复用播放器内置的 HLS 支持；当后端返回 HLS 流地址时，THE Video_Player SHALL 支持 HLS 播放，仅在兼容性或播放器封装场景确有需要时再补充 HLS.js
3. THE Video_Player SHALL 提供标准播放控制栏：播放/暂停、进度条、音量控制、全屏切换、播放速度调节（0.5x/1x/1.25x/1.5x/2x）
4. WHEN 后端返回多个清晰度选项时，THE Video_Player SHALL 在控制栏中提供清晰度选择菜单，展示所有可用清晰度（如标清、高清、超清），用户可自由切换
5. WHEN 用户切换清晰度时，THE Video_Player SHALL 记住当前播放进度，切换后从相同进度位置继续播放
6. WHEN 后端返回字幕列表时，THE Video_Player SHALL 在控制栏中提供字幕选择菜单，用户可在控制栏中选择字幕语言或关闭字幕
7. IF 后端未返回字幕信息或当前视频无可用字幕，THEN THE Video_Player SHALL 隐藏字幕选择菜单或展示"无字幕"状态，而不将其视为播放错误
8. IF 后端返回 `Transcoding` 状态，THEN THE Video_Player SHALL 展示"视频正在转码中，请稍后再试"或等效提示信息，并提供"刷新状态"按钮；若获取播放地址时后端返回暂不可播放的错误响应，前端 SHALL 进入错误态并展示错误原因；前端展示的状态文案与可执行操作 SHALL 以后端返回的状态字段或错误信息为准；当后端返回关联任务信息时，前端 SHALL 提供进入 Task_Panel 的入口
9. WHEN 用户按 Esc 键或点击关闭按钮时，THE Video_Player SHALL 停止播放并关闭播放器
10. WHILE 视频加载中，THE Video_Player SHALL 在播放区域展示加载动画
11. IF 视频播放地址因临时资源过期而失效，THEN THE Frontend_App SHALL 自动重新调用后端播放接口获取最新地址并重试一次
12. IF 视频播放地址获取失败或在重试后仍播放出错，THEN THE Video_Player SHALL 展示错误提示信息并提供"重试"按钮
13. WHEN 后端返回当前视频的历史播放进度时，THE Video_Player SHALL 在播放器初始化后提供从上次播放位置继续观看的能力，并允许用户选择从头播放
14. WHILE 视频处于播放中且进度发生变化时，THE Frontend_App SHALL 定期或在暂停、关闭播放器、离开页面时调用后端播放进度保存接口记录当前播放时间；若保存失败，前端 MAY 记录告警但不得中断当前播放

### 需求 9：文档预览

**用户故事：** 作为用户，我希望能够在线预览网盘中的文档文件，以便在支持的渲染方式下无需下载即可查看文档内容。

#### 验收标准

1. WHEN 用户点击 PDF 或 TXT 文件（或选择预览操作）时，THE Doc_Viewer SHALL 以模态窗口或独立面板形式内嵌渲染文档内容
2. WHILE 预览 PDF 文件时，THE Doc_Viewer SHALL 使用 PDF.js 或同等库渲染 PDF 内容，支持页码导航、缩放和滚动浏览
3. WHILE 预览 TXT 文件时，THE Doc_Viewer SHALL 以等宽字体渲染纯文本内容，支持滚动浏览
4. WHEN 用户点击 Office 格式文件（DOC/DOCX/XLS/XLSX/PPT/PPTX）且后端返回渲染方式为 ThirdParty 且第三方页面允许嵌入时，THE Doc_Viewer SHALL 使用后端返回的第三方预览服务 URL 通过 iframe 嵌入展示
5. IF 后端返回 Office 文件的渲染方式为 ThirdParty 但第三方页面拒绝 iframe 嵌入或加载失败，THEN THE Doc_Viewer SHALL 提供"新窗口打开"和"下载"按钮作为替代方案
6. IF 后端返回 Office 文件的渲染方式为 NotAvailable（第三方预览服务未配置），THEN THE Doc_Viewer SHALL 展示"预览服务未配置，请下载后查看"提示信息，并提供"下载"按钮
7. THE Doc_Viewer SHALL 在顶部工具栏展示文件名称，以及"下载"和"关闭"按钮
8. WHILE 文档加载中，THE Doc_Viewer SHALL 展示加载动画占位符
9. IF 文档预览因临时地址过期而失败，THEN THE Frontend_App SHALL 自动重新调用后端预览接口获取新地址并重试一次
10. IF 文档在重试后仍加载失败，THEN THE Doc_Viewer SHALL 展示错误提示信息并提供"重试"、"新窗口打开"（如适用）和"下载"按钮

### 需求 10：后台任务进度面板

**用户故事：** 作为用户，我希望能够查看后台任务（大文件上传、批量操作、视频转码等）的执行状态和进度，以便了解任务完成情况。

#### 验收标准

1. THE Task_Panel SHALL 以可折叠的底部面板或侧边抽屉形式展示，用户可通过工具栏中的任务图标按钮展开或收起
2. THE Task_Panel SHALL 展示当前用户的后台任务列表，每个任务项包含：任务类型图标、任务描述、进度条（百分比）、状态标签（等待中/执行中/已完成/失败/已取消）；任务描述 SHALL 优先基于前端在任务受理时记录的本地操作上下文（如上传、复制、移动、删除、回收站还原、回收站彻底删除、文件名或文件数量）生成，当本地上下文不可用时再回退到后端返回的 `jobType` 与 `resourceId`；后端当前始终返回 `cloudAccountUid`，对应任务项 SHALL 展示账号上下文
3. IF 当前用户存在多个已绑定的 115 账号，THEN THE Task_Panel SHALL 提供按当前账号或全部任务查看的切换/筛选能力；该切换通过是否向任务列表接口传递 `cloudAccountUid` 实现；若当前仅有单账号，前端 MAY 隐藏该筛选
4. WHILE 有任务正在执行中，THE Task_Panel 的触发按钮 SHALL 展示动态指示器（如旋转图标或数字角标），提示用户有进行中的任务
5. THE Task_Panel SHALL 通过受控轮询（正常间隔 ≤ 5 秒）后端任务状态接口自动更新任务进度和状态，并在无进行中任务、面板收起或页面不可见时降低轮询频率或暂停轮询
6. WHEN 任务状态变为"已完成"时，THE Frontend_App SHALL 通过 Toast_Notification 展示"[任务名称] 已完成"的提示；任务名称 SHALL 与任务列表描述保持一致，优先使用前端在任务受理时记录的本地操作上下文生成
7. WHEN 任务状态变为"失败"时，THE Task_Panel SHALL 在对应任务项中展示错误信息，并通过 Toast_Notification 展示"[任务名称] 失败"的提示；任务名称 SHALL 与任务列表描述保持一致，优先使用前端在任务受理时记录的本地操作上下文生成
8. THE Task_Panel SHALL 支持按任务状态筛选（全部/进行中/已完成/失败）
9. WHEN 任务列表为空时，THE Task_Panel SHALL 展示"暂无任务"的空状态提示

### 需求 11：暗色主题与 Glassmorphism 视觉风格

**用户故事：** 作为用户，我希望界面采用现代化的暗色主题和毛玻璃效果设计，以便获得沉浸式且视觉舒适的使用体验。

#### 验收标准

1. THE Frontend_App SHALL 默认使用暗色主题（Dark Mode），主背景色使用深色调（如 #0a0a0f 至 #1a1a2e 范围），文字使用浅色调确保可读性
2. THE Frontend_App SHALL 对卡片、对话框、面板、菜单等浮层组件应用 Glassmorphism 效果：半透明背景（background: rgba 透明度 0.1-0.3）、背景模糊（backdrop-filter: blur 10-20px）、微妙的 1px 半透明边框
3. THE Frontend_App SHALL 使用统一的色彩系统：主色调用于按钮和交互高亮，辅助色用于状态标识（成功/警告/错误/信息），所有颜色在暗色背景上满足 WCAG AA 级对比度要求
4. THE Frontend_App SHALL 对交互元素（按钮、卡片、菜单项）应用平滑的过渡动画：hover 状态变化、展开/收起、淡入/淡出，动画时长控制在 150-300ms
5. THE Frontend_App SHALL 使用现代化的图标库（如 Phosphor Icons 或 Lucide Icons），图标风格统一为线性或填充风格
6. THE Frontend_App SHALL 对文件类型使用差异化的颜色编码图标：文件夹、图片、视频、文档、音频、压缩包、其他类型各使用不同的图标和颜色标识
7. IF 用户在系统或浏览器中启用了减少动态效果偏好，THEN THE Frontend_App SHALL 将非必要动画降级为最小化淡入效果或禁用，以降低动态效果对可用性的影响

### 需求 12：响应式布局

**用户故事：** 作为用户，我希望界面能够在不同屏幕尺寸的设备上正常使用，以便在桌面端和平板端都能获得良好的操作体验。

#### 验收标准

1. THE Frontend_App SHALL 支持三个响应式断点：桌面端（宽度 ≥ 1024px）、平板端（宽度 768px-1023px）、移动端（宽度 < 768px）
2. WHILE 处于桌面端布局时，THE Frontend_App SHALL 展示完整的侧边栏导航、工具栏和文件浏览器区域
3. WHILE 处于平板端布局时，THE Frontend_App SHALL 将侧边栏收起为可展开的汉堡菜单，文件浏览器占据全部宽度
4. WHILE 处于移动端布局时，THE Frontend_App SHALL 使用底部导航栏替代侧边栏，文件浏览器默认使用列表视图，工具栏操作收纳至更多菜单
5. THE File_Browser SHALL 在网格视图下根据屏幕宽度自动调整每行展示的卡片数量
6. THE Image_Viewer、Video_Player 和 Doc_Viewer SHALL 在平板端与移动端以全屏模态形式展示；在桌面端则根据各预览组件自身的交互方案适配当前视口尺寸，不强制统一呈现方式

### 需求 13：键盘快捷键

**用户故事：** 作为用户，我希望能够通过键盘快捷键快速执行常用操作，以便提高操作效率。

#### 验收标准

1. THE Frontend_App SHALL 支持以下键盘快捷键：Ctrl+A 全选当前目录中当前已加载的文件、Delete 删除选中文件、F2 重命名选中文件、Ctrl+C 复制选中文件到应用内剪贴板、Ctrl+X 剪切选中文件到应用内剪贴板、Ctrl+V 将应用内剪贴板中的文件粘贴到当前目录、Enter 打开选中文件/文件夹、Backspace 返回上级目录、Ctrl+F 聚焦搜索框
2. THE File_Browser SHALL 支持方向键导航：上/下箭头在列表视图中移动选中项，上/下/左/右箭头在网格视图中移动选中项
3. WHILE 模态窗口（Image_Viewer、Video_Player、Doc_Viewer）打开时，THE Frontend_App SHALL 禁用文件浏览器的快捷键，仅保留当前模态对应的快捷键（如 Esc 关闭模态、图片左右切换、视频播放控制）
4. THE Image_Viewer SHALL 支持左/右箭头键切换上一张/下一张图片
5. THE Video_Player SHALL 支持空格键播放/暂停、左/右箭头键快进/快退 5 秒、上/下箭头键调节音量
6. IF 用户在可编辑状态（重命名、新建文件夹输入框）中，THEN THE Frontend_App SHALL 暂停所有文件操作快捷键，避免快捷键与文本输入冲突
7. IF File_Browser 未获得焦点，或焦点位于输入框、下拉框、按钮等可编辑/可交互控件内，THEN THE Frontend_App SHALL 禁用文件浏览器快捷键；Backspace 不得触发浏览器历史后退

### 需求 14：加载状态与错误状态展示

**用户故事：** 作为用户，我希望在数据加载和操作出错时能够看到友好的状态提示，以便了解当前系统状态并知道如何处理。

#### 验收标准

1. WHILE 文件列表数据加载中，THE File_Browser SHALL 展示 Skeleton_Loader 骨架屏动画，骨架屏样式与实际列表/网格布局一致
2. WHILE 任何 API 请求进行中，THE Frontend_App SHALL 在对应操作按钮上展示加载指示器（如旋转图标），并禁用该按钮防止重复提交
3. THE Frontend_App SHALL 对目录滚动加载、搜索输入与刷新操作实施防抖、请求去重或取消过期请求，避免重复请求造成界面闪烁或不必要的后端压力；对被用户主动取消或已过期的请求，不得展示错误提示或错误 Toast
4. IF 文件列表加载失败，THEN THE File_Browser SHALL 展示错误状态页面，包含错误插图、错误描述文字和"重试"按钮
5. IF 网络连接中断，THEN THE Frontend_App SHALL 在顶部展示持久性的网络断开提示条，提示"网络连接已断开，请检查网络"，网络恢复后自动消失
6. THE Toast_Notification SHALL 支持四种类型：成功（绿色）、警告（橙色）、错误（红色）、信息（蓝色），每条通知自动在 3-5 秒后消失，用户也可手动关闭
7. IF 用户未授权即尝试访问网盘功能，THEN THE Frontend_App SHALL 展示引导页面，提示用户先完成 115 网盘授权连接
8. IF 后端返回 115 网盘服务未配置、功能被禁用或当前环境暂不支持，THEN THE Frontend_App SHALL 展示功能不可用状态页面，说明当前限制并隐藏或禁用相关操作入口
9. WHILE 文件操作（上传、移动、复制、删除等）执行中，THE Frontend_App SHALL 展示操作进行中的状态指示，帮助用户理解当前执行状态
10. IF 存在无法由后台任务持续追踪的前台进行中操作（如同步上传），THEN THE Frontend_App SHALL 在离开页面或切换路由时给予离页保护提示；对于已进入 Task_Panel 的后台任务，THE Frontend_App SHALL 允许用户离开而不阻止导航

### 需求 15：云下载管理

**用户故事：** 作为用户，我希望能够查看、创建和管理云下载任务，以便将链接任务或 BT 任务直接离线下载到网盘中。

#### 验收标准

1. THE Frontend_App SHALL 在侧边栏或导航区域提供"云下载"入口，点击后进入 `Offline_Download_View`
2. THE Offline_Download_View SHALL 展示当前账号的云下载配额摘要，配额数值以后端返回结果为准
3. THE Offline_Download_View SHALL 展示任务统计信息（全部、进行中、已完成、失败）；统计数值以后端返回结果为准
4. THE Offline_Download_View SHALL 以列表形式展示云下载任务，并在后端返回对应字段时展示任务名称、状态、大小、进度、保存路径、来源链接等信息
5. THE Offline_Download_View SHALL 支持按任务状态筛选（全部、进行中、已完成、失败）；当后端提供服务端筛选或统计接口时，前端 SHALL 以服务端返回结果为准
6. WHEN 用户创建链接下载任务时，THE Frontend_App SHALL 提供多行文本输入区域，支持一次输入多个链接并以换行分隔；可提交的协议类型以后端支持的 HTTP(S)、FTP、磁力链和电驴链接能力为准
7. WHEN 用户创建链接下载任务并选择保存目录时，THE Frontend_App SHALL 将所选目标目录作为可选参数传递给后端；未选择时 SHALL 使用后端默认保存位置
8. WHEN 用户创建 BT 下载任务时，THE Frontend_App SHALL 先调用后端 BT 种子解析接口展示种子文件列表，再允许用户选择待下载文件、保存路径并提交任务创建
9. WHEN 用户删除单个云下载任务时，THE Frontend_App SHALL 调用后端删除接口并刷新任务列表；若后端提供"是否删除源文件"能力，前端 SHALL 以显式开关或确认项承载该选项
10. WHEN 用户执行"清空任务"操作时，THE Frontend_App SHALL 允许用户按任务分类清空（如已完成、全部、失败、进行中）；危险操作执行前 SHALL 先显示确认对话框
11. WHEN 云下载任务相关操作（创建、删除、清空）成功后，THE Offline_Download_View SHALL 刷新任务列表和任务统计；若配额信息受影响，也 SHALL 一并刷新配额摘要
12. WHEN 云下载任务列表为空时，THE Offline_Download_View SHALL 展示空状态提示；IF 云下载接口调用失败，THEN THE Frontend_App SHALL 通过 Toast_Notification 或页面错误态展示错误原因并提供重试入口

### 需求 16：分享管理

**用户故事：** 作为用户，我希望能够查看我的分享、发送记录和接收记录，以便统一管理当前账号相关的分享数据。

#### 验收标准

1. THE Frontend_App SHALL 在侧边栏或导航区域提供"分享管理"入口，点击后进入 `Share_Management_View`
2. THE Share_Management_View SHALL 提供三个标签页：`我的分享`、`发送记录`、`接收记录`；各标签分别对应后端的分享列表接口
3. WHILE 任一分享标签页处于激活状态时，THE Share_Management_View SHALL 以列表或表格形式展示后端返回的分享记录，并在可用时展示分享标题、创建时间、到期时间、状态、访问次数、提取码、分享链接等字段
4. THE Share_Management_View SHALL 对未返回的分享字段展示空值或占位符，不得伪造分享口令、访问次数或分享链接
5. THE Share_Management_View SHALL 支持分页加载或无限滚动加载；切换标签页时 SHALL 重新加载对应数据集，并避免复用错误的列表数据
6. IF 后端为某条分享记录返回可复制的 `shareUrl`、提取码或等效字段，THEN THE Frontend_App SHALL 提供复制操作；若未返回，前端 SHALL 隐藏对应操作
7. WHEN 当前标签页无数据时，THE Share_Management_View SHALL 展示空状态提示；IF 分享管理接口调用失败，THEN THE Frontend_App SHALL 展示错误提示并提供重试入口
