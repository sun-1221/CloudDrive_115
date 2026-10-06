# 决策日志：115-cloud-drive-net10（Portfolio 审计修复）

> 依据：`.kiro/audit-evidence/2026-07-22-specs-portfolio-full/audit-report.md`
> 用户确认：2026-07-22「按推荐默认全部改」

| ID | 问题 | 选项 | 当前决策 | 决策时间 | 影响范围 | 状态 |
|----|------|------|----------|----------|----------|------|
| D15 | ProblemDetails extensions | A net10 补外壳 / B 不约束 | **A** R10/D§6 补 extensions.code + 可选 upstream* | 2026-07-22 | requirements/design | 已决 |
| D16 | 架构图 | A 对齐时序图 / B 删架构图 | **A** Behaviors 含 Dispatch；事件经 Dispatch | 2026-07-22 | design | 已决 |
| D17 | ITransactionManager 术语 | A 补术语表 / B 删举例 | **A** 术语表+数据模型补定义 | 2026-07-22 | requirements/design | 已决 |
| D18 | errors 形态 | A 对象字典 / B JSON 数组 | **A** `Record<string,string[]>`；Result.Failure 亦须 extensions.code | 2026-07-25 | requirements/design | 已决 |
| D19 | design 章节名 | A 对齐 spec-format / B 保留旧名 | **A** 系统架构/时序图/组件设计 | 2026-07-25 | design | 已决 |
| D20 | 示例业务码风格 | A snake_case 占位 + 码表归业务模块 / B 点分 Pascal | **A** 示例非权威码；权威见 integration 码表 | 2026-07-25 | design | 已决 |
| D21 | ProblemDetails 线格式 | A 根级扁平 code / B 嵌套 extensions 对象 | **A** ASP.NET 根级兄弟字段；`extensions.*` 为槽位表述 | 2026-07-25 | design | 已决 |
| D22 | PreValidation 时序 | A alt/else 互斥 / B 保留 opt+直达双路径 | **A** alt 有/无 PreValidation 互斥 | 2026-07-25 | design | 已决 |
