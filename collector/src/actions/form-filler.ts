/**
 * 表单填写动作
 */
import type { Page, Locator } from 'playwright';

export class FormFiller {
  /** 清空并填入文本 */
  async fill(locator: Locator, text: string): Promise<void> {
    await locator.fill(text);
  }

  /** 在搜索框输入并触发搜索 */
  async searchAndSubmit(page: Page, locator: Locator, keyword: string): Promise<void> {
    await locator.fill(keyword);
    await page.keyboard.press('Enter');
  }
}
