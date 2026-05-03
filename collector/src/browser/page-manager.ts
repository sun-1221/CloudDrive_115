/**
 * 页面管理 —— 页面创建与统一配置
 */
import type { BrowserContext, Page } from 'playwright';
import type { CollectorConfig } from '../config/schema.js';

export class PageManager {
  constructor(private readonly config: CollectorConfig) {}

  /** 在指定上下文中创建页面并应用统一配置 */
  async createPage(context: BrowserContext): Promise<Page> {
    const page = await context.newPage();
    page.setDefaultTimeout(this.config.timeoutMs);
    page.setDefaultNavigationTimeout(this.config.timeoutMs);
    return page;
  }
}
