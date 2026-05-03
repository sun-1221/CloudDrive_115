/**
 * 场景相关模型 —— 对应设计文档 8.2 ScenarioContext + 8.3 ScenarioDefinition
 */
import type { BrowserContext, Page } from 'playwright';
import type { CollectorConfig } from '../config/schema.js';
import type { Logger } from 'pino';
import type { CaptureSession } from '../capture/network-listener.js';

// ---------------------------------------------------------------------------
// ScenarioDefinition —— 场景静态定义（8.3）
// ---------------------------------------------------------------------------

/** 场景静态定义，用于声明场景的元信息、前置条件和运行约束 */
export interface ScenarioDefinition {
  /** 场景唯一标识，如 home / files / search */
  id: string;
  /** 场景用途的简要说明 */
  description: string;
  /** 分类标签，如 core / readonly / destructive */
  tags: string[];
  /** 场景起始页面 URL 或路径 */
  entryUrl: string;
  /** 前置条件描述 */
  preconditions: ScenarioPrecondition;
  /** 运行前的数据探测逻辑标识 */
  requiredDataProbe: string | null;
  /** 是否包含写操作或副作用 */
  destructive: boolean;
  /** 场景级超时（毫秒），覆盖全局默认值 */
  timeout: number | null;
  /** 场景完成判定条件 */
  exitCriteria: ExitCriteria;
}

export interface ScenarioPrecondition {
  /** 所需账号状态，如 logged-in / vip 等 */
  accountState: string[];
  /** 所需数据样本，如"至少存在一个文件" */
  dataSamples: string[];
  /** 依赖的资源类型，如 share / image / video */
  requiredResources: string[];
}

export interface ExitCriteria {
  /** 预期最少采集的请求数 */
  minCapturedRequests: number;
  /** 预期页面最终 URL 匹配模式（可选） */
  finalUrlPattern: string | null;
  /** 自定义校验描述（人工判定时参考） */
  customCheck: string | null;
}

// ---------------------------------------------------------------------------
// ScenarioContext —— 场景运行时上下文（8.2）
// ---------------------------------------------------------------------------

/** 场景运行时上下文，在每个场景执行期间可用 */
export interface ScenarioContext {
  /** 当前 run 的唯一标识 */
  runId: string;
  /** 当前场景的唯一标识 */
  scenarioId: string;
  /** 当前场景的 Playwright Page 实例 */
  page: Page;
  /** 当前场景的 BrowserContext 实例 */
  browserContext: BrowserContext;
  /** 运行时配置 */
  config: CollectorConfig;
  /** 日志实例 */
  logger: Logger;
  /** 采集会话，用于标注当前动作上下文 */
  captureSession: CaptureSession;
}

// ---------------------------------------------------------------------------
// ScenarioResult —— 场景执行结果
// ---------------------------------------------------------------------------

export type ScenarioStatus = 'passed' | 'failed' | 'skipped';

export interface ScenarioResult {
  /** 场景 ID */
  scenarioId: string;
  /** 最终状态 */
  status: ScenarioStatus;
  /** 失败或跳过原因 */
  reason: string | null;
  /** 采集到的请求数 */
  capturedCount: number;
  /** 开始时间 ISO 8601 */
  startedAt: string;
  /** 结束时间 ISO 8601 */
  endedAt: string;
  /** 耗时（毫秒） */
  durationMs: number;
  /** 产物路径 */
  artifactPaths: ScenarioArtifacts;
}

export interface ScenarioArtifacts {
  /** 原始 NDJSON 文件路径 */
  raw: string | null;
  /** HAR 文件路径 */
  har: string | null;
  /** 错误截图路径 */
  screenshot: string | null;
  /** Playwright trace 路径 */
  trace: string | null;
}
