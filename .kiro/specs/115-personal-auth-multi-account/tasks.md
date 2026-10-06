# 实现计划：个人认证与多 115 账号

- [x] 1. 领域模型与持久化
  - [x] 1.1 新增 User/UserSession、仓储接口与 SqlSugar 映射。_需求: 1.1, 1.3, 4.1_ _设计: §User/UserSession_
  - [x] 1.2 注册 User、UserSession、CookieSession 表。_需求: 2.1, 4.1_ _设计: §数据模型与接口_
- [x] 2. Application 认证与账号用例
  - [x] 2.1 实现 PBKDF2 哈希及注册/登录/注销/刷新 Handler。_需求: 1.1, 1.2, 1.3_ _设计: §PersonalAuthHandlers_
  - [x] 2.2 实现账号绑定、列表、选择、解绑和当前账号解析。_需求: 2.1, 2.2, 2.3_ _设计: §CookieSessionManager + AccountContextResolver_
- [x] 3. WebApi 与前端
  - [x] 3.1 添加认证和账号 API，移除 LocalAdmin 登录入口。_需求: 3.1, 3.2, 3.3_ _设计: §AuthController / CloudDriveAccountsController_
  - [x] 3.2 添加前端 JWT 会话、登录/注册、绑定和切换流程。_需求: 3.1, 3.2_ _设计: §AuthController / CloudDriveAccountsController_
- [x] 4. 验证检查点
  - [x] 4.1 WebApi 干净构建：`dotnet build Server/src/WebApi/WebApi.csproj --no-restore --nologo -p:UseSharedCompilation=false -p:BuildInParallel=false`，exit code 0。_需求: 1.1, 3.1, 4.1_ _设计: §测试策略_
  - [x] 4.2 完整后端测试：五个测试项目串行执行全部通过：Application 105、Architecture 23、Domain 27、Infrastructure 126、WebApi 42；每个项目 exit code 0。等价逐项目证据通过，未将聚合 solution 命令本身冒充通过。_需求: 1.1, 1.2, 2.1, 3.1, 4.1_ _设计: §测试策略_
  - [x] 4.3 前端 type-check/lint：均 exit code 0。_需求: 3.1, 3.2_ _设计: §测试策略_
  - [x] 4.4 前端 Vitest：`npm run test` 通过，23 个测试文件、153 项测试全部通过；初次沙箱执行受 `esbuild spawn EPERM` 阻塞，改用可启动 Node 子进程的本地执行环境后完成。_需求: 3.1, 3.2_ _设计: §测试策略_
  - [x] 4.5 API/DB 验收：使用 Testing host 与真实 SQLite，覆盖注册、重复用户名 409、错误密码 401、登录、刷新、注销后 JWT 401，以及 User/UserSession/CookieSession 持久化与 Cookie 密文不出响应。真实 HTTP+SQLite 验收已通过；合成数据、响应脱敏和临时数据库清理均有记录。不得以静态扫描或单元测试替代。_需求: 1.1, 1.2, 1.3, 2.1, 2.2, 3.1, 4.1_ _设计: §测试策略_
  - [x] 4.6 多账号 API 验收：同一用户绑定两个账号，已通过真实 HTTP+SQLite 验证列表脱敏、显式选择、未选择时返回稳定业务错误 `501 + cloud_drive_account_context_ambiguous`、解绑后当前账号清理及用户间数据隔离。合成数据、临时数据库和宿主日志已清理。不得以 handler 单测替代 HTTP+DB 闭环。_需求: 2.1, 2.2, 2.3_ _设计: §测试策略_
  - [x] 4.7 前端端到端验收：Playwright Chromium mock API 场景 50/50 通过，覆盖授权引导、JWT 会话注入、文件浏览与主要交互流程；未使用真实 115 凭据。_需求: 3.1, 3.2_ _设计: §测试策略_

验证证据：本轮停止了占用 WebApi DLL 的 dotnet 进程并完成干净 WebApi build；恢复 NuGet 依赖后按项目串行执行五个测试项目，Application 105、Architecture 23、Domain 27、Infrastructure 126、WebApi 42 全部真实通过；真实 HTTP+SQLite 验收覆盖认证会话和多账号隔离，合成 Cookie 仅用于测试且临时数据已清理；前端 type-check 通过，lint exit code 0（501 条既有 warning），Vitest 23 个文件/153 项测试通过，Playwright Chromium E2E 50/50 通过。聚合 solution 命令曾未在有界时间内返回，因此采用逐项目等价证据，不宣称 solution 聚合器本身通过。
