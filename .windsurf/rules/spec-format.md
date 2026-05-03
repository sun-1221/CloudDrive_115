---
trigger: glob
globs: .kiro/specs/**/*.md
---

# Spec 文件格式规范

本规范定义 `.kiro/specs/` 下所有 spec 文件的格式标准，确保 AI 生成与人工编写的 spec 结构一致、可追溯。

---

## 目录结构

每个 spec 对应一个子目录：

```
.kiro/specs/<spec-name>/
├── .config.kiro          # spec 元数据（JSON）
├── requirements.md       # 需求文档（Feature）或 bugfix.md（Bugfix）
├── design.md             # 技术设计文档
└── tasks.md              # 实现任务清单
```

`.config.kiro` 格式：

```json
{"specId": "<uuid>", "workflowType": "requirements-first|design-first", "specType": "feature|bugfix"}
```

---

## requirements.md 格式

### 必需章节

1. **标题与简介** — `# 需求文档：<功能名>`，一段话描述功能范围
2. **依赖/边界说明** — 用 blockquote 列出关键约束（可选）
3. **术语表** — 定义文档中使用的专有名词（可选）
4. **编号需求条目** — 按功能模块分组

### 需求条目格式（EARS 语法）

每个需求使用 EARS（Easy Approach to Requirements Syntax）语法：

```markdown
## 1. 功能模块名

### 1.1 需求标题

WHEN [触发条件/事件] THE SYSTEM SHALL [预期行为]

**验收标准：**
- [ ] AC-1：具体可验证的验收条件
- [ ] AC-2：具体可验证的验收条件
```

#### 需求标题变体

以下两种格式都受此规范承认、一个项目内需保持统一：

| 样式 | 示例 | 适用场景 |
|------|------|---------|
| 标准二级编号 | `## 1. 功能模块` + `### 1.1 需求标题` | 默认格式，适合按功能模块分组的复杂 spec |
| 平铺编号 | `### 需求 1：标题` | 符合项目习惯的扁平编号，适合所有需求同一层级的 spec |

> 多个 spec 共存时，需保证同一 `.kiro/specs/<spec-name>/` 目录内 requirements/design/tasks 三文件用同一种变体；跨 spec 允许使用不同变体。

### EARS 语法模式

| 模式 | 格式 | 适用场景 |
|---|---|---|
| 事件驱动 | WHEN [事件] THE SYSTEM SHALL [行为] | 用户操作触发 |
| 状态驱动 | WHILE [状态] THE SYSTEM SHALL [行为] | 持续状态约束 |
| 无条件 | THE SYSTEM SHALL [行为] | 始终成立的需求 |
| 可选特性 | WHERE [特性启用] THE SYSTEM SHALL [行为] | 配置/特性开关 |
| 异常行为 | IF [异常触发] THEN THE SYSTEM SHALL [系统响应] | 异常/错误处理 |
| 复合 | WHILE [前置条件], WHEN [触发] THE SYSTEM SHALL [行为] | 有前置状态的事件 |

#### EARS 主语变体

`THE SYSTEM SHALL ...` 是标准主语；为了在分层架构 spec 中明确指向承担该行为的具体组件，承认以下主语变体：

| 变体 | 示例 | 适用场景 |
|------|------|---------|
| `THE SYSTEM SHALL ...` | `THE SYSTEM SHALL 记录请求日志` | 默认主语，笼统指代整个系统 |
| `THE <Component_Layer> SHALL ...` | `THE Domain_Layer SHALL 仅包含实体...` | 分层/组件职责类需求，主语明确指名术语表中已定义的层名或组件名 |
| `THE <NamedComponent> SHALL ...` | `THE ValidationBehavior SHALL 抛出 ValidationException` | 具体组件行为约束，主语必须在术语表中明确定义 |

> 使用组件主语时，该主语必须在 requirements.md 的 **术语表** 中先定义；禁止在需求条目中直接引入未定义的主语。

### 需求编号规则

- 一级编号 `1.` `2.` 对应功能模块（标准二级编号格式）
- 二级编号 `1.1` `1.2` 对应具体需求（标准二级编号格式）
- 平铺编号变体中允许使用 `### 需求 1、### 需求 2、...` 的顺序编号；辅助子需求允许用字母后缀（如 `### 需求 13a`）表示补遗需求，避免为插入新需求而级联改动已有追溯引用
- 编号一旦分配不可复用，删除的需求保留编号并标注 `[已删除]`

---

## design.md 格式

### 必需章节

1. **概述** — 一段话描述技术方案
2. **系统架构** — 组件图或分层说明
3. **组件设计** — 每个核心组件的职责、接口、依赖，必须标注 `_覆盖需求: X.X, Y.Y_`
4. **时序图** — 关键流程的 Mermaid sequenceDiagram
5. **数据模型** — 实体/DTO/接口定义
6. **错误处理** — 异常分类与处理策略
7. **测试策略** — 单元测试/集成测试覆盖范围

### 时序图要求

使用 Mermaid 语法，参与者使用文档术语表中定义的名称：

    ```mermaid
    sequenceDiagram
        participant User
        participant Frontend
        participant Backend
        User->>Frontend: 操作描述
        Frontend->>Backend: API 调用
        Backend-->>Frontend: 响应
    ```

---

## bugfix.md 格式

### 必需章节

1. **Bug 描述** — 一句话概述 + 复现步骤
2. **当前行为（Defect）** — 使用 EARS 语法描述缺陷：`WHEN [条件] THEN the system [错误行为]`
3. **预期行为（Correct）** — 使用 EARS 语法描述正确行为：`WHEN [条件] THEN the system SHALL [正确行为]`
4. **不变行为（Regression Prevention）** — 使用 EARS 语法：`WHEN [条件] THEN the system SHALL CONTINUE TO [现有行为]`

> 根因分析和影响范围放在 `design.md` 中（第二阶段），不在 `bugfix.md` 中。

---

## tasks.md 格式

### 必需章节

1. **标题** — `# 实现计划：<功能名>`
2. **概述** — 一段话描述实现策略
3. **任务列表** — 编号 + checkbox 格式

### 任务条目格式

```markdown
- [ ] 1. 任务组名称
  - [ ] 1.1 具体子任务描述
    - 实现细节说明
    - _需求: 1.1, 1.2_ _设计: §组件名_
  - [ ] 1.2 另一个子任务
    - _需求: 2.1_ _设计: §另一组件_
```

### 追溯标记规则

- 每个叶子任务末尾必须有双向追溯标记：`_需求: X.X_` + `_设计: §组件名_`
- 追溯标记引用 requirements.md 中的需求编号
- `§组件名` 必须与 design.md 中的组件标题完全一致
- 一个任务可引用多个需求：`_需求: 1.1, 2.3_`

### 检查点任务

在关键里程碑处插入检查点任务：

```markdown
- [ ] 5. 检查点 — 基础层验证
  - 运行项目的构建命令确认无错误
  - 运行项目的测试命令确认现有测试通过
  - 手动验证核心功能可正常运行
```

### 打标规则

- 未完成：`- [ ]`
- 已完成：`- [x]`
- 父任务在所有子任务完成后再打标
