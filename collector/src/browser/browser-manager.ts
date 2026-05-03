/**
 * 浏览器生命周期管理 —— 对应设计文档 7.3
 */
import { chromium, type Browser } from 'playwright';
import type { CollectorConfig } from '../config/schema.js';
import type { Logger } from 'pino';

export class BrowserManager {
  private browser: Browser | null = null;

  constructor(
    private readonly config: CollectorConfig,
    private readonly logger: Logger,
  ) {}

  async launch(): Promise<Browser> {
    this.logger.info({ headless: this.config.headless }, '启动浏览器');

    this.browser = await chromium.launch({
      headless: this.config.headless,
    });

    return this.browser;
  }

  async close(): Promise<void> {
    if (this.browser) {
      await this.browser.close();
      this.browser = null;
      this.logger.info('浏览器已关闭');
    }
  }

  getBrowser(): Browser {
    if (!this.browser) throw new Error('浏览器尚未启动');
    return this.browser;
  }
}
