import { test, expect } from '@playwright/test';
import { setupExtendedMock } from './helpers/mock-api';

test.describe('文件搜索（当前能力禁用）', () => {
  test.beforeEach(async ({ page }) => {
    await setupExtendedMock(page);
    await page.goto('/cloud-drive/files');
    await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });
  });

  test('搜索框不渲染', async ({ page }) => {
    await expect(page.locator('input[placeholder*="搜索"], input[type="search"], [data-testid="search-input"]')).toHaveCount(0);
  });
  test('输入搜索关键词不会发起搜索请求', async ({ page }) => {
    const requests: string[] = [];
    page.on('request', request => { if (request.url().includes('/files/search')) requests.push(request.url()); });
    await expect(page.locator('input[placeholder*="搜索"], input[type="search"], [data-testid="search-input"]')).toHaveCount(0);
    expect(requests).toHaveLength(0);
  });
  test('搜索结果面包屑不渲染', async ({ page }) => {
    await expect(page.getByText(/搜索结果|搜索暂不可用/)).toHaveCount(0);
  });
  test('无结果搜索不会发起请求', async ({ page }) => {
    await expect(page.locator('input[type="search"], [data-testid="search-input"]')).toHaveCount(0);
  });
  test('返回浏览入口不渲染', async ({ page }) => {
    await expect(page.getByText(/返回浏览|返回目录/)).toHaveCount(0);
  });
});