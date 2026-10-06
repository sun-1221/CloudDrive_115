# 115-cloud-drive-frontend Specs 质量审核报告（第 7 轮）

## 审核快照

- **审核时间**：2026-04-18 21:23:54
- **审核轮次**：第 7 轮（第 6 轮后的增量复查）
- **上轮审核**：`quality-history/latest.md`（第 6 轮，2026-04-18 19:45:00）— 已识别 P35-P39，仍有系统性遗留
- **Specs 基线**：
  - `requirements.md`：27560 字节（17 个有效需求模块，R10 已删除）
  - `design.md`：53880 字节（第 6 轮对象清单约 39 个核心设计对象；本轮额外扫描 `####` anchors 共 70 个）
  - `tasks.md`：30481 字节（103 个叶子任务）
- **项目规范依据**：`.windsurf/rules/spec-format.md` + `frontend-development-standards.md` + `frontend-general-standards.md`
- **本轮范围**：增量复查（Q4/Q7/Q9 全量；Q2/Q8 补扫；Q1/Q3/Q5/Q6 沿用第 6 轮结论）

## 审核工作量

- **R** = 17（有效需求模块）
- **D** ≈ 39（沿用第 6 轮核心组件对象清单）
- **T** = 103（叶子任务）
- **Q** = 10（Q1-Q10）
- **I** = 40（见 `references/quality-dimensions.md`）
- **本轮重点**：追溯有效性、AC 级覆盖缺口、跨需求冲突、遗留问题复查

## 增量审核矩阵（本轮范围）

> 未纳入本轮复查范围的维度统一记为 `N/A`；对对象类型天然不适用的维度记为 `-`。

| 编号 | 对象 | 类型 | Q1 | Q2 | Q3 | Q4 | Q5 | Q6 | Q7 | Q8 | Q9 | Q10 状态 | 问题摘要 |
|---|---|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|:--:|---|
| T* | 全部 103 个叶子任务 | 任务 | N/A | N/A | N/A | - | - | - | ❌ | N/A | N/A | [遗留] | `_设计:` 追溯 0/103 |
| T1.1/T2.1/T28.4 | 非编号需求追溯 | 任务 | N/A | N/A | N/A | - | - | - | ❌ | N/A | N/A | [新] | `_需求:` 使用“简介技术栈 / 简介 API 契约权威性”而非 `X.X` |
| D-trace | 设计追溯锚点 | 设计 | N/A | N/A | N/A | ❌ | N/A | N/A | - | - | N/A | [新] | `_覆盖需求:` 存在 11 处非编号 + 18 处通配符 `N.*` |
| T6.4/T8.2/T12.5/T18.1 | 大颗粒任务 | 任务 | N/A | N/A | N/A | - | - | - | - | ❌ | N/A | [遗留] | 仍超 60 分钟参考范围 |
| D§AuthGuard | AuthGuard 设计对象 | 设计 | - | - | - | ❌ | N/A | N/A | - | - | N/A | [遗留] | 有设计，无独立实现任务 |
| D§ConfirmDialog/D§FolderPickerDialog/D§LoginGuideView | 设计缺口 | 设计 | - | - | - | ❌ | N/A | N/A | - | - | N/A | [遗留] | tasks/路由已引用，design 未定义 |
| R3.8 | 202 轮询 | 需求 | N/A | ❌ | N/A | N/A | - | - | - | - | N/A | [遗留] | 轮询频率未量化 |
| R1.6/R18.3 | 401 错误处理 | 需求 | N/A | ❌ | N/A | N/A | - | - | - | - | ❌ | [新] | 一处要求 Toast+跳转，另一处要求仅跳转不 Toast |
| R5.8 | 搜索历史建议 | 需求 | N/A | N/A | N/A | ❌ | - | - | - | - | N/A | [新] | design/tasks 未覆盖 200ms 防抖 + localStorage 20 条 |
| R4.3/R4.4 | 上下文菜单交互 | 需求 | N/A | N/A | N/A | ❌ | - | - | - | - | N/A | [新] | design/tasks 未覆盖边缘翻转与键盘导航 |
| R11.5 | 主题切换控制 | 需求 | N/A | N/A | N/A | ❌ | - | - | - | - | N/A | [新] | `Theme_Controller` 与持久化未在 design/tasks 落地 |
| R14.5 | 网络异常阈值 | 需求 | N/A | ❌ | N/A | ❌ | - | - | - | - | N/A | [新] | `>30s` 超时阈值未落到 design/tasks |
| R3.10 | 连续下载拦截提示 | 需求 | N/A | N/A | N/A | ❌ | - | - | - | - | N/A | [新] | 浏览器拦截连续下载后的提示路径未设计/未分解 |

---

## 第 6 轮遗留项复查

### 🔴 P35 tasks.md 所有叶子任务仍缺失 `_设计: §组件名_` 追溯 — [遗留] Q7.2

- **specs 位置**：`tasks.md` 全文（模式扫描 `_设计:` → **0 命中**）
- **权威依据**：`spec-format.md:160-165`
- **差异说明**：规范要求每个叶子任务末尾必须带 `_需求: X.X_` + `_设计: §组件名_`；当前 103 个叶子任务全部缺 `_设计:`
- **影响**：任务无法从实现清单直达 design 组件，后续 `code-audit-matrix` 无法稳定做 D→T→Code 追溯

### 🟠 P36 部分任务颗粒度仍偏大 — [遗留] Q8.1

- **specs 位置**：`tasks.md:160-174`, `217-223`, `360-367`, `468-477`
- **权威依据**：`quality-dimensions.md:94-97`, `152`
- **差异说明**：规范要求任务参考 15-60 分钟可完成；当前 `T6.4 ErrorHandlerService`、`T8.2 FileStore`、`T12.5 FileBrowserView`、`T18.1 VideoPlayer` 仍为复合型大任务，未补“不可拆例外说明”

### 🟡 P37 AuthGuard 仍无独立任务 — [遗留] Q4.2

- **specs 位置**：`design.md:207-210`, `tasks.md:615`
- **权威依据**：`spec-format.md:106`, `151-165`
- **差异说明**：design 已定义 `AuthGuard 组件（布局层）`；tasks 仅在 `T26.1` 内联提及“处理 401”，无独立实现任务

### 🟡 P38 design.md 仍未定义 ConfirmDialog / FolderPickerDialog / LoginGuideView — [遗留] Q4.2

- **specs 位置**：
  - `tasks.md:299-303`（`ConfirmDialog`）
  - `tasks.md:389-392`（`FolderPickerDialog`）
  - `tasks.md:30`（`LoginGuideView` 路由）
  - `design.md` 全文无对应组件定义
- **权威依据**：`spec-format.md:104-107`
- **差异说明**：tasks 已要求实现或引用，但 design 未提供职责/接口/依赖说明

### 🟢 P39 R3.8 轮询间隔仍未量化 — [遗留] Q2.3

- **specs 位置**：`requirements.md:119`, `design.md:735`, `tasks.md:137`
- **权威依据**：`quality-dimensions.md:27`
- **差异说明**：需求只写“调用 `/api/jobs/{jobId}` 轮询”；design/tasks 已默认 2 秒，但 requirements 未给出量化阈值，导致上游不可验证

---

## 第 7 轮新增问题（7 项）

### 🔴 P40 design/tasks 存在非编号或通配符需求追溯，违反 `X.X` 规则 — [新] Q4.2 + Q7.1 + Q7.3

- **specs 位置**：
  - `tasks.md:21`, `44`, `666`
  - `design.md:62-68`, `150`, `156-161`, `176-180`, `1140`, `1223-1238`
- **权威依据**：
  - `spec-format.md:106` — design 组件必须标注 `_覆盖需求: X.X, Y.Y_`
  - `spec-format.md:155-165` — tasks 叶子任务必须引用 requirements 中真实存在的 `X.X`
- **差异说明**：
  - tasks 当前使用 `_需求: 简介技术栈_`、`_需求: 简介 API 契约权威性_`
  - design 当前使用 `_覆盖需求: 简介鉴权说明_`、`_覆盖需求: 架构调整_`、`_覆盖需求: 所有数据读写类需求_`，以及 `_覆盖需求: 1.* / 2.* / 6.* / 15.* / 18.*`
  - 这些都不是 requirements.md 中可稳定解析的具体需求编号
- **量化证据**：
  - `tasks.md` `_设计:` 命中：`0 / 103`
  - `tasks.md` 非编号 `_需求:`：`3` 处
  - `design.md` 非编号 `_覆盖需求:`：`11` 处
  - `design.md` 通配符 `_覆盖需求: N.*`：`18` 处
- **影响**：第 6 轮“追溯完整”结论被高估；当前 trace 语义对人类可读，但对审核自动化与跨轮比较不可验证
- **建议**：将“简介技术栈 / 鉴权说明 / API 契约权威性 / 架构调整”提升为显式编号需求，或把 design/tasks 的引用改写为现有精确编号

### 🔴 P41 requirements 对 HTTP 401 行为定义自相矛盾 — [新] Q2.1 + Q9.3

- **specs 位置**：`requirements.md:81`, `requirements.md:339`, 参照 `design.md:474-475`, `tasks.md:163-164`
- **权威依据**：`quality-dimensions.md:25-28`, `105-107`
- **差异说明**：
  - `R1.6`：要求“Toast 提示‘登录已过期’并跳转引导页”
  - `R18.3`：要求“跳转引导页，不展示 Toast（由 AuthGuard 统一处理）”
  - design/tasks 目前跟随的是“**不展示 Toast**”分支
- **影响**：同一触发条件（HTTP 401）对应两个互斥行为，实现者与测试者无法确定权威路径
- **建议**：选择单一规则；若以 `AuthGuard` 为准，应删除 `R1.6` 中 Toast 要求，并在 `R18` 声明“401 全局由 AuthGuard 接管”

### 🟠 P42 R5.8 搜索历史建议未进入 design/tasks — [新] Q4.1

- **specs 位置**：`requirements.md:151`, `design.md:289-304`, `tasks.md:414-425`
- **权威依据**：`quality-dimensions.md:46-49`
- **差异说明**：需求要求“关键词长度 ≥2 时，200ms 防抖展示搜索历史与建议，localStorage 最多保留 20 条”；当前 design 仅定义 `SearchStore` + `SearchBar` 的基础搜索/筛选，tasks 也未拆出搜索历史持久化或建议列表任务
- **影响**：R5.8 处于“需求存在，设计与任务缺失”的典型正向覆盖缺口
- **建议**：补 `SearchHistoryStore` 或在 `SearchStore` 中增加 `history/suggestions` 状态，并新增 `SearchBar` 建议面板任务

### 🟠 P43 R4.3/R4.4 上下文菜单边缘翻转与键盘导航未进入 design/tasks — [新] Q4.1

- **specs 位置**：`requirements.md:132-135`, `design.md:270-280`, `tasks.md:395-405`
- **权威依据**：`quality-dimensions.md:46-49`
- **差异说明**：需求要求“距视口右/下边缘 <50px 时自动翻转”与“方向键导航 + Enter 确认”；当前 design/tasks 只覆盖菜单项、关闭逻辑、移动端退化，缺少定位策略与键盘交互模型
- **影响**：R4 的可访问性与边界交互未形成可实现方案
- **建议**：在 `ContextMenu` 设计中补 `placement/keyboardActiveIndex` 状态及翻转算法，并在 `T14.1` 增加相应实现项

### 🟠 P44 R11.5 Theme_Controller 主题切换与持久化未进入 design/tasks — [新] Q4.1

- **specs 位置**：`requirements.md:236`, `design.md:386-397`, `tasks.md:51-74`
- **权威依据**：`quality-dimensions.md:46-49`
- **差异说明**：需求明确要求 `Theme_Controller` 支持暗/亮切换并保存到 localStorage；当前 design 仅定义静态主题系统与 `prefers-reduced-motion`，tasks 也只有样式与 token，没有主题状态控制器/切换入口/持久化任务
- **影响**：R11.5 当前不可实施也不可测试
- **建议**：补 `ThemeController`（或 `UIStore.theme`）设计与任务，明确切换入口、初始化顺序与 localStorage key

### 🟡 P45 R14.5 的 `>30s` 网络超时阈值未落到 design/tasks — [新] Q2.3 + Q4.1

- **specs 位置**：`requirements.md:277`, `design.md:470-482`, `tasks.md:82-86`, `160-174`, `622-626`
- **权威依据**：`quality-dimensions.md:27`, `46-49`
- **差异说明**：需求把“请求 >30s 未响应”纳入网络异常定义；当前 design/tasks 只写“网络/CORS/取消请求处理”，没有明确 `timeout=30000`、超时分类与错误映射策略
- **影响**：R14.5 的量化边界无法被测试验证
- **建议**：在 `http.ts` 明确超时配置，在 `ErrorHandlerService` 中单独区分 timeout 错误类型

### 🟡 P46 R3.10 浏览器阻止连续下载后的提示路径未进入 design/tasks — [新] Q4.1

- **specs 位置**：`requirements.md:121`, `design.md:246-257`, `tasks.md:379-387`
- **权威依据**：`quality-dimensions.md:46-49`
- **差异说明**：需求要求检测连续下载被浏览器拦截时，通过 Toast 提示用户；当前 design/tasks 只覆盖“逐个顺序下载”，未定义拦截检测、失败回退或提示文案触发点
- **影响**：R3.10 无实现入口，批量下载体验与需求不一致
- **建议**：在 `FileOperationService.download()` 中补“连续下载拦截”检测策略，并为 `T13.2` 增加显式提示逻辑

---

## 覆盖率验证

- **本轮范围**：增量复查（Q4/Q7/Q9 全量；Q2/Q8 补扫）
- **量化扫描**：
  - `tasks.md` 叶子任务数：`103`
  - `_设计:` 追溯命中：`0 / 103`
  - `_需求:` 追溯已全量扫描：发现非编号引用 `3` 处
  - `design.md` `_覆盖需求:` 已全量扫描：发现非编号引用 `11` 处、通配符引用 `18` 处
  - AC 级缺口已针对 `R3.10`, `R4.3/R4.4`, `R5.8`, `R11.5`, `R14.5` 做 requirements→design→tasks 反查
- **未覆盖对象**：Q1/Q3/Q5/Q6 未全量重扫 — 原因：本轮无相关 specs 变更，沿用第 6 轮结论

---

## 修复计划

### 🔴 高（先修，否则追溯体系不成立）

1. **P35 + P40**：
   - 批量补齐所有叶子任务 `_设计: §组件名_`
   - 清理 `design.md` / `tasks.md` 中所有非编号或通配符需求追溯
   - 必要时新增显式编号的“非功能/架构约束需求”承载“技术栈 / 鉴权说明 / API 契约权威性”

2. **P41**：
   - 统一 HTTP 401 的单一权威行为
   - 同步修订 `requirements.md`、`design.md`、`tasks.md`

3. **P42 + P43**：
   - 补齐搜索建议/历史、上下文菜单翻转/键盘导航的 design 与 tasks

### 🟠 中高（修后才能稳定进入实现）

4. **P44 + P45 + P46**：
   - 增补主题切换控制器
   - 增补 30 秒超时配置与分类
   - 增补连续下载被拦截后的提示策略

5. **P37 + P38**：
   - 补 `AuthGuard` 独立任务
   - 在 design 中补 `ConfirmDialog`、`FolderPickerDialog`、`LoginGuideView`

### 🟡 中（提升可执行性与可测试性）

6. **P36 + P39**：
   - 拆分大任务或补“不可拆例外说明”
   - 为 `R3.8` 增加明确轮询阈值（建议 `<= 5s` 或直接固化为 `2s`）

---

## 新发现反思

| 新发现 | 上轮为何漏检 |
|---|---|
| P40 非编号/通配符追溯 | 上轮只检查“是否存在 `_需求:` / `_覆盖需求:`”，未校验值是否满足 `X.X` 语法 |
| P41 401 行为冲突 | 上轮把认证模块与通用错误处理分开审，没有做跨需求条目的冲突对照 |
| P42 搜索历史建议缺口 | 上轮覆盖检查停留在“模块级有 SearchStore/SearchBar”，未下钻到 R5.8 AC 级别 |
| P43 上下文菜单翻转/键盘缺口 | 上轮只核对了菜单项覆盖，未逐条比对交互边界 AC |
| P44 主题切换缺口 | 上轮把“主题系统”误等同于“主题控制器”，忽略了切换与持久化要求 |
| P45 30 秒超时缺口 | 上轮只看到“网络/CORS 错误处理”，没有验证量化阈值是否真正下沉到设计/任务 |
| P46 连续下载拦截缺口 | 上轮确认了“逐个下载”，但未继续检查失败回退与提示路径 |

### 规则改进

- 下轮 Q7 必须同时检查“**追溯是否存在**”与“**追溯值是否合法**”
- Q4 必须从“模块级覆盖”下钻到“**AC 级覆盖**”
- 对同一错误码/全局行为（如 `401`）必须做 requirements 全文冲突扫描

---

## 第 7 轮结论

- **第 6 轮遗留的 5 组问题全部仍未修复**
- **本轮新增 7 组问题**，其中 P40/P41 属于会直接破坏追溯与行为确定性的关键缺陷
- **当前不建议直接按 `tasks.md` 进入实现阶段**；至少应先修复：
  1. P35 `_设计:` 追溯缺失
  2. P40 非编号/通配符追溯
  3. P41 401 行为冲突
  4. P42-P46 的 AC 级覆盖缺口

修复上述问题后，建议执行 **第 8 轮专审**：
- **Q4/Q7 追溯重建专审**（验证 exact trace）
- **Q2 冲突与量化专审**（401 / timeout / polling）
