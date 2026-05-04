---
description: 对后端代码进行全面审核，检查 DDD 分层、CQRS 模式、命名风格、测试规范等是否符合项目规范
---

# 代码审核工作流

## 使用方式

通过 `/code-audit-workflow` 调用，可附带以下参数：
- **审核范围**：指定目录或文件路径（如 `src/Application/CloudDrive/`），默认为全量 `Server/src/` + `Server/tests/`
- **审核模式**：`full`（全量）或 `quick`（仅自动化检查 + 高优规则），默认 `full`

---

## 第 0 步：确认审核范围与基线

1. 确认本次审核的文件范围（用户指定或全量）
2. 检查是否存在上一轮审核报告：
// turbo
   - 读取 `.kiro/specs/*/code-audit-history/latest.md`（如果存在）
3. 声明本次审核范围和涉及的层（Domain / Application / Infrastructure / WebApi / Tests）
4. 输出范围摘要：文件数量、涉及层、上轮基线结论

---

## 第 1 步：读取适用规范

根据审核范围涉及的层，读取对应规范文件：

### 1.1 必读（所有后端审核）
// turbo
- `.windsurf/rules/ddd-layer-architecture.md`
// turbo
- `.windsurf/rules/dotnet-naming-and-style.md`
// turbo
- `.windsurf/rules/code-quality-tooling.md`

### 1.2 按层选读
触碰 Domain 层时：
// turbo
- `.windsurf/rules/ddd-domain-patterns.md`

触碰 Application 层时：
// turbo
- `.windsurf/rules/cqrs-application-patterns.md`

触碰 Infrastructure 层时：
// turbo
- `.windsurf/rules/infrastructure-patterns.md`

触碰 WebApi 层时：
// turbo
- `.windsurf/rules/webapi-patterns.md`

触碰测试项目时：
// turbo
- `.windsurf/rules/unit-testing-standards.md`
// turbo
- `.windsurf/rules/integration-testing-standards.md`

触碰前端项目时：
// turbo
- `.windsurf/rules/frontend-general-standards.md`
// turbo
- `.windsurf/rules/frontend-development-standards.md`

---

## 第 2 步：自动化检查

按顺序执行以下自动化验证，记录每项的通过/失败状态：

### 2.1 编译检查
// turbo
```powershell
dotnet build .\CloudDrive.slnx --nologo --verbosity minimal
```
- 关注编译警告（特别是 BannedApiAnalyzers RS0030、Roslynator RCS1046、CA2016/CA2254）
- 记录警告数量和类型

### 2.2 测试执行
// turbo
```powershell
dotnet test .\CloudDrive.slnx --no-build --nologo --verbosity minimal
```
- 记录通过/失败/跳过数量
- 失败测试记为 🔴 高优问题

### 2.3 架构依赖扫描（grep 验证）

对每项 CD 规则执行 grep 扫描：

**CD0001 — Domain 层禁止引用基础设施类型**：
- 在 `Server/src/Domain/` 中搜索：`SqlSugar`、`Microsoft.AspNetCore`、`FluentValidation`、`Quartz`、`StackExchange.Redis`、`HttpClient`、`IDistributedCache`

**CD0002 — Application 层禁止直接依赖实现**：
- 在 `Server/src/Application/` 中搜索：`ISqlSugarClient`、`SqlSugarClient`、`IDistributedCache`、`HttpClient`、`IHttpClientFactory`、`IConnectionMultiplexer`

**CD0003 — Controller 禁止直接注入仓储/ORM**：
- 在 `Server/src/WebApi/Controllers/` 中搜索：`IRepository`、`ISqlSugarClient`、`SqlSugar`
- 检查 Controller 构造函数是否只注入 `ISender`

**CD0004 — 全项目禁止 AutoMapper**：
- 全项目搜索：`AutoMapper`、`IMapper`
- 检查所有 `.csproj` 中是否引用 AutoMapper 包

**CD0007 — QueryHandler 禁止写操作**：
- 在 `*QueryHandler*.cs` 中搜索：`Insert`、`Update`、`Delete`、`IMediator.Publish`、`IPublisher.Publish`

---

## 第 3 步：人工审核维度（8 维矩阵）

逐维度审查代码，对每个发现的问题标注严重级别和精确位置。

### D1 — DDD 分层边界

| 检查项 | 说明 |
|---|---|
| 层依赖方向 | Domain 无外部依赖 → Application 仅依赖 Domain → Infrastructure 实现接口 → WebApi 依赖 Application + Infrastructure |
| `.csproj` 引用 | 检查每个项目的 `ProjectReference` 是否符合依赖方向 |
| 包引用泄漏 | Domain.csproj 不应有 MediatR/SqlSugar 等包引用 |
| 实现类可见性 | Infrastructure 实现类是否使用 `internal sealed` |

### D2 — CQRS 与 Application 模式

| 检查项 | 说明 |
|---|---|
| Command/Query 命名 | `{动词}{名词}Command` / `Get{名词}Query` |
| Handler 命名 | `{Command/Query名}Handler` |
| Handler 修饰符 | `internal sealed class` |
| Result 返回 | Handler 返回 `Result<T>`，不直接抛业务异常 |
| Pipeline 顺序 | Logging → Validation → Transaction → DomainEventDispatch |
| Query 无副作用 | QueryHandler 中无写操作、无事件发布 |

### D3 — 领域模型规范

| 检查项 | 说明 |
|---|---|
| 实体封装 | 私有构造函数 + 静态工厂 `Create()` |
| 属性保护 | `private set` 或 `init`，无公开 setter |
| 状态变更 | 通过领域方法，非直接赋值 |
| 值对象 | `sealed record`，构造函数内验证不变量 |
| 聚合根 | 继承 `AggregateRoot<TId>`，事件通过 `RaiseDomainEvent()` |
| 仓储接口 | 每个聚合根一个接口，定义在 Domain 层 |

### D4 — 命名与代码风格

| 检查项 | 说明 |
|---|---|
| 类/方法/属性 | PascalCase |
| 私有字段 | `_camelCase` |
| 异步方法 | `Async` 后缀 + `CancellationToken` 参数 |
| 文件范围命名空间 | `namespace X.Y.Z;` 而非块状 |
| 大括号 | Allman 风格，if/for/while 必须有大括号 |
| var 使用 | 仅在右侧类型明确时使用 |

### D5 — Infrastructure 规范

| 检查项 | 说明 |
|---|---|
| SqlSugar 生命周期 | Scoped 注册 |
| 缓存封装 | 不直接暴露 `IDistributedCache` 给 Application 层 |
| HTTP 客户端 | Named Client + Polly 策略，读写分离 |
| DI 扩展方法 | 方法名以 `Add` 开头 |
| 事务管理 | 由 Pipeline Behavior 统一管理，仓储内不自行开启 |

### D6 — WebApi 规范

| 检查项 | 说明 |
|---|---|
| Controller 注入 | 仅 `ISender`，使用 Primary Constructor |
| Action 职责 | 构造 Command/Query → Send → 映射响应，三件事 |
| 异常中间件 | 最先注册，统一 ProblemDetails 输出 |
| 映射方式 | `ToDto()` / `ToEntity()` 扩展方法，无 AutoMapper |
| 中间件顺序 | Exception → Auth → Authorization → MapControllers |

### D7 — 测试规范

| 检查项 | 说明 |
|---|---|
| 命名 | `{被测方法}_{场景}_{期望结果}` |
| AAA 结构 | Arrange/Act/Assert 注释分隔 |
| 隔离 | 手写 Fake/Stub，无真实 DB/HTTP |
| 属性测试 | 不变量/映射/中间件性质用 `[Property]` |
| Theory vs Fact | 数据驱动用 Theory，单场景用 Fact |

### D8 — 代码质量工具链

| 检查项 | 说明 |
|---|---|
| BannedSymbols.txt | 全局 + 各层独立文件是否存在且内容正确 |
| .editorconfig | 关键规则是否配置（CA2016/CA2254/RCS1046/RS0030） |
| StyleCop | SA1309/SA1200/SA1633 是否已关闭 |
| 架构测试项目 | NetArchTest 测试是否覆盖 CD0003/CD0005/CD0007-CD0009 |

---

## 第 4 步：汇总审核报告

### 4.1 问题分级

| 级别 | 标记 | 含义 | 处理要求 |
|---|---|---|---|
| 🔴 高 | H | 违反架构红线、编译失败、测试失败 | 必须立即修复 |
| 🟠 中高 | MH | 违反 CD 规则但未触发编译/测试门禁 | 当前迭代内修复 |
| 🟡 中 | M | 命名/风格不一致、缺少文档注释 | 建议修复 |
| 🟢 低 | L | 代码可优化但不违规 | 可选改进 |

### 4.2 报告格式

按以下模板输出审核结论：

```markdown
# 代码审核报告

**审核日期**：YYYY-MM-DD HH:MM:SS
**审核范围**：{范围描述}
**上轮基线**：{上轮报告路径或"首次审核"}
**结论**：Pass ✅ / Pass ⚠️ / NeedFix ❌

## 自动化检查

| 项目 | 结果 | 备注 |
|---|---|---|
| 编译 | ✅/❌ | 警告数 N |
| 测试 | ✅/❌ | N passed / N failed |
| CD0001 | ✅/❌ | |
| CD0002 | ✅/❌ | |
| ... | | |

## 问题清单

| # | 级别 | 维度 | 位置 | 描述 | 建议修复 |
|---|---|---|---|---|---|
| P1 | 🔴 | D1 | `文件:行号` | 问题描述 | 修复建议 |
| P2 | 🟠 | D2 | `文件:行号` | 问题描述 | 修复建议 |

## 与上轮对比

| 状态 | 数量 | 说明 |
|---|---|---|
| [修复] | N | 上轮问题已修复 |
| [遗留] | N | 上轮问题仍存在 |
| [新增] | N | 本轮新发现 |
| [回归] | N | 上轮已修复但本轮重现 |

## 统计

- 总检查项：N
- 通过：N（X%）
- 问题：🔴 N / 🟠 N / 🟡 N / 🟢 N
```

---

## 第 5 步：持久化报告

1. 确定报告归属的 spec（根据审核范围推断，如 `115-cloud-drive-net10`）
2. 将报告写入：
   - `.kiro/specs/{spec-name}/code-audit-history/YYYY-MM-DD-HHMMSS.md`
   - `.kiro/specs/{spec-name}/code-audit-history/latest.md`（覆盖为本轮副本）
3. 如果无法确定 spec 归属，写入：
   - `.kiro/code-audit-history/YYYY-MM-DD-HHMMSS.md`
   - `.kiro/code-audit-history/latest.md`

---

## 第 6 步：修复跟进（可选）

如果用户要求修复：

1. 按优先级排序：🔴 → 🟠 → 🟡 → 🟢
2. 每修一个问题：
   a. 引用问题编号和位置
   b. 执行 `/code-standards-workflow` 读取对应规范
   c. 实施最小化修复
   d. 运行回归测试验证
3. 修复完成后重新执行第 2 步自动化检查，确认无回归
4. 更新审核报告，标注已修复项

---

## 防遗漏要点

1. **必先读基线**：有上轮报告时必须先读取，对比变化
2. **严禁抽样**：全量模式下每个文件都必须扫过，不能"挑几个看看"
3. **每个问题必须附精确位置**：文件路径 + 行号，不允许模糊描述
4. **新发现必须反思**：如果发现上轮漏掉的问题，说明遗漏原因
5. **自动化检查不可跳过**：即使人工审核发现很多问题，自动化检查仍须执行
6. **CD 规则全覆盖**：CD0001-CD0009 每条都必须验证，不能省略
