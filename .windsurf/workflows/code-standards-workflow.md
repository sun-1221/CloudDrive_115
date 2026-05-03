---
description: 编写或修改任何代码前的规范读取与核对流程
---

# 代码编写规范流程

## 1. 读取规范（硬性前置）

**在编写或修改任何代码之前**，必须根据要触碰的文件路径，读取对应的规范文件。

### 1.1 始终读取（所有 .cs 文件）

// turbo
- `.windsurf/rules/xml-documentation.md`（触发: `**/*.cs`）
// turbo
- `.windsurf/rules/dotnet-naming-and-style.md`（触发: `**/*.cs`）
// turbo
- `.windsurf/rules/ddd-layer-architecture.md`（全局架构约束）

### 1.2 按层选读（后端 src/）

触碰 `src/Domain/**/*.cs` 时：
// turbo
- `.windsurf/rules/ddd-domain-patterns.md`

触碰 `src/Application/**/*.cs` 时：
// turbo
- `.windsurf/rules/cqrs-application-patterns.md`

触碰 `src/Infrastructure/**/*.cs` 时：
// turbo
- `.windsurf/rules/infrastructure-patterns.md`

触碰 `src/WebApi/**/*.cs` 时：
// turbo
- `.windsurf/rules/webapi-patterns.md`

### 1.3 测试文件（tests/）

触碰 `tests/**/*.cs` 时：
// turbo
- `.windsurf/rules/unit-testing-standards.md`

如果涉及集成测试或宿主测试：
// turbo
- `.windsurf/rules/integration-testing-standards.md`

### 1.4 前端开发

触碰前端项目文件时：
// turbo
- `.windsurf/rules/frontend-general-standards.md`
// turbo
- `.windsurf/rules/frontend-development-standards.md`

### 1.5 按需参考

遇到代码质量工具配置（StyleCop、BannedSymbols、editorconfig 等）相关问题时：
// turbo
- `.windsurf/rules/code-quality-tooling.md`

## 2. 对照规范核对

读取完所有适用规范后，在动手写代码前，逐项核对规范中的要求。重点关注：

- **命名规范**：方法名、类名、文件名是否符合规范
- **注释规范**：XML 文档注释、AAA 区域注释是否齐全
- **架构约束**：层依赖方向、禁止引用项是否遵守
- **测试规范**：Theory vs Fact vs Property 的选择、隔离要求、禁止项
- **前端规范**：组件结构、状态管理、API 调用模式

## 3. 编写代码

按上述核对清单逐项满足后再开始写代码。

## 4. 回归验证

后端项目：
// turbo
- 运行 `dotnet test .\tests\{项目}\{项目}.csproj --nologo --verbosity minimal`
- 确认 0 失败后再提交结果

前端项目：
- 运行对应的 lint / type-check / test 命令
- 确认无错误后再提交结果
