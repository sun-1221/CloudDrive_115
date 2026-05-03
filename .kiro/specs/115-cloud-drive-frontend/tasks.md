# 实现计划：115 网盘前端展示

## 概述

基于 Vue 3 + TypeScript + Vite 构建 115 网盘前端展示层 SPA，通过后端 integration 服务的 `/api/*` 端点消费已归一化 DTO。前端不直连 115 网页端 API，不处理 Cookie、签名、错误字段变体或数据归一化。

## 架构原则

- **认证**：Cookie-based（后端注入），单账号模型，前端仅通过 `GET /api/auth/check` 查询登录态
- **DTO 契约**：前端从后端 `/openapi/v1.json` 自动生成 TypeScript 类型，字段 camelCase
- **错误处理**：统一消费 `application/problem+json` ProblemDetails
- **请求取消**：使用 `AbortController` / `AbortSignal`，不使用已废弃的 `CancelToken`
- **R10 已删除**：任务面板、`JobStore`、`JobPollingService` 不在本计划范围；后台任务通过按需调用 `GET /api/jobs/{jobId}` 轮询

## 任务

- [ ] 1. 项目脚手架与基础配置
  - [ ] 1.1 初始化 Vite + Vue 3 + TypeScript
    - 安装：`vue@^3.5`、`vue-router@^4.5`、`pinia@^2.3`、`naive-ui@^2.41`、`axios@^1.7`、`lucide-vue-next`、`tailwindcss@^4.1`、`@tailwindcss/vite`、`video.js@^8.21`、`pdfjs-dist@^4.9`、`openapi-typescript@^7`
    - 测试：`vitest`、`@vue/test-utils`、`fast-check`、`@fast-check/vitest`、`@playwright/test`
    - _需求: 19.1_
    - _设计: §2.3 技术栈_

  - [ ] 1.2 配置 Tailwind CSS（Vite 插件方案）
    - 在 `vite.config.ts` 注册 `@tailwindcss/vite`
    - 暗色主题默认启用，断点 768px / 1024px
    - _需求: 11.1, 12.1_
    - _设计: §主题系统_

  - [ ] 1.3 配置 Vue Router（对齐 design §3.3）
    - 创建 `src/router/index.ts`
    - 路由：`/` → `HomeView`、`/files` → `FileBrowserView`、`/recycle` → `RecycleBinView`、`/offline` → `OfflineDownloadView`、`/share` → `ShareManagementView`、`/user` → `UserCenterView`、`/login` → `LoginGuideView`
    - `requireAuth` 守卫：检查 `AuthStore.isAuthenticated`，未登录重定向 `/login` 保留原 URL
    - 在 `main.ts` 先 `AuthStore.bootstrap()` 再挂载 Router
    - _需求: 1.3, 1.6_
    - _设计: §3.3 路由设计, §路由守卫_

  - [ ] 1.4 创建应用入口
    - `src/main.ts`：注册 Pinia、Router、Naive UI provider
    - `src/App.vue`：`<router-view>` + 全局 Toast + NetworkStatusBanner
    - _需求: 11.1, 14.2, 14.5_
    - _设计: §3.3 路由设计, §Toast 通知系统, §NetworkStatusBanner 组件_

- [ ] 2. OpenAPI 类型契约
  - [ ] 2.1 配置 openapi-typescript 生成脚本
    - `package.json` 添加 `"gen:types": "openapi-typescript http://localhost:5000/openapi/v1.json -o src/types/generated.ts"`
    - CI 中校验生成文件与后端同步
    - _需求: 19.5_
    - _设计: §6.2 前端 DTO 与后端契约对齐_

  - [ ] 2.2 定义业务扩展类型
    - `src/types/index.ts` 导出 design §6.1 所有 DTO：`CloudDriveFileDto`、`CloudDriveBreadcrumbDto`、`CloudDriveFileListDto`、`CloudDriveRecycleItemDto`、`CloudDriveVideoPlayDto`、`CloudDriveSubtitleDto`、`CloudDriveOfflineTaskDto`、`CloudDriveOfflineQuotaDto`、`TaskStats`、`CloudDriveShareDto`、`CloudDriveUserInfoDto`、`CloudDriveVipInfoDto`、`CloudDriveBalanceDto`、`CloudDriveStorageDetailsDto`、`CloudDriveUserProfileDto`、`ProblemDetails`、`JobStatusDto`
    - 业务扩展：`SortField`、`SortOption`、`FileFilter`、`SearchFilters`、`TaskStatusFilter`、`VideoDefinition`、`AuthCheckResult`、`SpaceSummary`
    - _需求: 2.2, 2.3, 2.6, 4.7, 5.5, 5.6, 8.5, 15.4, 16.3, 17.3, 18.1, 18.2_
    - _设计: §6.1 共享 DTO_

- [ ] 3. 主题系统与样式
  - [ ] 3.1 Naive UI 暗色主题覆盖
    - `src/styles/naive-theme.ts`
    - 基础色：`--bg-base: #0a0a0f`、`--bg-surface: #1a1a2e`
    - 状态色：`--success`、`--warning`、`--error`、`--info`
    - 满足 WCAG AA（文本 ≥ 4.5:1，大文本 ≥ 3:1，图标 ≥ 3:1）
    - _需求: 11.1, 11.3_
    - _设计: §主题系统_

  - [ ] 3.2 Glassmorphism CSS 变量
    - `src/styles/glass.css`
    - 变量：`--glass-bg`、`--glass-border`、`--glass-blur: blur(12px)`
    - 复用类：`.glass-card`、`.glass-modal`、`.glass-breadcrumb`
    - 过渡时长统一 150-300ms
    - _需求: 11.2, 11.4_
    - _设计: §主题系统_

  - [ ] 3.3 文件类型图标配色
    - `src/utils/fileTypeMapping.ts`
    - `getFileIcon()`、`getFileColor()` 使用 Lucide Icons
    - `previewableImageExtensions` 白名单（`jpg/jpeg/png/gif/bmp/webp`，`svg` 排除）与 `isPreviewableImage()`
    - _需求: 11.6, 2.3_
    - _设计: §主题系统_

  - [ ] 3.4 prefers-reduced-motion 降级
    - 全局 `@media (prefers-reduced-motion: reduce)` 禁用非必要动画
    - _需求: 11.7_
    - _设计: §prefers-reduced-motion 降级_

  - [ ] 3.5 格式化工具
    - `src/utils/sizeFormatter.ts`、`dateFormatter.ts`
    - 优先使用后端 `sizeFormat` 字段
    - _需求: 2.2, 17.7_
    - _设计: §6.1 共享 DTO_

  - [ ] 3.6 ThemeController
    - `src/composables/useThemeController.ts`
    - 默认主题 `dark`，支持暗/亮切换并持久化到 `localStorage['cloudDrive.theme']`
    - 切换后同步 `document.documentElement.dataset.theme`
    - _需求: 11.1, 11.5_
    - _设计: §ThemeController_

- [ ] 4. API 层（design §3.1 `api/`）
  - [ ] 4.1 Axios 实例 + 拦截器 + ProblemDetails 解析
    - `src/api/http.ts`
    - 响应拦截器：检测 `content-type: application/problem+json` → 委托 `ErrorHandlerService.handle()`
    - `AbortController` 取消的请求静默结束
    - Axios `timeout = 30000`
    - _需求: 14.3, 14.5, 14.7, 18.1, 18.2_
    - _设计: §ErrorHandlerService_

  - [ ] 4.2 authApi
    - `src/api/authApi.ts`：`check()` → `GET /api/auth/check`
    - _需求: 1.1_
    - _设计: §AuthStore_

  - [ ] 4.3 fileApi
    - `src/api/fileApi.ts`
    - 读操作：`list({cid, offset, limit, o, asc, type, star})` → `GET /api/files`；`getDetail(fileId)` → `GET /api/files/{fileId}`
    - 写操作占位（待后端就绪）：`createFolder`、`rename`、`move`、`copy`、`delete`、`download`、`toggleStar`、`upload`
    - `upload()` 对接 `POST /api/files/upload`，以 `FormData` / `multipart/form-data` 提交
    - _需求: 2.1, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 4.2, 4.7_
    - _设计: §FileStore, §FileOperationService, §FileDetailsModal_

  - [ ] 4.4 searchApi
    - `src/api/searchApi.ts`：`search({searchValue, type, cid, date, offset, limit})` → `GET /api/files/search`
    - _需求: 5.2, 5.5, 5.6, 5.7, 5.9_
    - _设计: §SearchStore_

  - [ ] 4.5 recycleBinApi
    - `src/api/recycleBinApi.ts`
    - `list({offset, limit})` → `GET /api/recycle`
    - 写操作占位：`restore`、`permanentDelete`、`clearAll`
    - _需求: 6.1, 6.3, 6.4, 6.5_
    - _设计: §RecycleBinStore_

  - [ ] 4.6 videoApi
    - `src/api/videoApi.ts`
    - `getPlay(pickCode)` → `GET /api/videos/{pickCode}/play`
    - `getSubtitles(pickCode)` → `GET /api/videos/{pickCode}/subtitles`
    - `getHistory(pickCode)` / `saveHistory(pickCode, progress)`（待后端就绪）
    - _需求: 8.4, 8.7, 8.12, 8.14_
    - _设计: §VideoPlayer 组件, §SubtitleSelector 组件, §VideoHistoryService（待后端 API 就绪）_

  - [ ] 4.7 documentApi
    - `src/api/documentApi.ts`：`getPreview(pickCode)` → `GET /api/documents/{pickCode}/preview`
    - _需求: 9.1_
    - _设计: §DocViewer 组件_

  - [ ] 4.8 offlineApi
    - `src/api/offlineApi.ts`
    - 读操作：`getTasks(page)`、`getQuota()`、`getStats()` → `GET /api/offline/*`
    - 写操作占位：`createUrlTask`、`parseTorrent`、`createBtTask`、`deleteTask`、`clearTasks`
    - _需求: 15.1, 15.2, 15.3, 15.5, 15.6, 15.7, 15.8_
    - _设计: §OfflineStore_

  - [ ] 4.9 shareApi
    - `src/api/shareApi.ts`：`list({tab, offset, limit})` → `GET /api/shares?tab={slist|send|receive}`
    - _需求: 16.2_
    - _设计: §ShareStore_

  - [ ] 4.10 userApi
    - `src/api/userApi.ts`
    - `getProfile()` → `GET /api/user/profile`（聚合端点）
    - 细粒度：`getInfo`、`getVip`、`getBalance`、`getStorage`
    - _需求: 1.2, 1.5, 17.2_
    - _设计: §UserCenterStore_

  - [ ] 4.11 jobsApi
    - `src/api/jobsApi.ts`：`poll(jobId, {intervalMs=2000})` → 轮询 `GET /api/jobs/{jobId}` 至终态
    - _需求: 3.8, 6.5, 15.8_
    - _设计: §5.7 后台任务轮询（大文件上传、清空回收站）, §JobStatusDto（后台任务轮询）_

- [ ] 5. 检查点 — 基础层验证

- [ ] 6. 服务层（design §3.1 `services/`）
  - [ ] 6.1 ClipboardService
    - `src/services/clipboardService.ts`
    - `copy(items)`、`cut(items)`、`paste()`、`clear()`、`getState()`
    - _需求: 3.6, 4.5_
    - _设计: §ClipboardService_

  - [ ] 6.2 KeyboardService
    - `src/services/keyboardService.ts`
    - `register(scope, key, handler)`、`unregister`、`setActiveScope`、`suspend`、`resume`
    - 作用域：`fileBrowser`、`imageViewer`、`videoPlayer`、`docViewer`
    - `shouldHandleEvent()`：`activeModal` → `keyboardSuspended` → 可交互控件检测 → `fileBrowserFocused`
    - _需求: 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.7_
    - _设计: §KeyboardService_

  - [ ] 6.3 TemporaryResourceService
    - `src/services/temporaryResourceService.ts`
    - `fetchWithRetry(resourceFetcher)`：首次失败自动重新获取并仅重试一次；二次失败进入可恢复错误态
    - _需求: 7.8, 7.9, 8.11_
    - _设计: §TemporaryResourceService（视频/图片共用）_

  - [ ] 6.4 ErrorHandlerService（R18 统一消费 ProblemDetails）
    - `src/services/errorHandlerService.ts`
    - `handle(problem, httpStatus)`：
      - 401 → 跳转 `/login` + `clearSession()`，不 Toast
      - 404 → 显示 `detail` 或移除失效项
      - 410 → "分享已失效"空状态
      - 429 → Toast "请求过于频繁"，不重试
      - 5xx → Toast "服务异常"，不暴露 `upstreamCode`
      - 400 → Toast 显示 `detail`
      - 网络/CORS/timeout → Toast "网络异常，请检查连接"
    - 超时阈值：`30000ms`
    - 日志字段：`timestamp/endpoint/httpStatus/upstreamCode`
    - 严禁写入：手机号、邮箱、Cookie、令牌
    - 开发环境 `console.error` 输出完整 ProblemDetails
    - 重试上限 1 次
    - _需求: 14.5, 18.1, 18.2, 18.3, 18.4, 18.5, 18.6, 18.7, 18.8, 18.9, 18.10, 18.11_
    - _设计: §ErrorHandlerService_

  - [ ] 6.5 FileOperationService（依赖后端 API 就绪）
    - `src/services/fileOperationService.ts`
    - 封装 `createFolder`、`rename`、`move`、`copy`、`delete`、`download`、`toggleStar` 调度
    - 202 Accepted `{ jobId }` → `jobsApi.poll(jobId, { intervalMs: 2000 })` 轮询终态
    - 检测浏览器阻止连续下载时，Toast 提示允许连续下载或改为手动下载
    - 文件名前端校验：拒绝空白、拒绝 `/\:*?"<>|`
    - _需求: 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.10, 3.11, 4.2_
    - _设计: §FileOperationService_

  - [ ] 6.5.1 属性测试：文件名校验
    - _需求: 3.4_
    - _设计: §FileOperationService_

  - [ ] 6.6 VideoHistoryService（依赖后端 API 就绪）
    - `src/services/videoHistoryService.ts`
    - `getProgress(pickCode)`、`saveProgress(pickCode, progress)`（每 30 秒）
    - 404 → `null`，从 00:00 开始
    - _需求: 8.12, 8.14_
    - _设计: §VideoHistoryService（待后端 API 就绪）_

- [ ] 7. Composables（design §3.1 `composables/`）
  - [ ] 7.1 useBreakpoint
    - `src/composables/useBreakpoint.ts`
    - 基于 `window.matchMedia` 监听，同步 `UIStore.breakpoint`
    - _需求: 12.1_
    - _设计: §useBreakpoint Composable_

  - [ ] 7.2 useInfiniteScroll
    - `src/composables/useInfiniteScroll.ts`
    - 基于 `IntersectionObserver` + 300ms 防抖 + 请求 ID 去重
    - _需求: 2.7, 14.3_
    - _设计: §useInfiniteScroll Composable_

  - [ ] 7.3 useZoomPan
    - `src/composables/useZoomPan.ts`
    - 鼠标滚轮缩放、双击放大、触屏捏合、拖拽平移
    - 范围 0.25x-5x，步进 0.25
    - _需求: 7.4_
    - _设计: §useZoomPan Composable_

- [ ] 8. Pinia Store 层
  - [ ] 8.1 AuthStore
    - `src/stores/authStore.ts`
    - 状态：`isAuthenticated`、`user: CloudDriveUserInfoDto | null`、`spaceSummary: SpaceSummary | null`、`bootstrapStatus: 'idle'|'checking'|'ready'|'unauthenticated'`
    - Actions：`bootstrap()`（顺序调 checkAuth → fetchUserInfo → fetchSpaceSummary）、`checkAuth`、`fetchUserInfo`、`fetchSpaceSummary`、`clearSession`
    - Cookie 认证模型，无账号切换、无 OAuth、无 PKCE
    - _需求: 1.1, 1.2, 1.4, 1.5_
    - _设计: §AuthStore_

  - [ ] 8.2 FileStore
    - `src/stores/fileStore.ts`
    - 状态：`currentDirectoryId: string`（根目录 `"0"`）、`items`、`paging`、`sort`、`filter`、`selection`、`breadcrumb`、`loading`、`error`
    - Actions：`loadDirectory(cid)`、`loadMore()`、`setSort`、`setFilter`、`refresh`、`select`、`selectRange`、`selectAll`、`clearSelection`
    - 默认 `limit=50`
    - 排序时文件夹始终优先
    - _需求: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9, 2.10, 2.11, 2.12, 2.13, 2.14_
    - _设计: §FileStore_

  - [ ] 8.2.1 属性测试：文件夹优先排序
    - _需求: 2.6_
    - _设计: §FileStore_

  - [ ] 8.2.2 属性测试：全选仅作用于已加载项
    - _需求: 2.14_
    - _设计: §FileStore_

  - [ ] 8.3 SearchStore
    - `src/stores/searchStore.ts`
    - 状态：`keyword`、`filters`、`results`、`paging`、`previousDirectoryId`、`history`、`suggestions`、`suggestionOpen`
    - Actions：`search(keyword, filters)`、`loadMore`、`loadSuggestions`、`persistHistory`、`clearSearch`、`returnToBrowse`
    - 搜索前 `keyword.trim()` 非空校验
    - _需求: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9, 5.10, 5.11_
    - _设计: §SearchStore_

  - [ ] 8.4 RecycleBinStore
    - `src/stores/recycleBinStore.ts`
    - 状态：`items`、`paging`、`count`、`loading`、`error`
    - Actions：`fetchItems`、`loadMore`、`restore(ids)`、`permanentDelete(ids)`、`clearAll()`（后三者依赖后端 API）
    - `clearAll()` 202 + `jobId` → `jobsApi.poll()`
    - _需求: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7_
    - _设计: §RecycleBinStore_

  - [ ] 8.5 OfflineStore
    - `src/stores/offlineStore.ts`
    - 状态：`tasks`、`quota`、`stats`、`filter: TaskStatusFilter`
    - 状态枚举（后端归一化）：`'failed'|'allocating'|'downloading'|'success'`
    - Actions：`loadTasks`、`loadQuota`、`loadStats`、`setFilter`
    - 写操作待后端 API 就绪
    - 进行中任务 10 秒自动刷新
    - _需求: 15.1, 15.2, 15.3, 15.4, 15.5, 15.6, 15.7, 15.8, 15.9, 15.10, 15.11_
    - _设计: §OfflineStore_

  - [ ] 8.6 ShareStore
    - `src/stores/shareStore.ts`
    - 状态：按 Tab（`slist`/`send`/`receive`）维护 items、paging、`activeTab`
    - Actions：`loadShares(tab)`、`switchTab`、`loadMore`
    - _需求: 16.1, 16.2, 16.3, 16.4, 16.5, 16.6, 16.7_
    - _设计: §ShareStore_

  - [ ] 8.7 UserCenterStore
    - `src/stores/userCenterStore.ts`
    - 状态：`userInfo`、`vipInfo`、`balance`、`storageDetails`（均可 `null`）
    - Actions：`loadProfile`、`loadUserInfo`、`loadVipInfo`、`loadBalance`、`loadStorageDetails`
    - 开发环境检测到后端已过滤的敏感字段意外出现时记录告警
    - 优先使用 `sizeFormat`
    - _需求: 17.1, 17.2, 17.3, 17.4, 17.5, 17.6, 17.7, 17.8, 17.9_
    - _设计: §UserCenterStore_

  - [ ] 8.8 UIStore
    - `src/stores/uiStore.ts`
    - 状态：`viewMode`、`theme`、`sidebarCollapsed`、`breakpoint`、`isOffline`、`fileBrowserFocused`、`activeModal`、`keyboardSuspended`
    - Actions：`setViewMode`、`setTheme`、`toggleSidebar`、`setBreakpoint`、`setOfflineStatus`、`setFileBrowserFocused`、`setActiveModal`、`suspendKeyboard`、`resumeKeyboard`
    - 监听 `navigator.onLine` 更新 `isOffline`
    - _需求: 2.3, 11.1, 11.5, 12.1, 12.2, 12.3, 12.4, 13.6, 13.7, 14.5_
    - _设计: §UIStore 焦点与视图状态_

- [ ] 9. 检查点 — 服务层与状态层验证

- [ ] 10. 通用组件
  - [ ] 10.1 SkeletonLoader
    - `src/components/common/SkeletonLoader.vue`
    - 变体：`list`、`grid`、`detail`
    - _需求: 14.1_
    - _设计: §SkeletonLoader 组件_

  - [ ] 10.2 Toast 通知系统
    - Naive UI `useMessage()` 组件内使用
    - `createDiscreteApi(['message'])` 在 `main.ts` 创建全局实例
    - 类型：success / warning / error / info，时长 3-5 秒
    - _需求: 14.2_
    - _设计: §Toast 通知系统_

  - [ ] 10.3 ErrorState
    - `src/components/common/ErrorState.vue`
    - 错误插图、描述（`ProblemDetails.detail`）、重试按钮
    - _需求: 14.4, 18.4_
    - _设计: §ErrorHandlerService_

  - [ ] 10.4 NetworkStatusBanner
    - `src/components/common/NetworkStatusBanner.vue`
    - 离线时顶部固定条，文案"网络连接已断开，请检查网络"
    - _需求: 14.5_
    - _设计: §NetworkStatusBanner 组件_

  - [ ] 10.5 ConfirmDialog
    - `src/components/common/ConfirmDialog.vue`
    - 删除文案固定"文件将移至回收站"
    - 彻底删除文案固定"文件将被永久删除且无法恢复"
    - _需求: 3.6, 6.4, 6.5_
    - _设计: §ConfirmDialog_

- [ ] 11. 布局组件
  - [ ] 11.1 AppLayout（断点驱动）
    - `src/components/layout/AppLayout.vue`
    - 桌面：Sidebar + Header + `<router-view>`
    - 平板：汉堡抽屉 + Header + `<router-view>`
    - 移动：`<router-view>` + BottomNav
    - _需求: 12.1, 12.2, 12.3, 12.4_
    - _设计: §AppLayout 组件_

  - [ ] 11.2 Sidebar
    - `src/components/layout/Sidebar.vue`
    - 导航：文件浏览、回收站、云下载、分享、用户中心
    - 展示 `AuthStore.spaceSummary`
    - 平板退化为 `n-drawer`
    - _需求: 1.5, 12.2, 12.3_
    - _设计: §AppLayout 组件_

  - [ ] 11.3 Header
    - `src/components/layout/Header.vue`
    - 面包屑插槽、搜索栏、用户头像入口
    - _需求: 17.1_
    - _设计: §AppLayout 组件_

  - [ ] 11.4 BottomNav
    - `src/components/layout/BottomNav.vue`
    - 移动端底部导航
    - _需求: 12.4_
    - _设计: §AppLayout 组件_

  - [ ] 11.5 AuthGuard
    - `src/components/layout/AuthGuard.vue`
    - 根据登录态渲染主应用或 `LoginGuideView`
    - 401 / Cookie 失效时清除敏感内存状态，不展示 Toast
    - _需求: 1.3, 1.6, 18.3_
    - _设计: §AuthGuard 组件（布局层）_

  - [ ] 11.6 LoginGuideView
    - `src/views/LoginGuideView.vue`
    - 区分 `cookie_invalid` / `network` 两种引导状态并提供重试入口
    - _需求: 1.3_
    - _设计: §LoginGuideView_

- [ ] 12. 文件浏览器（R2）
  - [ ] 12.1 BreadcrumbNav
    - `src/components/file/BreadcrumbNav.vue`
    - 数据源：`FileStore.breadcrumb`（后端归一化 `CloudDriveBreadcrumbDto[]`）
    - 每层可点击；搜索结果模式展示"搜索结果：[关键词]" + "返回浏览"
    - _需求: 2.5, 5.3_
    - _设计: §BreadcrumbNav_

  - [ ] 12.2 FileToolbar
    - `src/components/file/FileToolbar.vue`
    - 视图切换、排序、筛选、新建文件夹、上传、刷新
    - 选中时展示批量操作：移动、复制、删除；全为文件时展示逐个下载
    - _需求: 2.3, 2.6, 3.3, 3.5, 3.6_
    - _设计: §FileBrowser 组件_

  - [ ] 12.3 FileListView
    - `src/components/file/FileListView.vue`
    - 缩略图、文件名、大小、类型、修改时间
    - 列头排序、`Ctrl+点击`多选、`Shift+点击`范围选择
    - 缩略图懒加载（IntersectionObserver）、失败回退类型图标
    - 不得为列表展示请求完整原图/原视频
    - _需求: 2.2, 2.8, 2.9, 2.10_
    - _设计: §FileBrowser 组件_

  - [ ] 12.3.1 属性测试：缩略图懒加载且失败可回退
    - _需求: 2.9_
    - _设计: §FileBrowser 组件_

  - [ ] 12.4 FileGridView
    - `src/components/file/FileGridView.vue`
    - Glassmorphism 卡片网格
    - 列数自适应：桌面 ≥5、平板 3-4、移动 2
    - _需求: 2.3, 12.5_
    - _设计: §FileBrowser 组件_

  - [ ] 12.5 FileBrowserView 页面
    - `src/views/FileBrowserView.vue`
    - 组装 BreadcrumbNav、FileToolbar、FileListView/FileGridView、ContextMenu、UploadZone
    - 集成 `useInfiniteScroll`
    - 双击文件夹进入；单击文件按类型打开 ImageViewer/VideoPlayer/DocViewer
    - `loading` → SkeletonLoader；`error` → ErrorState + 重试
    - 注册 `fileBrowser` 作用域快捷键
    - _需求: 2.1, 2.4, 2.7, 14.1, 14.4_
    - _设计: §FileBrowser 组件, §useInfiniteScroll Composable_

  - [ ] 12.5.1 属性测试：无限滚动请求去重
    - _需求: 2.7, 14.3_
    - _设计: §useInfiniteScroll Composable_

- [ ] 13. 文件操作（R3，大部分占位）
  - [ ] 13.1 UploadZone（占位）
    - `src/components/operation/UploadZone.vue`
    - 拖拽进入展示全屏半透明覆盖层
    - 实现等待后端 `POST /api/files/upload` 就绪
    - _需求: 3.1, 3.2_
    - _设计: §UploadZone 组件（占位）_

  - [ ] 13.2 FileOperationService 集成
    - 新建文件夹：行内编辑 → `createFolder()` → 刷新
    - 重命名：双击 → 校验 → `rename()` → 刷新
    - 移动/复制：FolderPickerDialog → `move()` / `copy()` → 刷新
    - 删除：ConfirmDialog → `delete()`
    - 下载：`download()` 触发浏览器下载，过期自动重试一次
    - 多文件下载：逐个调用
    - 大文件/批量 202 Accepted → `jobsApi.poll()` → 刷新
    - _需求: 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.10, 3.11_
    - _设计: §FileOperationService, §ConfirmDialog, §FolderPickerDialog_

  - [ ] 13.3 FolderPickerDialog
    - `src/components/operation/FolderPickerDialog.vue`
    - 目录树选择
    - _需求: 3.5_
    - _设计: §FolderPickerDialog_

- [ ] 14. 右键菜单（R4）
  - [ ] 14.1 ContextMenu
    - `src/components/operation/ContextMenu.vue`
    - Glassmorphism 背景
    - 文件夹：打开、重命名、移动、复制、删除
    - 图片：预览 + 通用项；视频：播放 + 通用项；文档：预览 + 通用项
    - 所有文件：下载、详情、星标切换
    - 多选：移动、复制、删除、逐个下载（全为文件时）
    - 空白：新建文件夹、上传、刷新、粘贴（剪贴板非空时）
    - 距视口右/下边缘 <50px 时自动翻转到左/上侧
    - 支持方向键导航、Enter 确认、`activeIndex` 高亮
    - 点击项先关闭菜单再执行；Esc 或外部点击关闭
    - 移动端退化为底部动作面板
    - _需求: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 12.4_
    - _设计: §ContextMenu 组件_

  - [ ] 14.2 FileDetailsModal
    - `src/components/operation/FileDetailsModal.vue`
    - 调用 `fileApi.getDetail(fileId)` → `GET /api/files/{fileId}`
    - 仅展示后端返回元数据，缺失字段占位符
    - _需求: 4.7_
    - _设计: §FileDetailsModal_

- [ ] 15. 搜索（R5）
  - [ ] 15.1 SearchBar
    - `src/components/common/SearchBar.vue`
    - 输入 + 类型/目录/日期筛选 + 历史建议下拉
    - 关键词空白展示"请输入搜索关键词"
    - `Ctrl+F` 聚焦
    - _需求: 5.1, 5.5, 5.6, 5.7, 5.9, 13.1_
    - _设计: §SearchBar 组件_

  - [ ] 15.1.1 搜索历史与建议
    - 关键词长度 ≥2 时，200ms 防抖触发 `loadSuggestions`
    - localStorage 最多保留 20 条历史记录
    - _需求: 5.8_
    - _设计: §SearchStore, §SearchBar 组件_

  - [ ] 15.2 搜索结果视图（复用 FileBrowserView）
    - 数据源切换为 `SearchStore.results`
    - 每个结果项显示所在路径
    - 空结果展示"未找到匹配文件"
    - _需求: 5.2, 5.3, 5.4_
    - _设计: §SearchStore, §FileBrowser 组件_

  - [ ] 15.2.1 属性测试：返回浏览恢复原目录
    - _需求: 5.4_
    - _设计: §SearchStore_

- [ ] 16. 回收站（R6）
  - [ ] 16.1 RecycleBinView
    - `src/views/RecycleBinView.vue`
    - 列表：文件名、大小、原始目录、删除时间（`fileName/fileSize/originalParentName/deletedTime`）
    - 加载 → SkeletonLoader；错误 → ErrorState
    - 单选/多选还原、彻底删除（占位）
    - 彻底删除 ConfirmDialog"文件将被永久删除且无法恢复"
    - 清空：ConfirmDialog → `DELETE /api/recycle` → 202 + `jobId` → `jobsApi.poll()` → 刷新
    - 分页（`offset` + `limit`，最大 200）
    - _需求: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7_
    - _设计: §RecycleBinView + RecycleItemCard 组件, §ConfirmDialog_

- [ ] 17. 图片预览（R7）
  - [ ] 17.1 ImageViewer
    - `src/components/preview/ImageViewer.vue`
    - 全屏灯箱：ImageStage + ImageTopBar（文件名 + 下载 + 关闭）+ ThumbnailNavigator
    - `props: { images: CloudDriveFileDto[], initialIndex: number }`
    - 过滤：`isVideo === false` 且扩展名在 `previewableImageExtensions`（`svg` 排除）
    - 资源：优先 `originalUrl`；为空时 `GET /api/files/{pickCode}/preview`
    - URL 过期通过 `TemporaryResourceService` 重试一次
    - 顶部展示"3 / 15"索引和文件名
    - _需求: 7.1, 7.2, 7.5, 7.8, 7.9_
    - _设计: §ImageViewer 组件_

  - [ ] 17.2 ThumbnailNavigator
    - `src/components/preview/ThumbnailNavigator.vue`
    - 底部缩略图列表，高亮当前，点击切换
    - 使用 `thumb` 字段
    - _需求: 7.3_
    - _设计: §ThumbnailNavigator 组件_

  - [ ] 17.3 useZoomPan 集成
    - 滚轮缩放、+/- 键、拖拽平移，范围 0.25x-5x
    - _需求: 7.4_
    - _设计: §useZoomPan Composable_

  - [ ] 17.4 ImageViewer 快捷键与行为
    - `imageViewer` 作用域：左右切换、Esc 关闭、Space 全屏切换
    - 加载失败重试一次；二次失败展示错误占位 + 重试
    - _需求: 7.6, 7.7, 7.8, 7.9, 13.4_
    - _设计: §ImageViewer 组件, §KeyboardService, §TemporaryResourceService（视频/图片共用）_

- [ ] 18. 视频播放器（R8）
  - [ ] 18.1 VideoPlayer
    - `src/components/preview/VideoPlayer.vue`
    - Video.js 8.x，复用内置 VHS/HLS
    - 全屏模态
    - 调用 `videoApi.getPlay(pickCode)` → `CloudDriveVideoPlayDto`
    - 控制栏：播放/暂停、进度、音量、全屏、速度（0.5x/1x/1.25x/1.5x/2x）
    - 清晰度菜单：`availableDefinitions` 渲染（SD=1/HD=2/FHD=3/FullHD=4/UHD4K=5/Original=100）
    - `status: 'transcoding'` → "视频转码中" + 刷新按钮
    - `vipUpgradeRequired: true` → "升级 VIP 以观看此清晰度"
    - _需求: 8.1, 8.2, 8.3, 8.4, 8.5, 8.9, 8.10_
    - _设计: §VideoPlayer 组件_

  - [ ] 18.2 清晰度切换保留进度
    - 切换前记录 `currentTime` / `paused`
    - 切换 `src` 后 `loadedmetadata` 触发 `player.currentTime(savedTime)`
    - 切换前播放中则恢复 `player.play()`
    - 404/410 → `TemporaryResourceService` 重试
    - _需求: 8.6, 8.11_
    - _设计: §VideoPlayer 组件, §TemporaryResourceService（视频/图片共用）_

  - [ ] 18.3 SubtitleSelector
    - `src/components/preview/SubtitleSelector.vue`
    - 调用 `videoApi.getSubtitles(pickCode)`
    - 展示 `language/title`；空列表隐藏或展示"无字幕"
    - _需求: 8.7, 8.8_
    - _设计: §SubtitleSelector 组件_

  - [ ] 18.4 VideoHistoryService 集成（待后端就绪）
    - 打开时 `getProgress(pickCode)` 恢复；404 → 从 00:00
    - 每 30 秒 `saveProgress`
    - _需求: 8.12, 8.13, 8.14_
    - _设计: §VideoHistoryService（待后端 API 就绪）_

  - [ ] 18.5 VideoPlayer 快捷键
    - `videoPlayer` 作用域：Space、左/右 10s、上/下 ±5%、Esc
    - _需求: 8.11, 13.5_
    - _设计: §VideoPlayer 组件, §KeyboardService_

  - [ ] 18.5.1 属性测试：清晰度切换保持播放进度
    - _需求: 8.6_
    - _设计: §VideoPlayer 组件_

- [ ] 19. 文档预览（R9）
  - [ ] 19.1 DocViewer
    - `src/components/preview/DocViewer.vue`
    - 调用 `documentApi.getPreview(pickCode)` → `{ url, renderMode }`
    - `renderMode: 'Direct'`：PDF 用 PDF.js（显式 worker，使用 `new URL(..., import.meta.url)`）；TXT 用等宽字体（`Consolas/Monaco`）
    - `renderMode: 'ThirdParty'`：iframe 嵌入
    - `renderMode: 'NotAvailable'`：展示"预览服务未配置，请下载后查看"
    - 失败重试一次；二次失败展示"重试"和"下载"
    - `docViewer` 作用域：Esc 关闭
    - _需求: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 13.1_
    - _设计: §DocViewer 组件_

- [ ] 20. 检查点 — 预览组件验证

- [ ] 21. 键盘快捷键集成（R13）
  - [ ] 21.1 FileBrowser 作用域快捷键
    - 在 FileBrowserView 注册：`Ctrl+A` 全选、`Delete` 删除、`F2` 重命名、`Ctrl+C/X/V` 复制/剪切/粘贴、`Enter` 打开、`Backspace` 返回上级、`Ctrl+F` 搜索、方向键导航
    - 焦点管理：`focusin/focusout` 维护 `UIStore.fileBrowserFocused`
    - `Backspace` 仅在 `fileBrowser` 作用域且 `fileBrowserFocused === true` 时触发，`preventDefault()` 阻止浏览器后退
    - 对话框打开焦点离开，快捷键失效
    - 右键菜单通过 `keyboardSuspended` 暂停
    - _需求: 13.1, 13.2, 13.3, 13.6, 13.7_
    - _设计: §KeyboardService, §UIStore 焦点与视图状态_

  - [ ] 21.1.1 属性测试：Backspace 不触发浏览器后退
    - _需求: 13.7_
    - _设计: §KeyboardService_

  - [ ] 21.2 预览组件快捷键
    - ImageViewer / VideoPlayer / DocViewer 各自作用域
    - 模态打开暂停 `fileBrowser` 作用域
    - _需求: 13.4, 13.5_
    - _设计: §KeyboardService_

- [ ] 22. 响应式布局（R12）
  - [ ] 22.1 断点驱动布局切换
    - 桌面（≥1024px）：Sidebar + Header
    - 平板（768-1023px）：汉堡抽屉
    - 移动（<768px）：BottomNav
    - 网格列数：移动 2、平板 3-4、桌面 ≥5
    - _需求: 12.1, 12.2, 12.3, 12.4, 12.5_
    - _设计: §断点系统, §AppLayout 组件, §FileBrowser 组件_

  - [ ] 22.2 预览组件响应式
    - 平板/移动：ImageViewer/VideoPlayer/DocViewer 全屏模态
    - _需求: 12.6_
    - _设计: §断点系统_

- [ ] 23. 云下载（R15）
  - [ ] 23.1 OfflineDownloadView
    - `src/views/OfflineDownloadView.vue`
    - 组装 TaskListItem、QuotaSummaryCard、StatsBar
    - `loadTasks` / `loadQuota` / `loadStats`
    - 进行中任务 10 秒自动刷新
    - 状态筛选：全部/进行中/已完成/失败（active = `allocating`+`downloading`）
    - _需求: 15.1, 15.2, 15.3, 15.4, 15.9, 15.10, 15.11_
    - _设计: §OfflineDownloadView + TaskListItem + QuotaSummaryCard 组件_

  - [ ] 23.2 TaskListItem
    - `src/components/offline/TaskListItem.vue`
    - 任务名、大小、进度、状态、添加时间、清晰度
    - 删除按钮（占位）
    - _需求: 15.4, 15.7_
    - _设计: §OfflineDownloadView + TaskListItem + QuotaSummaryCard 组件_

  - [ ] 23.3 QuotaSummaryCard
    - `src/components/offline/QuotaSummaryCard.vue`
    - 展示 `count/surplus/maxSize/used`
    - _需求: 15.2_
    - _设计: §OfflineDownloadView + TaskListItem + QuotaSummaryCard 组件_

  - [ ] 23.4 添加任务对话框（待后端 API）
    - URL：`POST /api/offline/tasks/urls`
    - BT：`POST /api/offline/parse-torrent` → `POST /api/offline/tasks/bt`
    - 清空：`POST /api/offline/tasks:clear` → 202 + `jobId` → `jobsApi.poll()`
    - _需求: 15.5, 15.6, 15.8_
    - _设计: §OfflineStore, §5.7 后台任务轮询（大文件上传、清空回收站）_

- [ ] 24. 分享管理（R16）
  - [ ] 24.1 ShareManagementView
    - `src/views/ShareManagementView.vue`
    - 三 Tab：我的分享、已发送、已接收（`slist/send/receive`）
    - 切换 Tab 调 `shareApi.list({ tab, offset, limit })`
    - 分页加载
    - _需求: 16.1, 16.2, 16.4_
    - _设计: §ShareManagementView + ShareItemCard 组件, §ShareStore_

  - [ ] 24.2 ShareItemCard
    - `src/components/share/ShareItemCard.vue`
    - 分享名称、数量、创建时间、状态、有效期
    - 未返回字段占位符
    - `shareUrl`/`receiveCode` 存在时渲染复制按钮（`navigator.clipboard.writeText()`，降级 `execCommand`）
    - _需求: 16.3, 16.6, 16.7_
    - _设计: §ShareManagementView + ShareItemCard 组件_

- [ ] 25. 用户中心（R17）
  - [ ] 25.1 UserCenterView
    - `src/views/UserCenterView.vue`
    - 调用 `userApi.getProfile()` 聚合
    - 任一子模块为 `null` 时对应区域展示 ErrorState + 重试（错误隔离）
    - 组装 UserProfileCard、VipStatusCard、BalanceCard、StorageDetailsCard
    - _需求: 17.1, 17.2, 17.9_
    - _设计: §UserCenterView + UserProfileCard + VipStatusCard + BalanceCard + StorageDetailsCard 组件, §UserCenterStore_

  - [ ] 25.2 UserProfileCard
    - 昵称、头像、UID、`maskedPhone`、`maskedEmail`
    - 严禁访问后端已过滤的 34 个敏感字段
    - _需求: 17.3, 17.8_
    - _设计: §UserCenterView + UserProfileCard + VipStatusCard + BalanceCard + StorageDetailsCard 组件_

  - [ ] 25.3 VipStatusCard
    - VIP 等级、有效期、剩余空间
    - _需求: 17.4_
    - _设计: §UserCenterView + UserProfileCard + VipStatusCard + BalanceCard + StorageDetailsCard 组件_

  - [ ] 25.4 BalanceCard
    - 云币、优惠券等（从归一化结果读取）
    - _需求: 17.5_
    - _设计: §UserCenterView + UserProfileCard + VipStatusCard + BalanceCard + StorageDetailsCard 组件_

  - [ ] 25.5 StorageDetailsCard
    - 已用/总空间/各类型占比
    - 优先使用 `sizeFormat`
    - _需求: 17.6, 17.7_
    - _设计: §UserCenterView + UserProfileCard + VipStatusCard + BalanceCard + StorageDetailsCard 组件_

- [ ] 26. 加载与错误处理集成（R14 + R18）
  - [ ] 26.1 全局集成
    - `AuthGuard` 处理 401 → 跳转 `/login` + `clearSession()`
    - `ErrorHandlerService` 统一路由 ProblemDetails
    - `NetworkStatusBanner` 监听 `navigator.onLine`
    - 页面切换保留搜索关键词、视图模式、排序方式（localStorage）
    - 前台操作（同步上传）注册 `beforeunload` 提示
    - _需求: 14.2, 14.3, 14.4, 14.5, 14.6, 14.7, 14.9_
    - _设计: §AuthGuard 组件（布局层）, §ErrorHandlerService, §NetworkStatusBanner 组件, §UIStore 焦点与视图状态_

  - [ ] 26.2 AbortController 取消请求
    - 搜索、无限滚动使用 `AbortController`
    - 切换页面或关闭组件时取消未完成请求
    - 主动取消的请求静默结束
    - _需求: 14.3_
    - _设计: §SearchStore, §useInfiniteScroll Composable_

  - [ ] 26.3 结构化日志脱敏
    - 日志字段：`timestamp/endpoint/httpStatus/upstreamCode`
    - 严禁写入：手机号、邮箱、Cookie、令牌、请求/响应体
    - _需求: 18.9, 18.10_
    - _设计: §ErrorHandlerService_

  - [ ] 26.3.1 属性测试：同一操作重试上限 1 次
    - _需求: 18.11_
    - _设计: §ErrorHandlerService_

- [ ] 27. 后台任务轮询集成（202 Accepted）
  - [ ] 27.1 大文件上传 / 批量操作 / 清空回收站 / 清空云下载
    - 所有 202 Accepted `{ jobId }` 响应通过 `jobsApi.poll(jobId, { intervalMs: 2000 })`
    - 终态（`completed`/`failed`）触发刷新或错误提示
    - _需求: 3.8, 6.5, 15.8_
    - _设计: §5.7 后台任务轮询（大文件上传、清空回收站）, §JobStatusDto（后台任务轮询）_

- [ ] 28. 测试
  - [ ] 28.1 单元测试（Vitest）
    - `fileNameValidator.spec.ts` _需求: 3.4_
    - `sizeFormatter.spec.ts`：格式化字段优先、回退字节 _需求: 17.7_
    - `problemDetailsParser.spec.ts`：各 HTTP 状态码 → 分类 _需求: 18.1, 18.2, 18.3, 18.4, 18.5, 18.6, 18.7, 18.8, 18.9, 18.10, 18.11_
    - `errorHandlerService.spec.ts`：401/404/410/429/5xx 路由 _需求: 18.3, 18.4, 18.5, 18.6, 18.7, 18.8_
    - _需求: 3.4, 17.7, 18.1, 18.2, 18.3, 18.4, 18.5, 18.6, 18.7, 18.8, 18.9, 18.10, 18.11_
    - _设计: §FileOperationService, §6.1 共享 DTO, §ErrorHandlerService_

  - [ ] 28.2 组件测试（@vue/test-utils + Vitest）
    - `FileBrowser.spec.ts` _需求: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9, 2.10, 2.11, 2.12, 2.13, 2.14_
    - `ImageViewer.spec.ts` _需求: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 7.8, 7.9_
    - `VideoPlayer.spec.ts` _需求: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8, 8.9, 8.10, 8.11_
    - `RecycleBinView.spec.ts` _需求: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7_
    - _需求: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9, 2.10, 2.11, 2.12, 2.13, 2.14, 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7, 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 7.8, 7.9, 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8, 8.9, 8.10, 8.11_
    - _设计: §FileBrowser 组件, §RecycleBinView + RecycleItemCard 组件, §ImageViewer 组件, §VideoPlayer 组件_

  - [ ] 28.3 E2E 测试（Playwright）
    - `auth.spec.ts`：Cookie 有效/失效/网络异常 _需求: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_
    - `file-browse.spec.ts`：目录切换、排序、筛选、分页 _需求: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9, 2.10, 2.11, 2.12, 2.13, 2.14_
    - `search.spec.ts`：搜索、日期筛选、返回浏览 _需求: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9, 5.10, 5.11_
    - `video.spec.ts`：播放、清晰度、字幕、VIP 提示 _需求: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8, 8.9, 8.10, 8.11, 8.12, 8.13, 8.14_
    - `offline.spec.ts`：任务列表、配额、筛选 _需求: 15.1, 15.2, 15.3, 15.4, 15.5, 15.6, 15.7, 15.8, 15.9, 15.10, 15.11_
    - `error-handling.spec.ts`：401、429、404、410、网络异常 _需求: 18.1, 18.2, 18.3, 18.4, 18.5, 18.6, 18.7, 18.8, 18.9, 18.10, 18.11_
    - `responsive.spec.ts`：桌面/平板/移动视口 _需求: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6_
    - _需求: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 2.8, 2.9, 2.10, 2.11, 2.12, 2.13, 2.14, 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9, 5.10, 5.11, 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8, 8.9, 8.10, 8.11, 8.12, 8.13, 8.14, 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 15.1, 15.2, 15.3, 15.4, 15.5, 15.6, 15.7, 15.8, 15.9, 15.10, 15.11, 18.1, 18.2, 18.3, 18.4, 18.5, 18.6, 18.7, 18.8, 18.9, 18.10, 18.11_
    - _设计: §AuthStore, §AuthGuard 组件（布局层）, §FileBrowser 组件, §SearchStore, §VideoPlayer 组件, §OfflineDownloadView + TaskListItem + QuotaSummaryCard 组件, §ErrorHandlerService, §断点系统_

  - [ ] 28.4 契约测试
    - `openapi-sync.spec.ts`：启动时 fetch `/openapi/v1.json` 校验生成类型与运行时一致
    - _需求: 19.5_
    - _设计: §6.2 前端 DTO 与后端契约对齐_

- [ ] 29. 最终集成验收
  - 确保所有模块集成完毕，路由导航正常，文件操作、预览、搜索、回收站、云下载、分享、用户中心、键盘快捷键、响应式布局均可正常工作
  - 可访问性审核：WCAG AA 对比度
  - 性能审核：首屏加载、无限滚动流畅度、视频起播延迟

## 备注

- 标记 `*` 的任务（未在本版本使用）为增强型测试；本版本所有属性测试与组件测试均为 MVP 核心验证
- 每个任务引用的需求编号已严格对齐 `requirements.md`（18 个模块，R10 已删除）
- 每个组件/章节引用已严格对齐 `design.md` 中对应标题
- 检查点任务用于阶段性验证，确保增量开发正确性
- 依赖后端待补采集 API 的组件（文件写操作、回收站写操作、视频历史、云下载写操作）已标注"占位"或"待后端就绪"
