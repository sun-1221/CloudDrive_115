<!-- Generated from .windsurf/rules/code-quality-tooling.md by scripts/sync-windsurf-rules.ps1. -->

# 代码质量工具链配置参考

本文档是工具链配置的参考手册，适用于项目进入编码阶段时查阅。

---

## 质量门禁分层策略

| 层次 | 工具 | 职责 |
|---|---|---|
| 格式与命名 | `.editorconfig` | 缩进、命名、using 排序、命名空间风格 |
| 通用代码质量 | `.NET Analyzers` | 空引用、异常、日志模板、CancellationToken 传递 |
| 代码一致性 | `StyleCop.Analyzers` | 文件结构、元素顺序、文档注释 |
| 架构约束 | BannedApiAnalyzers + NetArchTest + Roslynator | 分层依赖、禁止控制器直连仓储、禁止 AutoMapper |
| 运行时行为验证 | 集成测试 / 启动期验证 | Pipeline 顺序、领域事件分发时机、ProblemDetails 输出 |

---

## `.editorconfig` 推荐基线

```ini
root = true

[*]
charset = utf-8-bom
indent_style = space
indent_size = 4
insert_final_newline = true
trim_trailing_whitespace = true

[*.cs]
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = true
csharp_using_directive_placement = outside_namespace:warning
csharp_style_namespace_declarations = file_scoped:warning
csharp_new_line_before_open_brace = all
csharp_preserve_single_line_statements = false
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = false:suggestion
csharp_prefer_braces = true:warning

# 常用 .NET Analyzer 严重级别
dotnet_diagnostic.IDE0005.severity = warning
dotnet_diagnostic.IDE0065.severity = warning
dotnet_diagnostic.CA2016.severity = warning
dotnet_diagnostic.CA2208.severity = warning
dotnet_diagnostic.CA2254.severity = warning
dotnet_diagnostic.CA1031.severity = warning

# 当前项目是 ASP.NET Core 应用，不强制 ConfigureAwait(false)
dotnet_diagnostic.CA2007.severity = none

# CD0006: 异步方法缺少 Async 后缀（Roslynator）
dotnet_diagnostic.RCS1046.severity = warning

# BannedApiAnalyzers: 禁止符号
dotnet_diagnostic.RS0030.severity = warning

# 命名规则：接口 I 前缀
dotnet_naming_symbols.interfaces.applicable_kinds = interface
dotnet_naming_style.interface_prefix.required_prefix = I
dotnet_naming_style.interface_prefix.capitalization = pascal_case
dotnet_naming_rule.interfaces_should_have_prefix.symbols = interfaces
dotnet_naming_rule.interfaces_should_have_prefix.style = interface_prefix
dotnet_naming_rule.interfaces_should_have_prefix.severity = warning

# 命名规则：私有字段 _camelCase
dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private
dotnet_naming_style.private_field_style.required_prefix = _
dotnet_naming_style.private_field_style.capitalization = camel_case
dotnet_naming_rule.private_fields_should_be_underscore_prefixed.symbols = private_fields
dotnet_naming_rule.private_fields_should_be_underscore_prefixed.style = private_field_style
dotnet_naming_rule.private_fields_should_be_underscore_prefixed.severity = warning

# 命名规则：常量使用 PascalCase
dotnet_naming_symbols.constants.applicable_kinds = field, local
dotnet_naming_symbols.constants.required_modifiers = const
dotnet_naming_style.constants_pascal_case.capitalization = pascal_case
dotnet_naming_rule.constants_should_be_pascal_case.symbols = constants
dotnet_naming_rule.constants_should_be_pascal_case.style = constants_pascal_case
dotnet_naming_rule.constants_should_be_pascal_case.severity = warning

# 测试项目放宽文档与部分风格约束
[**/tests/**/*.cs]
dotnet_diagnostic.SA1600.severity = none
dotnet_diagnostic.SA1649.severity = none
```

---

## `Directory.Build.props` 当前配置

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>10-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <RunAnalyzersDuringBuild>true</RunAnalyzersDuringBuild>
    <RunAnalyzersDuringLiveAnalysis>true</RunAnalyzersDuringLiveAnalysis>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <CodeAnalysisTreatWarningsAsErrors>false</CodeAnalysisTreatWarningsAsErrors>
    <NoWarn>$(NoWarn);1591</NoWarn>
  </PropertyGroup>

  <ItemGroup Condition="'$(MSBuildProjectExtension)' == '.csproj'">
    <PackageReference Include="StyleCop.Analyzers" Version="1.1.118" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.BannedApiAnalyzers" Version="3.3.4" PrivateAssets="all" />
    <PackageReference Include="Roslynator.Analyzers" Version="4.*" PrivateAssets="all" />
    <AdditionalFiles Include="$(MSBuildThisFileDirectory)stylecop.json" Link="stylecop.json"
                     Condition="Exists('$(MSBuildThisFileDirectory)stylecop.json')" />
    <!-- 全局 BannedSymbols.txt（根目录，所有项目共享） -->
    <AdditionalFiles Include="$(MSBuildThisFileDirectory)BannedSymbols.txt" Link="BannedSymbols.txt"
                     Condition="Exists('$(MSBuildThisFileDirectory)BannedSymbols.txt')" />
    <!-- 项目级 BannedSymbols.txt（每层独立禁用符号） -->
    <AdditionalFiles Include="$(MSBuildProjectDirectory)\BannedSymbols.txt" Link="BannedSymbols.Local.txt"
                     Condition="Exists('$(MSBuildProjectDirectory)\BannedSymbols.txt')" />
  </ItemGroup>
</Project>
```

> **说明**：`AnalysisLevel` 固定为 `10-recommended`，避免 SDK 升级后 analyzer 集合变化导致 CI 噪音突增。

---

## `stylecop.json` 推荐样例

```json
{
  "settings": {
    "documentationRules": {
      "documentInterfaces": true,
      "documentExposedElements": true,
      "documentInternalElements": false,
      "documentPrivateElements": false
    }
  }
}
```

`stylecop.json` 必须通过 `Directory.Build.props` 的 `AdditionalFiles` 注入才能生效，仅放在根目录不够。

---

## `.NET Analyzers` 规则建议

### 优先启用

| 规则 | 建议级别 | 原因 |
|---|---|---|
| `CA2016` | warning/error | 架构大量依赖 `CancellationToken`，适合强制向下传递 |
| `CA2208` | warning/error | 参数校验异常应准确使用 `nameof(...)` |
| `CA2254` | warning/error | 日志统一使用静态模板 |
| `CA1031` | warning | 禁止随意 `catch (Exception)`，全局中间件允许例外 |
| `IDE0005` | warning/error | 保持 using 清洁 |

### 建议关闭

| 规则 | 原因 |
|---|---|
| `CA2007` | ASP.NET Core 应用不应强制 `ConfigureAwait(false)` |

---

## StyleCop 规则策略

### 启用

| 规则 | 建议级别 | 说明 |
|---|---|---|
| `SA1402` | warning | 一个文件只放一个顶级类型 |
| `SA1649` | warning | 文件名与主类型名一致 |
| `SA1516` | suggestion | 元素之间空行分隔 |
| `SA1600` | suggestion | 对外暴露 API 建议补文档（不建议设为 error） |

### 关闭

| 规则 | 原因 |
|---|---|
| `SA1309` | 与项目 `_privateField` 约定冲突 |
| `SA1200` | 与 `using` 放在命名空间外部的约定冲突 |
| `SA1633` | 文件头注释收益低，产生噪音 |

---

## CD 规则实施状态

| 规则 | 约束内容 | 实施工具 | 反馈时机 |
|---|---|---|---|
| `CD0001` | Domain 层禁止引用基础设施类型 | `BannedSymbols.txt` (Domain) | ✅ 编译时 |
| `CD0002` | Application 层禁止直接依赖 SqlSugar/缓存/HTTP 实现 | `BannedSymbols.txt` (Application) | ✅ 编译时 |
| `CD0003` | Controller 禁止直接注入仓储或 ORM 客户端 | NetArchTest | ✅ 测试时 |
| `CD0004` | 全项目禁止引用 AutoMapper | `BannedSymbols.txt` (全局) | ✅ 编译时 |
| `CD0005` | Command/Query/Handler 命名规范 | NetArchTest | ✅ 测试时 |
| `CD0006` | 异步方法缺少 `Async` 后缀 | Roslynator `RCS1046` | ✅ 编译时 |
| `CD0007` | Query Handler 中禁止写操作和 `IMediator.Publish()` | NetArchTest | ✅ 测试时 |
| `CD0008` | 映射扩展方法统一命名为 `ToDto()`/`ToEntity()` | NetArchTest | ✅ 测试时 |
| `CD0009` | `IServiceCollection` 扩展方法必须以 `Add` 开头 | NetArchTest | ✅ 测试时 |
| `CD0010` | Pipeline Behavior 注册顺序校验（含可选业务前置扩展点） | DI 容器测试 | ✅ 测试时 |

---

## BannedSymbols.txt 规范内容

### 全局禁用符号（根目录 `BannedSymbols.txt`，所有项目生效）

```text
# CD0004: 全项目禁止引用 AutoMapper
T:AutoMapper.IMapper; CD0004: 禁止使用 AutoMapper，请使用手动映射扩展方法 ToDto()/ToEntity()
T:AutoMapper.MapperConfiguration; CD0004: 禁止使用 AutoMapper
T:AutoMapper.Profile; CD0004: 禁止使用 AutoMapper
N:AutoMapper; CD0004: 禁止引用 AutoMapper 命名空间
```

### Domain 层禁用符号（`src/Domain/BannedSymbols.txt`）

```text
# CD0001: Domain 层禁止引用基础设施类型

# ORM
T:SqlSugar.ISqlSugarClient; CD0001: Domain 层禁止引用 SqlSugar
T:SqlSugar.SqlSugarClient; CD0001: Domain 层禁止引用 SqlSugar
T:SqlSugar.SqlSugarScope; CD0001: Domain 层禁止引用 SqlSugar

# ASP.NET Core
N:Microsoft.AspNetCore; CD0001: Domain 层禁止引用 ASP.NET Core

# Redis
T:StackExchange.Redis.IConnectionMultiplexer; CD0001: Domain 层禁止引用 Redis
T:StackExchange.Redis.ConnectionMultiplexer; CD0001: Domain 层禁止引用 Redis

# HTTP
T:System.Net.Http.HttpClient; CD0001: Domain 层禁止引用 HttpClient
T:System.Net.Http.IHttpClientFactory; CD0001: Domain 层禁止引用 IHttpClientFactory

# 分布式缓存
T:Microsoft.Extensions.Caching.Distributed.IDistributedCache; CD0001: Domain 层禁止引用 IDistributedCache

# FluentValidation
N:FluentValidation; CD0001: Domain 层禁止引用 FluentValidation

# Quartz
N:Quartz; CD0001: Domain 层禁止引用 Quartz
```

### Application 层禁用符号（`src/Application/BannedSymbols.txt`）

```text
# CD0002: Application 层禁止直接依赖 SqlSugar/缓存/HTTP 实现

# ORM 直连
T:SqlSugar.ISqlSugarClient; CD0002: Application 层禁止直接依赖 SqlSugar，请通过仓储接口
T:SqlSugar.SqlSugarClient; CD0002: Application 层禁止直接依赖 SqlSugar
T:SqlSugar.SqlSugarScope; CD0002: Application 层禁止直接依赖 SqlSugar

# 分布式缓存实现
T:Microsoft.Extensions.Caching.Distributed.IDistributedCache; CD0002: Application 层禁止直接依赖 IDistributedCache，请通过封装接口

# HTTP 客户端实现
T:System.Net.Http.HttpClient; CD0002: Application 层禁止直接依赖 HttpClient，请通过网关接口
T:System.Net.Http.IHttpClientFactory; CD0002: Application 层禁止直接依赖 IHttpClientFactory

# Redis
T:StackExchange.Redis.IConnectionMultiplexer; CD0002: Application 层禁止直接依赖 Redis
T:StackExchange.Redis.ConnectionMultiplexer; CD0002: Application 层禁止直接依赖 Redis
```

> **维护规则**：新增禁用符号时，同时更新对应 `BannedSymbols.txt` 文件和本文档。
> BannedSymbols.txt 格式：`T:` 禁用类型，`M:` 禁用方法，`N:` 禁用命名空间；分号后加自定义说明。

---

## 架构测试（`tests/Architecture.Tests`）

已建立独立的 `Architecture.Tests` 项目，使用 `NetArchTest.eNhancedEdition` + xUnit。

### 测试清单（15 个测试）

| 测试文件 | 覆盖规则 | 测试数 |
|---|---|---|
| `LayerDependencyTests.cs` | 层依赖方向（Domain/Application 禁止反向依赖） | 5 |
| `ControllerDependencyTests.cs` | CD0003: Controller 禁注入仓储/ORM | 2 |
| `NamingConventionTests.cs` | CD0005 命名 + CD0008 映射命名 + CD0009 Add 前缀 | 5 |
| `QueryHandlerSideEffectTests.cs` | CD0007: QueryHandler 禁写操作 | 2 |
| `PipelineBehaviorOrderTests.cs` | CD0010: Pipeline Behavior 注册顺序 | 2 |

---

## CI 门禁命令

```bash
dotnet restore
dotnet build --no-restore -warnaserror
dotnet test --no-build
dotnet format --verify-no-changes
```

---

## 不建议自动强制的规则（当前项目）

| 规则 | 原因 |
|---|---|
| `ConfigureAwait(false)` | 当前是 ASP.NET Core 应用，不是通用类库 |
| 所有公开成员必须有 XML 注释 | 应用型项目会产生较大噪音 |
| 所有 Handler 必须返回 `Result<T>` | 架构约定，适合通过代码模板和审查推进 |
| 所有模块必须完整 CQRS+DDD | 简单模块不需要把所有模式用满 |
