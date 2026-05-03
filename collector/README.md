# 115 网页端自动化接口采集器

基于 Playwright 的自动化网页接口采集工具，通过场景驱动的方式系统性记录 115 网页端的所有 API 请求，生成接口台账，支撑前端重写。

## 快速开始

```bash
# 安装依赖（需要 Node.js >= 20）
yarn install

# 安装 Playwright 浏览器
npx playwright install chromium

# 初始化目录结构
npx tsx scripts/setup.ts

# 登录（打开浏览器手动完成登录）
yarn login

# 执行采集
yarn collect

# 分析已采集数据
yarn analyze --run <runId>

# 导出接口台账
yarn export --run <runId>
```

## 项目结构

```
collector/
├── configs/                # 配置文件
├── scripts/                # 工具脚本
├── src/
│   ├── cli/                # CLI 入口与命令
│   │   ├── index.ts        # 主入口
│   │   └── commands/       # login / collect / replay / analyze / export
│   ├── config/             # 配置加载与 Zod schema
│   ├── model/              # 核心数据模型
│   │   ├── capture-record  # 原始采集记录
│   │   ├── endpoint-model  # 归一化后的接口模型
│   │   ├── scenario-model  # 场景定义与上下文
│   │   └── report-model    # 运行报告与台账
│   ├── app/                # 应用层（Runner、生命周期、上下文）
│   ├── browser/            # 浏览器管理（启动、上下文、页面、会话）
│   ├── capture/            # 采集层（网络监听、事件总线、HAR）
│   ├── scenario/           # 场景层（注册表、基类、内置场景）
│   ├── actions/            # 动作层（导航、点击、填写、等待、DOM 探测）
│   ├── parser/             # 解析层（归一化、指纹、Schema 推断、合并）
│   ├── storage/            # 存储层（NDJSON、SQLite、路径解析）
│   ├── exporter/           # 导出层（JSON、CSV、Markdown、OpenAPI）
│   ├── security/           # 安全层（脱敏、域名过滤）
│   ├── logger/             # 日志（pino）
│   └── utils/              # 工具函数（hash、time、retry）
├── state/                  # 运行时状态（gitignored）
│   └── auth/               # 登录态 storageState
└── output/                 # 采集输出（gitignored）
    └── runs/               # 每次运行的独立目录
```

## CLI 命令

| 命令 | 说明 |
|---|---|
| `collector login` | 打开浏览器，手动完成登录并保存会话状态 |
| `collector collect` | 执行场景并采集网络请求 |
| `collector replay -r <runId>` | 基于已有登录态重新执行采集 |
| `collector analyze -r <runId>` | 对原始记录做归一化、聚合和统计 |
| `collector export -r <runId>` | 导出接口台账（JSON/CSV/Markdown/OpenAPI） |

## 环境变量

参见 `.env.example`：

| 变量 | 说明 | 默认值 |
|---|---|---|
| `BASE_URL` | 目标入口 URL | `https://115.com` |
| `HEADLESS` | 是否无头运行 | `false` |
| `STORAGE_STATE_PATH` | 登录态文件路径 | `state/auth/storage-state.json` |
| `OUTPUT_DIR` | 输出目录 | `output/runs` |
| `LOG_LEVEL` | 日志级别 | `info` |

## 注意事项

- **better-sqlite3**：SQLite 索引功能需要原生编译，安装前需确保系统有 C++ 构建工具链（Windows 需安装 Visual Studio Build Tools）。当前暂未包含在依赖中，需要时手动安装：
  ```bash
  yarn add better-sqlite3
  yarn add -D @types/better-sqlite3
  ```
- **npm 版本**：推荐使用 yarn 或 npm >= 8。当前项目使用 yarn 管理依赖。

## 设计文档

详见 [`docs/115-web-collector-architecture.md`](../docs/115-web-collector-architecture.md)
