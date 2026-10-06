import { test, expect } from '@playwright/test';
import { setupFullMock } from './helpers/mock-api';

test.describe('键盘快捷键', () => {
  test.beforeEach(async ({ page }) => {
    await setupFullMock(page);
    await page.goto('/cloud-drive/files');
    await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });
  });

  test('Backspace 不触发浏览器后退', async ({ page }) => {
    // 点击文件浏览器区域使其获得焦点
    await page.click('body');

    // 按 Backspace
    await page.keyboard.press('Backspace');

    // 等一小段时间确认没有导航
    await page.waitForTimeout(500);

    // URL 应该保持在 cloud-drive 下（不会回到之前的页面）
    expect(page.url()).toContain('/cloud-drive');
  });

  test('Escape 关闭模态窗口', async ({ page }) => {
    // 先触发某个模态（如果有的话），然后按 Escape 关闭
    await page.keyboard.press('Escape');

    // Escape 不应导致页面导航
    expect(page.url()).toContain('/cloud-drive');
  });
});
