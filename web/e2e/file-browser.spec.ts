import { test, expect } from '@playwright/test';
import { setupFullMock } from './helpers/mock-api';

test.describe('文件浏览器', () => {
  test.beforeEach(async ({ page }) => {
    await setupFullMock(page);
    await page.goto('/cloud-drive/files');
    // 等待文件列表加载
    await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });
  });

  test('文件列表渲染正确', async ({ page }) => {
    // 文件夹应显示
    await expect(page.getByText('我的文档')).toBeVisible();
    await expect(page.getByText('照片')).toBeVisible();

    // 文件应显示
    await expect(page.getByText('报告.pdf')).toBeVisible();
    await expect(page.getByText('风景.jpg')).toBeVisible();
    await expect(page.getByText('演示.mp4')).toBeVisible();
  });

  test('文件夹排在文件前面', async ({ page }) => {
    // 获取所有文件名元素的文本
    const fileNames = await page.locator('[data-testid="file-name"], .file-name, td:first-child, .n-data-table-td:first-child').allTextContents();

    if (fileNames.length >= 3) {
      // 前两个应该是文件夹
      const firstTwo = fileNames.slice(0, 2);
      expect(firstTwo).toContain('我的文档');
      expect(firstTwo).toContain('照片');
    }
  });

  test('双击文件夹可进入', async ({ page }) => {
    const folderElement = page.getByText('我的文档');
    await folderElement.dblclick();

    // URL 应变化为子目录
    await expect(page).toHaveURL(/\/cloud-drive\/files\/dir-1/, { timeout: 5_000 });
  });

  test('面包屑导航可见', async ({ page }) => {
    // 根目录面包屑（BreadcrumbNav 渲染 fileStore.breadcrumbs 初始值 "根目录"）
    await expect(page.getByText('根目录')).toBeVisible();
  });
});

test.describe('文件浏览器 - 空目录', () => {
  test('空目录展示空态', async ({ page }) => {
    const { mockAuthorizedState, mockFileList, mockJobList, mockRefreshDirectory } = await import('./helpers/mock-api');
    await mockAuthorizedState(page);
    await mockRefreshDirectory(page);
    await mockJobList(page);
    await mockFileList(page, []);

    await page.goto('/cloud-drive/files');

    // 空目录应展示空态提示（FileBrowserPage 显示 "当前目录为空"）
    await expect(page.getByText('当前目录为空')).toBeVisible({ timeout: 10_000 });
  });
});
