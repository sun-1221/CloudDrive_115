/**
 * 点击动作 —— 封装元素点击与重试逻辑
 */
import type { Page, Locator } from 'playwright';

export class Clicker {
  /** 点击指定定位器并等待导航或网络请求 */
  async click(page: Page, locator: Locator): Promise<void> {
    await locator.click();
  }

  /** 安全点击 —— 仅当元素可见时点击，否则跳过 */
  async clickIfVisible(page: Page, locator: Locator): Promise<boolean> {
    if (await locator.isVisible()) {
      await locator.click();
      return true;
    }
    return false;
  }
}
