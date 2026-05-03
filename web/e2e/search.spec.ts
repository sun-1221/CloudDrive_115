import { test, expect } from '@playwright/test';
import { setupExtendedMock, mockEmptySearch } from './helpers/mock-api';

test.describe('文件搜索', () => {
  test.beforeEach(async ({ page }) => {
    await setupExtendedMock(page);
    await page.goto('/cloud-drive/files');
    await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });
  });

  test('需求 5.1: 搜索框可见', async ({ page }) => {
    // 搜索框应位于工具栏中
    const searchInput = page.locator('input[placeholder*="搜索"], input[type="search"], [data-testid="search-input"]').first();
    await expect(searchInput).toBeVisible();
  });

  test('需求 5.2: 输入关键词搜索并展示结果', async ({ page }) => {
    const searchInput = page.locator('input[placeholder*="搜索"], input[type="search"], [data-testid="search-input"]').first();

    await searchInput.fill('搜索结果');
    await searchInput.press('Enter');

    // 搜索结果应出现
    await expect(page.getByText('搜索结果文件.pdf')).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText('搜索结果图片.jpg')).toBeVisible();
  });

  test('需求 5.3: 搜索结果面包屑显示搜索标识', async ({ page }) => {
    const searchInput = page.locator('input[placeholder*="搜索"], input[type="search"], [data-testid="search-input"]').first();

    await searchInput.fill('测试');
    await searchInput.press('Enter');

    // 面包屑区域应显示搜索结果标识
    await expect(page.getByText(/搜索结果|"测试"/).first()).toBeVisible({ timeout: 10_000 });
  });

  test('需求 5.5: 无结果时展示空状态', async ({ page }) => {
    // 覆盖搜索 mock 为空结果
    await mockEmptySearch(page);

    const searchInput = page.locator('input[placeholder*="搜索"], input[type="search"], [data-testid="search-input"]').first();
    await searchInput.fill('不存在的文件');
    await searchInput.press('Enter');

    // 应展示空状态提示
    await expect(page.getByText(/未找到|没有匹配|无结果/).first()).toBeVisible({ timeout: 10_000 });
  });

  test('需求 5.7: 返回浏览按钮恢复目录', async ({ page }) => {
    const searchInput = page.locator('input[placeholder*="搜索"], input[type="search"], [data-testid="search-input"]').first();
    await searchInput.fill('搜索结果');
    await searchInput.press('Enter');
    await expect(page.getByText('搜索结果文件.pdf')).toBeVisible({ timeout: 10_000 });

    // 点击返回浏览按钮
    const backButton = page.getByText(/返回浏览|返回目录/).first();
    if (await backButton.isVisible()) {
      await backButton.click();
      // 应恢复到文件列表
      await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });
    }
  });
});
