# 代码审核问题修复记录：115-cloud-drive-net10

**修复日期**: 2026-05-03
**基线报告**: `code-audit-history/2026-04-20-150000.md`
**修复范围**: H1、M1、M2、M3
**验证命令**: `dotnet test CloudDrive.slnx --verbosity minimal`
**验证结果**: 24/24 通过 ✅

---

## 修复结论

| ID | 原级别 | 原问题 | 修复状态 | 说明 |
|----|--------|--------|----------|------|
| H1 | High | HTTP 超时默认值 30s ≠ 需求 10s | ✅ 已修复 | `DefaultTimeout` 改为 10 秒 |
| M1 | Medium | Program.cs 未调用 InitTables() | ✅ 框架入口已补强 | 新增 `AddSqlSugarCodeFirst(params Type[])` 启动建表注册入口，业务模块传入实体类型后由 HostedService 执行 |
| M2 | Medium | WebApi.csproj 直接引用 SqlSugarCore | ✅ 已修复 | `DbType` 解析迁移到 Infrastructure；WebApi 移除 `SqlSugarCore` 包引用与 `using SqlSugar` |
| M3 | Medium | Domain 层 4 个孤儿文件 | ✅ 已确认归属 | 这些类型已在 `115-cloud-drive-integration` spec 的 tasks/design 中有明确追溯，非 net10 框架需求孤儿代码 |

---

## 变更清单

### H1：HTTP 超时默认值修复

- **文件**: `Server/src/Infrastructure/Http/HttpClientSetup.cs:23`
- **变更**: `DefaultTimeout` 从 30 秒改为 10 秒
- **需求依据**: `requirements.md:139-140` 要求读/写请求默认超时 10 秒

### M1：CodeFirst 启动建表入口补强

- **文件**:
  - `Server/src/Infrastructure/Persistence/SqlSugarSetup.cs`
  - `Server/src/Infrastructure/Persistence/SqlSugarCodeFirstHostedService.cs`
  - `Server/src/Infrastructure/Persistence/SqlSugarCodeFirstOptions.cs`
- **变更**:
  - 新增 `AddSqlSugarCodeFirst(params Type[] entityTypes)` 扩展方法
  - 新增内部 `IHostedService`，应用启动时调用 `SqlSugarSetup.InitTables(...)`
- **边界说明**:
  - net10 spec 是框架层，不应硬编码 `CookieSession` 等 integration 业务实体
  - 当前 `CookieSession` 尚未按 integration tasks 添加 SqlSugar 主键/表字段特性，直接自动建表存在迁移风险
  - 业务模块完成实体持久化标注后，应在模块 DI 扩展中调用 `AddSqlSugarCodeFirst(typeof(...))`

### M2：WebApi 去 ORM 直接包依赖

- **文件**:
  - `Server/src/Infrastructure/Persistence/SqlSugarSetup.cs`
  - `Server/src/WebApi/Program.cs`
  - `Server/src/WebApi/WebApi.csproj`
- **变更**:
  - 新增 `AddSqlSugar(string connectionString, string? dbTypeName = null)` 重载
  - `DbType` 解析迁移到 Infrastructure 层内部
  - `Program.cs` 不再引用 `SqlSugar` 命名空间
  - `WebApi.csproj` 移除 `SqlSugarCore` 包引用

### M3：业务类型追溯确认

- **涉及类型**:
  - `CloudDriveJobStatus`
  - `CloudDriveJobType`
  - `CookieSessionStatus`
  - `CookieTriplet`
- **确认依据**:
  - `.kiro/specs/115-cloud-drive-integration/tasks.md:12-16`
  - `.kiro/specs/115-cloud-drive-integration/tasks.md:18-25`
- **处理结论**:
  - 不删除代码
  - 归属 integration spec，后续由 integration 任务继续补全实体/仓储/测试

---

## 回归验证

| 检查项 | 结果 |
|--------|------|
| `dotnet test CloudDrive.slnx --verbosity minimal` | ✅ 24/24 通过 |
| Application 层无 SqlSugar/Cache/Http 直接依赖 | ✅ grep 无结果 |
| WebApi 移除 `SqlSugarCore` 包引用 | ✅ 已移除 |
| WebApi 移除 `using SqlSugar` / `DbType` | ✅ 已移除 |
| 服务定位器使用 | ✅ grep 无结果 |

> 备注：测试过程中出现 NuGet 漏洞数据源访问警告 `NU1900`，原因是无法访问 `https://api.nuget.org/v3/index.json`，不影响本地编译和测试通过。

---

## 当前状态

**NeedFix → Fixed ✅**

- 阻塞问题 H1 已修复
- 架构问题 M2 已修复
- M1 已补齐框架级可调用入口，避免在框架层硬编码业务实体
- M3 已确认归属 integration spec，不作为 net10 框架代码缺陷处理
