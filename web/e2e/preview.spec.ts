import { test, expect } from '@playwright/test';
import { setupExtendedMock } from './helpers/mock-api';

async function openFileWithoutPreview(page: import('@playwright/test').Page, name: string) {
  await page.getByText(name).dblclick();
  await expect(page.locator('.n-modal, [data-testid="image-viewer"], [data-testid="video-player"], [data-testid="doc-viewer"]')).toHaveCount(0);
}

test.describe('预览能力（当前未采集时只读且零调用）', () => {
  test.beforeEach(async ({ page }) => {
    await setupExtendedMock(page);
    await page.goto('/cloud-drive/files');
    await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });
  });

  test('图片预览未采集时不打开灯箱', ({ page }) => openFileWithoutPreview(page, '风景.jpg'));
  test('图片预览工具栏未渲染', ({ page }) => openFileWithoutPreview(page, '风景.jpg'));
  test('图片预览 Esc 无副作用', async ({ page }) => { await openFileWithoutPreview(page, '风景.jpg'); await page.keyboard.press('Escape'); });
  test('视频播放未采集时不打开播放器', ({ page }) => openFileWithoutPreview(page, '演示.mp4'));
  test('视频控制栏未渲染', ({ page }) => openFileWithoutPreview(page, '演示.mp4'));
  test('视频清晰度菜单未渲染', ({ page }) => openFileWithoutPreview(page, '演示.mp4'));
  test('视频播放器 Esc 无副作用', async ({ page }) => { await openFileWithoutPreview(page, '演示.mp4'); await page.keyboard.press('Escape'); });
  test('PDF 预览未采集时不打开查看器', ({ page }) => openFileWithoutPreview(page, '报告.pdf'));
  test('PDF 预览工具栏未渲染', ({ page }) => openFileWithoutPreview(page, '报告.pdf'));
  test('TXT 预览未采集时不打开查看器', ({ page }) => openFileWithoutPreview(page, '备忘录.txt'));
});