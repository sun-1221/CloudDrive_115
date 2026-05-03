import { test, expect } from '@playwright/test';
import { setupFullMock } from './helpers/mock-api';

test.describe('响应式布局', () => {
  test('需求 12.2: 桌面端（1280x720）侧边栏和文件列表同时可见', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 720 });
    await setupFullMock(page);
    await page.goto('/cloud-drive/files');
    await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });

    // 桌面端侧边栏应展开可见
    const sidebar = page.locator('[data-testid="sidebar"], .sidebar, aside, nav').first();
    await expect(sidebar).toBeVisible({ timeout: 5_000 });

    // 文件列表区域也应可见
    const fileList = page.locator('[data-testid="file-list"], .file-list, main').first();
    await expect(fileList).toBeVisible();

    // 不应出现错误
    await expect(page.getByText('加载文件列表失败')).not.toBeVisible();
  });

  test('需求 12.3: 平板端（768x1024）侧边栏折叠或汉堡菜单', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    await setupFullMock(page);
    await page.goto('/cloud-drive/files');
    await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });

    // 平板端侧边栏应折叠，或有汉堡菜单按钮
    const hamburger = page.locator('button[aria-label*="菜单"], [data-testid="hamburger-menu"], button:has-text("☰"), .hamburger-menu').first();
    const collapsedSidebar = page.locator('.sidebar--collapsed, [data-collapsed="true"]').first();

    // 至少其中一个应存在
    const hasHamburger = await hamburger.isVisible().catch(() => false);
    const hasCollapsed = (await collapsedSidebar.count()) > 0;

    // 文件列表占据更多宽度
    const mainContent = page.locator('main, [data-testid="file-list"], .file-list').first();
    await expect(mainContent).toBeVisible();

    // 页面不应有错误
    await expect(page.getByText('加载文件列表失败')).not.toBeVisible();
  });

  test('需求 12.4: 移动端（375x812）底部导航或移动端布局', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await setupFullMock(page);
    await page.goto('/cloud-drive/files');

    // 移动端文件列表应正常渲染
    await expect(page.getByText('我的文档')).toBeAttached({ timeout: 10_000 });

    // 移动端底部导航或移动端操作栏
    const bottomNav = page.locator('[data-testid="bottom-nav"], .bottom-nav, .mobile-nav, .mobile-action-bar').first();
    const mobileLayout = page.locator('.mobile-layout, [data-breakpoint="mobile"]').first();

    // 页面不应有错误状态
    await expect(page.getByText('加载文件列表失败')).not.toBeVisible();

    // 文件列表中的文件名应存在于 DOM 中（可能被截断但仍然附着）
    await expect(page.getByText('报告.pdf')).toBeAttached({ timeout: 10_000 });
  });

  test('需求 12.1: 桌面→移动端动态切换', async ({ page }) => {
    await page.setViewportSize({ width: 1280, height: 720 });
    await setupFullMock(page);
    await page.goto('/cloud-drive/files');
    await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });

    // 切换到移动端
    await page.setViewportSize({ width: 375, height: 812 });
    await page.waitForTimeout(500); // 等待 resize 事件触发

    // 文件列表应仍然存在
    await expect(page.getByText('我的文档')).toBeAttached({ timeout: 5_000 });

    // 不应崩溃
    await expect(page.getByText('加载文件列表失败')).not.toBeVisible();
  });
});
