/**
 * 采集器配置 Schema —— 对应设计文档 8.1 CollectorConfig
 *
 * 使用 Zod 做运行时校验，同时导出 TypeScript 类型。
 */
import { z } from 'zod';

export const CollectorConfigSchema = z.object({
  /** 入口 URL */
  baseUrl: z.string().url().default('https://115.com'),
  /** 允许采集的域名白名单 */
  allowedDomains: z.array(z.string()).default(['115.com', 'anxia.com', 'aps.115.com']),
  /** 是否无头运行 */
  headless: z.boolean().default(false),
  /** 登录态保存路径 */
  storageStatePath: z.string().default('state/auth/storage-state.json'),
  /** 输出目录 */
  outputDir: z.string().default('output/runs'),
  /** 执行的场景列表（空数组表示全部） */
  scenarioIds: z.array(z.string()).default([]),
  /** 是否记录响应体 */
  captureResponseBody: z.boolean().default(true),
  /** 单条 body 记录大小上限（字节） */
  maxBodySize: z.number().int().positive().default(512 * 1024),
  /** 页面与动作超时（毫秒） */
  timeoutMs: z.number().int().positive().default(30_000),
  /** 场景之间的等待间隔（毫秒） */
  scenarioIntervalMs: z.number().int().nonnegative().default(2_000),
  /** 动作之间的基础延迟（毫秒） */
  actionDelayMs: z.number().int().nonnegative().default(500),
  /** 页面加载稳定等待时间（毫秒） */
  pageStabilityMs: z.number().int().nonnegative().default(1_000),
  /** 单步最大重试次数 */
  maxRetries: z.number().int().nonnegative().default(2),
  /** 重试退避基数（毫秒） */
  retryBackoffMs: z.number().int().nonnegative().default(1_000),
});

export type CollectorConfig = z.infer<typeof CollectorConfigSchema>;
