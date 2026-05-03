/**
 * 页面导航动作
 */
import type { Page } from 'playwright';
import type { CollectorConfig } from '../config/schema.js';

export class Navigator {
  constructor(private readonly config: CollectorConfig) {}

  /** 导航到指定 URL 并等待页面稳定 */
  async goto(page: Page, url: string): Promise<void> {
    await page.goto(url, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(this.config.pageStabilityMs);
  }

  /** 等待页面网络空闲 */
  async waitForNetworkIdle(page: Page): Promise<void> {
    await page.waitForLoadState('networkidle');
  }
}
