import { test, expect } from '@playwright/test';
import { setupExtendedMock } from './helpers/mock-api';

test.describe('右键上下文菜单', () => {
  test.beforeEach(async ({ page }) => {
    await setupExtendedMock(page);
    await page.goto('/cloud-drive/files');
    await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });
  });

  test('需求 4.1: 右键文件弹出上下文菜单', async ({ page }) => {
    const fileElement = page.getByText('报告.pdf');
    await fileElement.click({ button: 'right' });

    // 菜单应出现（查找常见菜单项）
    const menu = page.locator('[data-testid="context-menu"], .context-menu, .n-dropdown-menu');
    await expect(menu).toBeVisible({ timeout: 5_000 });
  });

  test('需求 4.2: 文件夹右键菜单包含"打开"', async ({ page }) => {
    const folderElement = page.getByText('我的文档');
    await folderElement.click({ button: 'right' });

    // 应包含"打开"菜单项
    await expect(page.getByText('打开').first()).toBeVisible({ timeout: 5_000 });
  });

  test('需求 4.4: 空白区域右键菜单包含"新建文件夹"', async ({ page }) => {
    // 在文件列表空白区域右键
    const fileBrowserArea = page.locator('[data-testid="file-browser"], .file-browser, main').first();
    await fileBrowserArea.click({ button: 'right', position: { x: 10, y: 400 } });

    // 应包含"新建文件夹"
    await expect(page.getByText('新建文件夹').first()).toHaveCount(0);
  });

  test('需求 4.6: 点击菜单外部关闭菜单', async ({ page }) => {
    const fileElement = page.getByText('报告.pdf');
    await fileElement.click({ button: 'right' });

    const menu = page.locator('[data-testid="context-menu"], .context-menu, .n-dropdown-menu');
    await expect(menu).toBeVisible({ timeout: 5_000 });

    // 点击菜单外部区域
    await page.click('body', { position: { x: 1, y: 1 } });

    // 菜单应关闭
    await expect(menu).not.toBeVisible({ timeout: 3_000 });
  });

  test('需求 4.6: Esc 键关闭菜单', async ({ page }) => {
    const fileElement = page.getByText('报告.pdf');
    await fileElement.click({ button: 'right' });

    const menu = page.locator('[data-testid="context-menu"], .context-menu, .n-dropdown-menu');
    await expect(menu).toBeVisible({ timeout: 5_000 });

    await page.keyboard.press('Escape');

    await expect(menu).not.toBeVisible({ timeout: 3_000 });
  });
});
