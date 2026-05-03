/**
 * 上下文管理 —— 场景级 BrowserContext 创建与登录态恢复
 */
import type { Browser, BrowserContext } from 'playwright';
import type { CollectorConfig } from '../config/schema.js';
import type { Logger } from 'pino';
import { getHarRecordOptions } from '../capture/har-writer.js';

export interface ContextOptions {
  runId: string;
  scenarioId: string;
  outputDir: string;
}

export class ContextManager {
  constructor(
    private readonly config: CollectorConfig,
    private readonly logger: Logger,
  ) {}

  /** 为场景创建独立的 BrowserContext，恢复登录态并启用 HAR 录制 */
  async createScenarioContext(
    browser: Browser,
    options: ContextOptions,
  ): Promise<BrowserContext> {
    const harOpts = getHarRecordOptions({
      outputDir: options.outputDir,
      scenarioId: options.scenarioId,
    });

    const context = await browser.newContext({
      storageState: this.config.storageStatePath,
      ...harOpts,
      viewport: { width: 1920, height: 1080 },
    });

    this.logger.debug({ scenarioId: options.scenarioId }, '场景上下文已创建');
    return context;
  }

  /** 关闭场景上下文 */
  async closeContext(context: BrowserContext): Promise<void> {
    await context.close();
  }
}
