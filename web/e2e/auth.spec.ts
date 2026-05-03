import { test, expect } from '@playwright/test';
import { mockAuthorizedState, mockUnauthorizedState, mockFeatureDisabled, mockFileList, mockJobList, mockRefreshDirectory } from './helpers/mock-api';

test.describe('授权流程', () => {
  test('未授权时显示授权引导界面', async ({ page }) => {
    await mockUnauthorizedState(page);
    await mockFileList(page, []);
    await mockJobList(page);

    await page.goto('/cloud-drive/files');

    // 未授权状态应显示授权引导（AuthPanel 显示 "连接 115 网盘"）
    await expect(page.getByText('连接 115 网盘').first()).toBeVisible({ timeout: 10_000 });
  });

  test('已授权时直接进入文件浏览器', async ({ page }) => {
    await mockAuthorizedState(page);
    await mockRefreshDirectory(page);
    await mockJobList(page);
    await mockFileList(page);

    await page.goto('/cloud-drive/files');

    // 应显示文件列表内容
    await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });
  });

  test('功能不可用时显示提示', async ({ page }) => {
    await mockFeatureDisabled(page);
    await mockJobList(page);

    await page.goto('/cloud-drive/files');

    // 应显示功能不可用提示（FeatureUnavailableState 显示 "功能暂不可用"）
    await expect(page.getByText('功能暂不可用')).toBeVisible({ timeout: 10_000 });
  });
});
