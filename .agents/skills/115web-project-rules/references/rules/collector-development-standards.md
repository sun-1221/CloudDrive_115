<!-- Generated from .windsurf/rules/collector-development-standards.md by scripts/sync-windsurf-rules.ps1. -->

# 115 Web Collector 开发规范

## 适用范围

本规则适用于 `collector/**` 下的 TypeScript CLI 采集工具，包括：

- Commander 命令入口、命令参数、配置加载与生命周期编排
- Playwright 浏览器自动化、网络监听、场景动作和会话保存
- 请求/响应解析、端点合并、Schema 推断、报告与导出
- 本地存储、日志、安全脱敏、范围过滤和采集产物管理

`collector` 不是 `web/**` Vue 前端应用；不要套用 Vue、Pinia、Tailwind、页面组件或 Cloud Drive Web UI 的状态管理规则。

当以下来源发生冲突时，优先级如下：

1. 当前采集目标的真实网页行为与接口响应
2. 当前 feature 的 `requirements.md`、`design.md`、`tasks.md`
3. 本规则文档
4. 仓库现有 collector 实现模式

---

## 技术栈与运行边界

- 使用 Node.js 20+、TypeScript、ESM 和现有 `package.json` 脚本。
- CLI 参数解析沿用 `commander`，不要引入第二套命令框架。
- 浏览器自动化沿用 `playwright`，浏览器上下文、页面生命周期和会话文件由 `src/browser/**` 统一管理。
- 配置校验沿用 `zod` schema，新增配置项时同步更新默认值、合并逻辑和错误提示。
- 结构化日志沿用 `pino` 封装，CLI 面向用户的短提示可继续使用 `chalk`。

---

## 分层与目录职责

- `src/cli/**`：命令定义、参数解析、用户提示和退出码处理。
- `src/app/**`：采集流程编排、上下文创建、生命周期和分析入口。
- `src/browser/**`：Playwright 浏览器、上下文、页面和 session 管理。
- `src/capture/**`：网络、WebSocket、HAR、请求/响应记录与事件总线。
- `src/actions/**`：页面动作、等待、点击、表单填充和 DOM 探测。
- `src/parser/**`：请求归一化、响应归一化、端点指纹、合并和 schema 推断。
- `src/model/**`：采集记录、端点、报告和场景模型。
- `src/storage/**`：本地路径、NDJSON、SQLite 或其他持久化实现。
- `src/security/**`：敏感信息脱敏、URL/请求范围过滤和安全边界。
- `src/exporter/**`：JSON、CSV、Markdown、OpenAPI 等导出格式。
- `src/scenario/**`：场景定义、注册和内置采集路径。

跨目录调用保持 `CLI -> App -> Browser/Capture/Scenario -> Parser -> Storage/Exporter` 的方向。底层模块不反向依赖 CLI，因为 CLI 选项和用户提示属于边界层。

---

## 采集与安全规则

- 默认只采集明确允许的域名、路径或场景范围；新增采集面时先更新 scope filter，因为采集器容易意外记录无关请求。
- Cookie、Authorization、Set-Cookie、账号标识、手机号、邮箱、文件名等敏感数据必须经过 `security/redact` 统一脱敏后再写日志、报告或导出文件。
- 原始响应体只在用户明确要求或场景需要时保存；保存前必须经过体积限制和脱敏处理。
- Playwright 监听器应在页面或上下文关闭时释放，避免重复注册导致同一请求被记录多次。
- 等待逻辑优先使用明确的页面状态、网络事件或业务断言；固定 sleep 只能作为兜底，并需要说明原因。
- 采集结果写入应具备幂等或可恢复能力，避免重试后生成重复端点。

---

## CLI 与错误处理

- 命令失败时输出面向用户的简短错误，并在 debug 日志中保留可诊断上下文。
- 预期业务失败返回非 0 退出码但不打印未处理异常栈；未预期异常可以保留 stack 方便定位。
- 新增命令时同步更新 `bin` 入口可达性、帮助文本和 README/使用说明中的命令示例。
- 文件路径参数统一通过 path resolver 解析，避免命令层散落相对路径拼接。
- 长耗时命令应显示当前阶段或进度，避免用户误判为卡住。

---

## 数据模型与导出

- 端点指纹、请求归一化和响应 schema 推断应保持稳定；修改算法时需要说明兼容影响。
- 导出格式新增字段时保持向后兼容；无法兼容时在导出器和报告中显式标记版本或迁移说明。
- CSV 和 Markdown 导出只承载摘要信息；完整结构化数据优先放 JSON 或 OpenAPI 输出。
- 不在导出器中重新推断业务语义；导出器只消费 parser/model 已归一化的结果。

---

## 验证

按改动范围选择验证：

- 默认运行 `yarn typecheck` 和 `yarn lint`。
- 触碰 CLI 入口、发布产物、导出格式或 TypeScript 构建配置时运行 `yarn build`。
- 触碰浏览器自动化或场景动作时，优先用最小目标场景做一次本地采集或 replay；若因登录、网络或外部页面不可用无法执行，记录原因和未覆盖风险。
- 触碰脱敏、scope filter、parser、fingerprint 或 exporter 时，补充或更新最小测试样例；如果仓库尚未建立测试命令，至少用现有 fixture/样例数据做手工校验并说明。

---

## 避坑

- `collector` 会处理真实网页请求与凭据上下文；任何日志、报告和导出文件都要先考虑脱敏。
- 浏览器 session 文件可能包含敏感状态；不要把它作为普通 fixture 或示例提交。
- Playwright 事件监听顺序会影响请求/响应配对；修改 listener 时要核对请求、响应、失败和 WebSocket 分支。
- Schema 推断倾向“归纳”，不是官方接口契约；将采集结果写入 spec 前，需要人工确认字段语义和稳定性。
- 不要用 Web UI 的 Store、组件测试或 Tailwind 规则评价 collector 代码；collector 的核心风险是采集准确性、脱敏、可复现和 CLI 可诊断性。
