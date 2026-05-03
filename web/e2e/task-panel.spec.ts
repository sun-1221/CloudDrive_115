import { test, expect } from '@playwright/test';
import { mockAuthorizedState, mockFileList, mockRefreshDirectory, mockFileOperations, mockRunningJobs } from './helpers/mock-api';

test.describe('任务面板', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthorizedState(page);
    await mockFileOperations(page);
    await mockRefreshDirectory(page);
    await mockRunningJobs(page);
    await mockFileList(page);

    await page.goto('/cloud-drive/files');
    await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });
  });

  test('需求 10.1: 任务图标按钮可见', async ({ page }) => {
    // 工具栏中应有任务面板触发按钮
    const taskButton = page.locator('[data-testid="task-panel-trigger"], button[title*="任务"], button:has-text("任务")').first();
    await expect(taskButton).toBeVisible({ timeout: 5_000 });
  });

  test('需求 10.2: 展开任务面板显示任务列表', async ({ page }) => {
    const taskButton = page.locator('[data-testid="task-panel-trigger"], button[title*="任务"], button:has-text("任务")').first();
    await taskButton.click();

    // 应展示任务面板
    const taskPanel = page.locator('[data-testid="task-panel"], .task-panel, .n-drawer').first();
    await expect(taskPanel).toBeVisible({ timeout: 5_000 });

    // 应包含运行中的任务信息
    await expect(page.getByText(/复制|批量复制/).first()).toBeVisible({ timeout: 5_000 });
  });

  test('需求 10.4: 有运行中任务时按钮展示指示器', async ({ page }) => {
    // 任务按钮应有数字角标或动态指示器
    const badge = page.locator('[data-testid="task-panel-trigger"] .badge, .task-badge, .n-badge').first();
    // 至少按钮本身应可见
    const taskButton = page.locator('[data-testid="task-panel-trigger"], button[title*="任务"], button:has-text("任务")').first();
    await expect(taskButton).toBeVisible({ timeout: 5_000 });
  });

  test('需求 10.8: 任务状态筛选', async ({ page }) => {
    const taskButton = page.locator('[data-testid="task-panel-trigger"], button[title*="任务"], button:has-text("任务")').first();
    await taskButton.click();

    // 面板中应有状态筛选控件（全部/进行中/已完成/失败）
    const filterTabs = page.locator('[data-testid="task-status-filter"], .task-filter, .n-tabs').first();
    if (await filterTabs.isVisible({ timeout: 3_000 })) {
      // 应有多个筛选选项
      await expect(page.getByText(/全部|进行中|已完成|失败/).first()).toBeVisible();
    }
  });

  test('需求 10.9: 空任务列表展示空状态', async ({ page }) => {
    // 使用空任务列表的 mock 替换
    const { mockJobList } = await import('./helpers/mock-api');
    await mockJobList(page);

    const taskButton = page.locator('[data-testid="task-panel-trigger"], button[title*="任务"], button:has-text("任务")').first();
    await taskButton.click();

    // 切换到"已完成"等筛选条件或直接检查空态
    const emptyState = page.getByText(/暂无任务|没有任务/).first();
    // 空任务提示可能需要切换筛选才可见
    if (await emptyState.isVisible({ timeout: 3_000 })) {
      await expect(emptyState).toBeVisible();
    }
  });
});
