/**
 * 默认配置文件 —— 可通过 CLI --config 参数指定
 */
import type { CollectorConfig } from '../src/config/schema.js';

const config: Partial<CollectorConfig> = {
  baseUrl: 'https://115.com',
  headless: false,
  storageStatePath: 'state/auth/storage-state.json',
  outputDir: 'output/runs',
  captureResponseBody: true,
  maxBodySize: 512 * 1024,
  timeoutMs: 30_000,
  scenarioIntervalMs: 2_000,
  actionDelayMs: 500,
  pageStabilityMs: 1_000,
  maxRetries: 2,
  retryBackoffMs: 1_000,
};

export default config;
