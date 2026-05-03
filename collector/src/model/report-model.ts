/**
 * 报表相关模型 —— 对应设计文档 8.6 RunManifest + 11.3 推荐台账字段
 */
import type { ScenarioResult } from './scenario-model.js';

// ---------------------------------------------------------------------------
// RunManifest —— 每次 run 的元数据清单（8.6）
// ---------------------------------------------------------------------------

/** 每次 run 生成的 manifest.json 结构 */
export interface RunManifest {
  /** 本轮 run 的唯一标识 */
  runId: string;
  /** 目标站点名称 */
  targetName: string;
  /** 入口 URL */
  baseUrl: string;
  /** 配置快照（运行时实际生效的配置） */
  configSnapshot: Record<string, unknown>;
  /** 浏览器版本 */
  browserVersion: string;
  /** 运行开始时间 ISO 8601 */
  startedAt: string;
  /** 运行结束时间 ISO 8601 */
  endedAt: string | null;
  /** 各场景执行结果 */
  scenarioResults: ScenarioResult[];
  /** 统计计数器 */
  counters: RunCounters;
  /** 产物路径索引 */
  artifactPaths: RunArtifactPaths;
  /** 退出状态 */
  exitStatus: RunExitStatus;
}

export interface RunCounters {
  /** 总请求数 */
  totalRequests: number;
  /** 命中目标域名的请求数 */
  targetDomainRequests: number;
  /** 去重后的接口数（仅 analyze 后填充） */
  uniqueEndpoints: number | null;
  /** 成功场景数 */
  passedScenarios: number;
  /** 失败场景数 */
  failedScenarios: number;
  /** 跳过场景数 */
  skippedScenarios: number;
}

export interface RunArtifactPaths {
  /** 原始数据目录 */
  raw: string;
  /** 归一化结果目录 */
  normalized: string;
  /** 报表目录 */
  reports: string;
  /** HAR 目录 */
  har: string;
  /** 截图目录 */
  screenshots: string;
  /** trace 目录 */
  traces: string;
}

export type RunExitStatus =
  | 'completed'
  | 'partial'
  | 'sessionExpired'
  | 'aborted'
  | 'error';

// ---------------------------------------------------------------------------
// 接口台账行（11.3）
// ---------------------------------------------------------------------------

/** 导出台账中的单行记录 */
export interface LedgerEntry {
  /** 所属模块 */
  module: string;
  /** 页面 */
  page: string;
  /** 动作 */
  action: string;
  /** 请求方法 */
  method: string;
  /** 接口路径 */
  path: string;
  /** Query 参数摘要 */
  queryParams: string;
  /** Body 结构摘要 */
  bodyStructure: string;
  /** 鉴权方式 */
  authType: string;
  /** 响应结构摘要 */
  responseStructure: string;
  /** 状态码 */
  statusCodes: string;
  /** 样本数 */
  sampleCount: number;
  /** 备注 */
  notes: string;
}
