---
name: 115web-project-rules
description: 当用户在 115web 仓库中要求编写、修改、实现任务、修复、重构、补测试、格式化、审核代码、按规范检查、改 .kiro spec，或触碰 Server/.NET、web/Vue、collector/TypeScript CLI、测试、质量工具配置、.windsurf/.agents 规则迁移、Skill 审核/优化/同步来源检查时使用此 Skill。Use for 115web implementation, bug fixes, refactors, tests, code reviews, architecture checks, spec edits, project-rule migration, and 115web skill/rule maintenance. Do not use for unrelated repositories, generic .NET/frontend/TypeScript Q&A, or discussions that do not touch 115web artifacts.
---

# 115web 项目规则

## 目标

按任务范围选择并执行仓库规则，因为一次性加载全部参考文件会稀释关键约束。只依据用户要求、适用 reference、当前 spec 和现有代码识别约束，不自行增加项目规范。

## 激活边界

- **主场景**：115web 仓库内的代码实现、Bug 修复、重构、测试补充、代码审核、架构检查、Spec 编辑、collector CLI 维护、规则迁移、Skill 审核/优化和项目规范同步。
- **路径触发**：`Server/**`、`web/**`、`collector/**`、`.kiro/specs/**`、`.windsurf/**`、`.agents/skills/115web-project-rules/**`。
- **不处理**：非 115web 仓库任务、纯通用 .NET/前端知识问答、只讨论业务取舍且不要求修改或审核仓库 artifact、与本仓库规则无关的外部 API 文档解释、其他仓库中同名目录或相似技术栈但不触碰 115web artifact 的任务。

## 执行流程

1. 确认任务类型、目标路径和涉及层级。
2. 按“规则选择”读取所有适用 reference。
3. 编辑前提炼一份仅覆盖本次改动的最小核对清单，因为过大的清单容易遮蔽真正相关的规则。
4. 遵循现有代码模式实施最小范围修改。
5. 按“验证”执行静态扫描、构建和测试。
6. 收尾时列出改动、适用规则、验证结果和无法验证的剩余风险。

详细的原始编码流程见 [代码编写规范工作流](references/workflows/code-standards-workflow.md)。

若只做代码审核，先按审核范围拆分路径：后端审核才读取 [代码审核工作流](references/workflows/code-audit-workflow.md)；`web/**`、`collector/**` 或 `.kiro/specs/**` 审核只读取对应规则并输出问题表。用户未给审核范围时，先用用户上下文、`git diff --name-only` 和路径规则定位范围；仍无法定位时默认审核当前已改文件并在最终回答说明范围假设，因为无范围的全仓扫描容易混入无关问题。默认不写入审核报告文件，因为只读审核不应产生仓库改动；用户明确要求保存报告时才落盘。若审核后需要修复，再进入上述编辑流程。

## 规则选择

### 后端通用

触碰任何 `Server/**/*.cs` 时读取：

- [DDD 分层架构](references/rules/ddd-layer-architecture.md)
- [.NET 命名与风格](references/rules/dotnet-naming-and-style.md)
- [XML 文档注释](references/rules/xml-documentation.md)

### 后端分层

- `Server/src/Domain/**/*.cs`：读取 [DDD 领域模式](references/rules/ddd-domain-patterns.md)。
- `Server/src/Application/**/*.cs`：读取 [CQRS Application 模式](references/rules/cqrs-application-patterns.md)。
- `Server/src/Infrastructure/**/*.cs`：读取 [Infrastructure 模式](references/rules/infrastructure-patterns.md)。
- `Server/src/WebApi/**/*.cs`：读取 [WebApi 模式](references/rules/webapi-patterns.md)。

### 测试

- `Server/tests/**/*.cs`：读取 [单元测试规范](references/rules/unit-testing-standards.md)。
- 涉及真实宿主、数据库、HTTP、缓存或跨层验证：额外读取 [集成测试规范](references/rules/integration-testing-standards.md)。

### Web 前端

触碰 `web/**` 下的 Vue 前端代码、配置或测试时读取：

- [通用前端规范](references/rules/frontend-general-standards.md)
- [115web 前端开发规范](references/rules/frontend-development-standards.md)

### Collector

触碰 `collector/**` 下的 TypeScript CLI、Playwright 自动化、采集解析、导出或配置代码时读取 [Collector 开发规范](references/rules/collector-development-standards.md)。不要为 collector 加载 Vue、Pinia、Tailwind 或浏览器页面规则，因为它是 Node.js CLI 采集工具而不是 `web/**` 前端应用。

### Spec

创建或修改 `.kiro/specs/**/*.md` 时读取 [Spec 格式规范](references/rules/spec-format.md)。

### 工具链

触碰 `.editorconfig`、`Directory.Build.props`、`Directory.Packages.props`、`BannedSymbols.txt`、analyzer、StyleCop、架构测试或 CI 门禁时读取 [代码质量工具链](references/rules/code-quality-tooling.md)。

### 综合审核

执行全量或快速后端代码审核时读取：

- [代码审核工作流](references/workflows/code-audit-workflow.md)
- 审核范围对应的所有分层规则
- [代码质量工具链](references/rules/code-quality-tooling.md)

执行 `web/**` 审核时读取 Web 前端规则；执行 `collector/**` 审核时读取 Collector 开发规范；执行 `.kiro/specs/**` 审核时读取 Spec 格式规范。混合范围审核先按路径拆分，再分别应用对应规则。

## 冲突处理

按以下顺序处理约束：

1. 用户对当前任务的明确要求。
2. 当前功能的 `requirements.md`、`design.md`、`tasks.md` 和外部接口契约。
3. 路径最具体的项目规则。
4. 通用项目规则。
5. 仓库现有实现模式。

发现规则、spec、实际接口或工具配置互相冲突时，不要静默选择。先核对当前代码和配置；能够以更具体依据解决时按该依据执行，否则向用户指出冲突。

## 验证

按改动范围选择最窄但充分的验证：

- 后端默认先运行 `dotnet build CloudDrive.slnx --no-restore`；若因依赖未还原失败，运行 `dotnet restore CloudDrive.slnx` 后重试同一 build。
- 后端测试先用 `rg --files Server/tests -g '*.csproj'` 定位受影响测试项目，再运行 `dotnet test Server/tests/<Project>/<Project>.csproj --no-build --nologo --verbosity minimal`；涉及多层时逐个运行对应测试项目，无法定位时运行 `dotnet test CloudDrive.slnx --no-build --nologo --verbosity minimal`。
- `web/**` 默认在 `web` 目录按顺序运行 `npm run type-check`、`npm run lint`、`npm run test`；涉及打包、路由或构建配置时再运行 `npm run build`。
- `collector/**` 默认在 `collector` 目录按顺序运行 `yarn typecheck`、`yarn lint`；涉及发布产物或 CLI 入口时再运行 `yarn build`。
- Spec 修改检查章节、编号、术语和 requirements/design/tasks 双向追溯。
- 后端审核任务执行工作流中的自动扫描，并记录无法执行的检查；`web/**`、`collector/**` 和 Spec 审核只执行对应路径的静态扫描、脚本和人工规则核对。
- 不把 build/test 通过等同于规范通过；另外核对命名、注释、分层、测试结构和禁止引用。
- 若测试项目、包管理脚本或质量命令不存在，先核对项目文件和 `package.json`/`yarn.lock`；确认本次范围没有对应验证入口时，在最终回答记录未执行原因，而不是改用无关命令。

不要并行运行会竞争同一 `bin/obj`、前端输出目录或包管理器锁文件的命令，因为这些命令共享中间产物时容易互相污染结果。

## 输出模板

实现或修复任务的最终回答使用这个最小结构：

```markdown
已完成：[一句话说明结果]

改动：
- [文件/模块]：[变更摘要]

适用规则：[读取过的 reference 或规则组]
验证：[命令] -> [结果]
未验证/风险：[没有则写“无”]
```

代码审核任务使用这个问题表；先列问题，再给简短总结：

```markdown
| # | 级别 | 维度 | 位置 | 问题 | 建议 |
|---|---|---|---|---|---|
| P1 | 高/中/低 | [规则维度] | [文件:行号] | [问题描述] | [修复建议] |
```

代码审核未发现问题时仍先给结论，再列适用规则和验证：

```markdown
未发现违反本次适用规则的问题。

适用规则：[读取过的 reference 或规则组]
验证：[命令/人工核对] -> [结果]
未验证/风险：[没有则写“无”]
```

规则、Skill 或规范同步审核使用这个最小结构：

```markdown
结论：[通过/有问题/无法确认]

问题：
| # | 级别 | 维度 | 位置 | 问题 | 建议 |
|---|---|---|---|---|---|
| P1 | 高/中/低 | [触发/篇幅/同步/路径/验证] | [文件:行号] | [问题描述] | [修复建议] |

适用规则：[读取过的 Skill/reference/源规则]
验证：[链接检查/生成物标记检查/触发评估记录] -> [结果]
未验证/风险：[没有则写“无”]
```

## 避坑

- 当需要修改 `.agents/skills/115web-project-rules/references/**` 时，先查看首行是否为 `<!-- Generated from .windsurf/* by scripts/sync-windsurf-rules.ps1. -->`；这表示文件会被同步脚本覆盖，因此应修改 `.windsurf` 源文件后重新同步。
- 当审核任务只读取主 `SKILL.md` 时，通常会漏掉命名、分层、测试等细则；主文件只负责导航，因此需要读取审核范围对应 reference 后再下结论。
- 当多个 Skill 同时适用时，先执行用户显式点名的 Skill，再执行本项目规则 Skill 的路径与验证约束；这样既保留专门审核方法，也不会遗漏 115web 的同步来源和落盘规则。
- 当用户说“审核代码”但范围是 `web/**`、`collector/**` 或 `.kiro/specs/**` 时，不要套用后端代码审核工作流；该工作流的自动扫描和报告结构只覆盖后端分层规则。
- 当用户没有明确要求保存审核报告时，不要创建 `code-audit-history` 文件；只读审核默认在最终回答中列出问题，因为隐式落盘会制造用户未请求的仓库改动。
- 当验证命令失败时，先检查错误是否来自未 restore、依赖缺失、SDK/Node/Yarn 不可用或网络受限；这些属于环境或依赖问题，应按授权补齐依赖或记录无法验证，不能误报为代码或 Skill 不合规。

## 同步来源

reference 由 `.windsurf/rules` 和 `.windsurf/workflows` 转换生成。源规则更新后，在仓库根目录运行：

```powershell
& .\.agents\skills\115web-project-rules\scripts\sync-windsurf-rules.ps1
```

脚本会移除 Windsurf 的 YAML trigger 头和 `// turbo` 标记，并将工作流中的旧规则路径改为本 skill 的 reference 路径。不要直接修改生成的 reference；应修改 `.windsurf` 源文件后重新同步。

## 触发评估样例

应触发：

- “按规范实现下一个 task”
- “修一下 `Server/src/Application/...Handler.cs`，顺便补测试”
- “审核 WebApi Controller 有没有违反分层”
- “把 `.windsurf` 规则同步成 Codex skill”
- “审核 `.agents/skills/115web-project-rules/SKILL.md` 的触发描述”
- “改 `.kiro/specs/115-cloud-drive-net10/tasks.md` 的追溯”
- “web 里这个 Vue 组件为什么状态不刷新，帮我修”

不应触发：

- “解释一下 CQRS 是什么”，且没有 115web 文件或规则上下文。
- “帮我看另一个仓库的 React 组件”。
- “115 开放平台某接口是什么意思”，且不要求更新本仓库 spec 或代码。
- “在另一个 .NET 项目里帮我设计 DDD 分层”，且没有触碰 115web artifact。
- “Vue watch 为什么没触发”，且没有 `web/**`、`collector/**` 或本仓库上下文。

调整 description、激活边界或路径触发后，用 Train/Validation 分离的真实查询复测：至少 5 条应触发和 3 条相近不应触发查询，每条在全新会话运行 3 次，记录 trigger rate。Train set 用于调整 description；Validation set 只用于最终确认。任一应触发查询低于 0.5 或任一不应触发查询高于等于 0.5 时，继续收窄或扩展触发词。

没有完成全新会话三次实测时，不要填写 T/F 结果或计算 Trigger rate；在最终回答中写“触发评估未实测”，因为伪造触发率会让后续 description 调整失去基线。

```markdown
| 集合 | 查询 | 预期 | 3 次结果 | Trigger rate | 结论 | 调整 |
|---|---|---|---|---|---|---|
| Train/Validation | [真实用户查询] | 触发/不触发 | [T/F/T] | 0.67 | 正确/漏触发/误触发 | [description 或边界调整] |
```

当前基线记录：

```markdown
| 集合 | 查询 | 预期 | 3 次结果 | Trigger rate | 结论 | 调整 |
|---|---|---|---|---|---|---|
| Train | 按规范实现下一个 task | 触发 | 未实测 | 未实测 | 待确认 | 无 |
| Train | 修一下 `Server/src/Application/...Handler.cs`，顺便补测试 | 触发 | 未实测 | 未实测 | 待确认 | 无 |
| Train | 审核 WebApi Controller 有没有违反分层 | 触发 | 未实测 | 未实测 | 待确认 | 无 |
| Train | 审核 `.agents/skills/115web-project-rules/SKILL.md` 的触发描述 | 触发 | 未实测 | 未实测 | 待确认 | 已补 description |
| Validation | web 里这个 Vue 组件为什么状态不刷新，帮我修 | 触发 | 未实测 | 未实测 | 待确认 | 无 |
| Validation | 解释一下 CQRS 是什么 | 不触发 | 未实测 | 未实测 | 待确认 | 无 |
| Validation | 帮我看另一个仓库的 React 组件 | 不触发 | 未实测 | 未实测 | 待确认 | 无 |
| Validation | Vue watch 为什么没触发 | 不触发 | 未实测 | 未实测 | 待确认 | 无 |
```
