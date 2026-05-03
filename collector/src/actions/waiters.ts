/**
 * 等待动作 —— 封装各类等待策略
 */
import type { Page, Locator } from 'playwright';

export class Waiters {
  /** 等待列表加载完成（至少出现 N 个列表项） */
  async waitForListItems(locator: Locator, minCount: number = 1): Promise<void> {
    await locator.first().waitFor({ state: 'visible' });
    // TODO: 更精确的列表项数量判断
  }

  /** 等待弹窗出现 */
  async waitForDialog(page: Page, timeout: number = 5000): Promise<boolean> {
    try {
      await page.waitForSelector('[role="dialog"]', { timeout });
      return true;
    } catch {
      return false;
    }
  }

  /** 等待网络请求匹配指定 URL 模式 */
  async waitForRequest(page: Page, urlPattern: string | RegExp): Promise<void> {
    await page.waitForRequest(urlPattern);
  }
}
