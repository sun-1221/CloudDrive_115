import { test, expect } from '@playwright/test';
import { setupExtendedMock, mockRecycleBinList } from './helpers/mock-api';

test.describe('回收站', () => {
  test.beforeEach(async ({ page }) => {
    await setupExtendedMock(page);
    await page.goto('/cloud-drive/files');
    await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });
  });

  test('需求 6.1: 回收站入口可见', async ({ page }) => {
    // 侧边栏或导航中应有回收站入口
    const recycleBinEntry = page.getByText(/回收站/).first();
    await expect(recycleBinEntry).toBeVisible({ timeout: 5_000 });
  });

  test('需求 6.2: 回收站列表展示', async ({ page }) => {
    // 点击回收站入口
    const recycleBinEntry = page.getByText(/回收站/).first();
    await recycleBinEntry.click();

    // 应展示回收站中的文件
    await expect(page.getByText('已删文件.txt')).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText('已删照片.jpg')).toBeVisible();
  });

  test('需求 6.7: 空回收站展示空状态', async ({ page }) => {
    // 覆盖回收站为空
    await mockRecycleBinList(page, []);

    const recycleBinEntry = page.getByText(/回收站/).first();
    await recycleBinEntry.click();

    // 应展示空状态
    await expect(page.getByText(/回收站为空|暂无/).first()).toBeVisible({ timeout: 10_000 });
  });
});
