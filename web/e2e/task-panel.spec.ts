import { test, expect } from '@playwright/test';
import { mockAuthorizedState, mockFileList, mockRefreshDirectory, mockRunningJobs } from './helpers/mock-api';

test.describe('任务面板（当前迭代不在范围）', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthorizedState(page);
    await mockRefreshDirectory(page);
    await mockRunningJobs(page);
    await mockFileList(page);
    await page.goto('/cloud-drive/files');
    await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });
  });

  test('任务触发按钮不渲染', async ({ page }) => {
    await expect(page.locator('[data-testid="task-panel-trigger"], button[title*="任务"], button:has-text("任务")')).toHaveCount(0);
  });
  test('运行中任务不在文件页面展示', async ({ page }) => {
    await expect(page.locator('[data-testid="task-panel"], .task-panel, .n-drawer')).toHaveCount(0);
  });
  test('任务指示器不渲染', async ({ page }) => {
    await expect(page.locator('[data-testid="task-panel-trigger"], [data-testid="task-indicator"]')).toHaveCount(0);
  });
  test('任务状态筛选不渲染', async ({ page }) => {
    await expect(page.locator('[data-testid="task-status-filter"], .task-filter')).toHaveCount(0);
  });
  test('空任务状态不影响文件列表', async ({ page }) => {
    await expect(page.getByText('我的文档')).toBeVisible();
  });
});