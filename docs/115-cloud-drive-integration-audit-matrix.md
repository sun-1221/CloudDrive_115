# 115 CloudDrive 集成代码审核追溯矩阵

审核范围：`.kiro/specs/115-cloud-drive-integration/{requirements,design,tasks}.md`、`Server/src/**`、`Server/tests/**`。

规则来源：

- `.kiro/specs/115-cloud-drive-integration/requirements.md`
- `.kiro/specs/115-cloud-drive-integration/design.md`
- `.kiro/specs/115-cloud-drive-integration/tasks.md`
- `.agents/skills/115web-project-rules/SKILL.md` 及其后端、测试、工具链 references
- `docs/115-api-docs.md` collected API evidence

## 需求追溯矩阵

| 需求范围 | 代码入口与关键实现 | D1/D2 | D3/D4 | D5/D6 | D7 | D8 | 结论 |
|---|---|---|---|---|---|---|---|
| R1–R3 Cookie 会话、配置与隔离 | `CookieSessionManager`、`CloudDriveAccountContextBehavior`、`CloudDriveApiClient` | 通过 | 通过 | 通过 | 通过 | 通过：Cookie/Auth/配置测试 | 通过 |
| R4–R6 文件列表、详情派生与字段映射 | `GetFileListQueryHandler`、`GetFileDetailQueryHandler`、`CloudDriveFileHandlerSupport`、`FileMapping` | 通过 | 通过 | 通过：`/files` 闭集 query | 通过 | 通过：文件 Handler/Mapping 测试 | 通过 |
| R8–R13 文件下载、目录、复制、重命名、星标 | `CloudDriveFileHandlerSupport`、对应 CommandHandler、`CloudDriveFileManager` | 通过 | 通过 | 通过：已采集 form/query 与成功 envelope | 通过 | 通过：请求形状与失败语义测试 | 通过 |
| R14 回收站只读列表 | `GetRecycleBinListQueryHandler`、`CloudDriveRecycleBinResponse` | 通过 | 通过 | 通过：`asc,format,limit,o,offset` 与字符串 item | 通过 | 通过：非空映射测试 | 通过 |
| R15 图片/文档临时预览 | `GetImagePreviewQueryHandler`、`GetDocPreviewQueryHandler`、`CloudDrivePreviewSupport` | 通过 | 通过 | 通过：复用 `/files/download` | 通过 | 通过：PreviewHandlerTests | 通过 |
| R16–R18 视频、字幕、播放历史 | `GetVideoPlayUrlCommandHandler`、`GetSubtitlesQueryHandler`、`GetVideoPlayProgressQueryHandler`、`CloudDriveVideoSupport` | 通过 | 通过 | 通过：`PlaybackSchemaStatus=available`，质量/音轨空数组 | 通过 | 通过：VideoHandlerTests | 通过 |
| R20 云下载三项读操作 | `GetOfflineTaskListQueryHandler`、`GetOfflineStatsQueryHandler`、`GetOfflineQuotaQueryHandler` | 通过 | 通过 | 通过：`clouddownload.115.com/web/` POST + query，无 body | 通过 | 通过：Offline/HTTP 请求测试 | 通过 |
| R22 我的分享与 R28 禁用写操作 | `GetMySharesQueryHandler`、`GetShareSendsQueryHandler`、`GetShareReceivesQueryHandler` | 通过 | 通过 | 通过：`slist` 映射，send/receive fail-closed | 通过 | 通过：Share/WebApi 禁用集成测试 | 通过 |
| R23 VIP、余额、空间与基础资料边界 | `UserAggregationService`、用户聚合 Handler、`GetUserInfoQueryHandler` | 通过 | 通过 | 通过：`space_info`、`type_nums`、`type_summury/pecent`、pay balance；基础资料为空 | 通过 | 通过：User/Auth 测试 | 通过 |
| R24–R25 限流与错误字段适配 | `IErrorFieldAdapter`、`ErrorFieldAdapter`、`CloudDriveApiClient` | 通过 | 通过 | 通过：method + host + exact path + required query，未知形态 fail-closed | 通过 | 通过：错误映射与请求测试 | 通过 |
| R26–R30 脱敏、缓存、任务完成与恢复 | `CloudDriveSensitiveFieldsFilter`、`CloudDriveMetadataCache`、`CloudDriveJobOrchestrator`、恢复协调器 | 通过 | 通过 | 通过：缓存失效、任务状态和禁用副作用边界 | 通过 | 通过：Cache/Job/Sensitive/WebApi 测试 | 通过 |
| R31 WebApi 错误协议与禁用端点 | `ExceptionHandlingMiddleware`、`ResultHttpExtensions`、CloudDrive Controllers | 通过 | 通过 | 通过：501 + `cloud_drive_api_not_collected` | 通过 | 通过：WebApi 42/42、Architecture 23/23 | 通过 |

## 关键上下文数据流闭环

| 字段/状态 | Source | 真实入口 | Transform | Sink/副作用 | 异常路径 | 测试是否会失败 | 结论 |
|---|---|---|---|---|---|---|---|
| 115 Cookie UID/CID/USSID | 管理员注入 | Auth Controller → `UpsertCookieSessionCommandHandler` | Data Protection 加密 → `CookieSessionManager` | Cookie Header 注入 `CloudDriveApiClient` | 缺失/失效进入配置或冷却错误，不发起上游调用 | CookieSecurity、LoginStatus、WebApi 敏感字段测试 | 闭环通过 |
| `CloudAccountUid` | 服务端账户上下文 | WebApi 用户上下文 → `CloudDriveAccountContextBehavior` | 解析并注入 Query/Command | Gateway、缓存键、限流键、任务归属 | 无 Active 会话返回 `cookie_not_active` | AccountContext、CacheConsistency、Auth 测试 | 闭环通过 |
| `state` 与上游错误字段 | 115 JSON envelope | `CloudDriveApiClient` | `ErrorFieldAdapter` 按 method/host/path/query 严格解析 | `CloudDrive*Exception` → ProblemDetails | 未匹配/类型变化为 `unknown_error_shape` | ErrorFieldAdapter、ErrorCodeMapping、WebApi 测试 | 闭环通过 |
| 临时下载/预览 URL | `/files/download` 成功响应 | Preview Handler | 仅构造临时 DTO，禁止持久化/日志 | WebApi 响应 | 不支持格式返回明确业务失败 | Preview 与 TemporaryResourceMarker 测试 | 闭环通过 |
| 禁用能力状态 | collected 闭集之外的入口 | Controller/Handler | `cloud_drive_api_not_collected` | HTTP 501 ProblemDetails | 不进入 Gateway、Job 或缓存失效 | CloudDriveCapabilityIntegrationTests、JobRecovery 测试 | 闭环通过 |
| 任务完成状态 | 已确认成功的上游 envelope | Handler → JobOrchestrator | 仅成功 envelope 标记 Completed | Job 持久化与恢复 | 未采集/占位失败保持失败 | JobOrchestrator/Recovery 测试 | 闭环通过 |

## 验证结果

- `dotnet build CloudDrive.slnx --no-restore -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false`：0 warning、0 error。
- `dotnet test Server/tests/Application.Tests/Application.Tests.csproj ...`：105/105。
- `dotnet test Server/tests/Infrastructure.Tests/Infrastructure.Tests.csproj ...`：126/126。
- `dotnet test Server/tests/WebApi.Tests/WebApi.Tests.csproj ...`：42/42。
- `dotnet test Server/tests/Architecture.Tests/Architecture.Tests.csproj ...`：23/23。
- `dotnet test Server/tests/Domain.Tests/Domain.Tests.csproj ...`：27/27。
- `powershell -NoProfile -File .kiro/scripts/Test-SpecQuality.ps1 -Spec .kiro/specs/115-cloud-drive-integration`：通过，Requirements 220、refs 220、headings 74。
- `git diff --check`：通过。
- `dotnet list CloudDrive.slnx package --vulnerable --include-transitive --format json`：未完成；NuGet restore 失败，漏洞状态为 `Unknown/NotRun`。
- 只读外部探测 `GET https://webapi.115.com/files?cid=0&limit=1&format=json`：未建立连接，目标计算机积极拒绝连接；未发送 Cookie、未写入数据。真实上游契约验证仍为 `Blocked/NotRun`。

未识别到需要真实外部 115 HTTP 调用才能完成的本地验收项；外部联机验证仍属于未执行证据，不作为本地测试通过的替代。
