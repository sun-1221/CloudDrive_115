# 115 CloudDrive 集成总目标与执行计划

## 1. 总目标

在当前 `E:\Code\115web` 本地版本基础上，完成 115 CloudDrive 集成的审核、修复、实现和验收，形成可追溯、可验证、可持续推进的后端集成基线。

总体要求如下：

- 只实现有官方采集证据支持的 API。
- 未采集或低置信度能力保持 fail-closed。
- 禁用接口统一返回 HTTP `501` 和错误码 `cloud_drive_api_not_collected`。
- 禁用能力不得调用 Gateway、创建后台任务或失效缓存。
- 已启用能力严格遵循已采集的 method、host、path、query、form 和响应字段契约。
- Cookie、签名、账号、文件名、pickCode、临时 URL、金额等敏感数据必须完整脱敏。
- 缓存、限流、任务完成、失败和重启恢复语义必须一致。
- 构建、测试、Spec quality、漏洞扫描和格式检查必须通过。
- 保持当前依赖版本，特别是 MediatR `12.5.0`，不进行未经授权的升级。

## 2. 执行约束

- 使用当前本地 checkout：`E:\Code\115web`。
- 不创建或使用 Git worktree。
- 总控对话采用单写入者原则，避免并发修改同一 checkout。
- 不执行 `reset`、`clean`、`checkout` 或覆盖无关用户修改。
- 所有 Spec checkbox 只有在对应实现和验证证据完成后才能勾选。
- 未验证内容必须标记为 `Unknown`、`NeedFix` 或 `NotCollected/Disabled`，不能标记为通过。

## 3. 阶段计划

### 阶段一：基线与环境门禁

1. 读取项目规则、Spec、设计文档和官方 API 证据。
2. 确认当前依赖解析版本和漏洞状态。
3. 确认构建目录可访问并完成基础构建。
4. 固化需求、设计、代码和测试之间的追溯关系。

### 阶段二：Cookie 会话与 Gateway

1. 使用统一接口 `ICloudDriveWebApiGateway`。
2. 只注册已采集的主机和路径。
3. 完善 method + host + path + 关键 query 感知的错误适配注册表。
4. 未匹配、未采集、非 JSON 或不完整响应统一 fail-closed。
5. 补齐错误字段映射、脱敏和契约测试。

### 阶段三：已采集文件与回收站能力

1. 核验文件列表和文件详情派生逻辑。
2. 核验下载、新建目录、单项复制、重命名和星标。
3. 核验回收站只读列表。
4. 确认请求参数来自已采集闭集。
5. 确认成功响应、缓存失效和完成语义一致。

### 阶段四：预览、视频与播放历史

1. 通过已采集下载契约提供图片和文档临时资源。
2. 核验视频播放基础字段和字幕空数组。
3. 核验播放历史读取。
4. 保持播放进度写入和主动转码禁用。

### 阶段五：云下载、分享与用户聚合

1. 核验云下载 `task_lists`、`task_count` 和 `get_quota_package_info` 只读操作。
2. 核验我的分享只读列表。
3. 核验 VIP、余额和空间聚合。
4. 保持云下载写操作、分享发送和分享接收禁用。

### 阶段六：缓存、限流与后台任务

1. 只有已启用操作才能执行缓存失效。
2. 只有真实上游调用才能进入限流分类。
3. 禁用操作不得创建任务记录。
4. `Completed` 只表示上游操作已确认成功。
5. 启动恢复时，不可恢复任务必须标记失败，可恢复任务才允许重新调度。

### 阶段七：测试与最终验收

1. 运行 Domain、Application、Infrastructure、WebApi 和 Architecture 全量测试。
2. 运行禁用能力的真实 HTTP 入口集成测试。
3. 观察 Gateway、Job、Cache 的副作用计数保持为零。
4. 运行漏洞扫描、Spec quality 和 `git diff --check`。
5. 输出需求 → 设计 → 代码入口 → Gateway → 持久化/任务/缓存 → 测试的审核矩阵。
6. 汇总 `Passed`、`NeedFix`、`NotCollected/Disabled` 和 `Unknown` 项。

## 4. 当前进度

已完成并验证：

- 构建目录权限修复。
- 默认解决方案构建，0 个错误。
- 依赖漏洞扫描无易受攻击包。
- Quartz 4 迁移。
- 上传、搜索、移动、删除、回收站写、云下载写、主动转码、播放进度写入、分享发送/接收等禁用能力 fail-closed。
- 禁用能力 HTTP 集成测试：16 项通过。
- Application 测试：105 项通过。
- Infrastructure 测试：126 项通过。
- WebApi 测试：42 项通过。
- Spec quality 检查通过。
- `git diff --check` 通过。
- Gateway 接口重命名为 `ICloudDriveWebApiGateway`。
- 错误适配注册表已按 method + host + 精确 path + observed query 收敛，并通过 Infrastructure 测试验证。
- Spec 已勾选：`1`、`1.1`、`2`、`2.1`、`2.2`、`3`、`3.1`、`3.2`、`3.3`、`4`、`4.1`、`4.2`、`5`、`5.1`、`5.2`、`5.3`、`6`、`6.1`、`6.2`、`6.3`、`6.4`、`7`、`7.1`、`7.2`、`8`、`8.1`、`8.2`。

当前已知分析器警告主要包括未使用参数、using 排序和文件末尾空行，不影响当前构建结果。

## 5. 当前状态

最终全量验收已完成：

完成条件：

- WebApi.Tests 42/42、Architecture.Tests 23/23、Domain.Tests 27/27、Application.Tests 105/105、Infrastructure.Tests 126/126。
- CloudDrive.slnx 构建 0 warning/0 error；Spec quality 通过；git diff --check 通过。
- 剩余风险：工作区包含大量既有未提交改动，未执行提交或清理；NuGet restore 失败导致漏洞扫描为 Unknown；外部 115 只读探测被目标主机拒绝连接，真实上游契约验证为 Blocked/NotRun。

## 6. 最终交付物

- 可编译的当前版本代码。
- 通过验证的测试套件和集成测试证据。
- 更新后的 `.kiro/specs/115-cloud-drive-integration/tasks.md`。
- 代码审核追溯矩阵：[115-cloud-drive-integration-audit-matrix.md](115-cloud-drive-integration-audit-matrix.md)。
- 需求、设计、代码、API 和测试之间的审核矩阵。
- 本文档作为总目标、阶段状态和后续执行入口。

## 7. 可见任务编排

总控对话：`01a107d2-7011-73f2-9f48-03fb24de2a38`（本地 checkout）。

执行对话：

- `01a107d3-075d-7ee3-9c18-69f12c76f2d2`：错误适配注册表，负责 Spec 2.2。
- `01a107d3-52e9-7ce3-a5e7-f93f4b5d3a2c`：文件与回收站能力，负责 Spec 3.x。
- `01a107d3-b9d8-7b12-8b1a-cf2724cce55e`：预览、视频、云下载、分享和用户聚合，负责 Spec 4.x/5.x。

编排规则：总控负责阶段合同、写入窗口、验证和汇总；执行对话负责各自范围的分析、实现和测试。由于任务共用同一 checkout，任何写入必须由总控明确分配串行窗口，执行对话不得并发修改文件。
