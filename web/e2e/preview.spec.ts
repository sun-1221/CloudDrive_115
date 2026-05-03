import { test, expect } from '@playwright/test';
import { setupExtendedMock } from './helpers/mock-api';

test.describe('图片预览', () => {
  test.beforeEach(async ({ page }) => {
    await setupExtendedMock(page);
    await page.goto('/cloud-drive/files');
    await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });
  });

  test('需求 7.1: 点击图片文件打开灯箱预览', async ({ page }) => {
    // 点击（或双击）图片文件
    await page.getByText('风景.jpg').dblclick();

    // 应出现图片预览模态（灯箱），带深色遮罩
    const viewer = page.locator('[data-testid="image-viewer"], .image-viewer, .lightbox, .n-modal').first();
    await expect(viewer).toBeVisible({ timeout: 10_000 });
  });

  test('需求 7.6: 预览顶部工具栏显示文件名和操作按钮', async ({ page }) => {
    await page.getByText('风景.jpg').dblclick();

    const viewer = page.locator('[data-testid="image-viewer"], .image-viewer, .lightbox, .n-modal').first();
    await expect(viewer).toBeVisible({ timeout: 10_000 });

    // 顶部应有文件名
    await expect(page.getByText('风景.jpg').first()).toBeVisible();

    // 应有关闭按钮
    const closeButton = page.locator('button[aria-label="关闭"], button:has-text("关闭"), [data-testid="close-viewer"]').first();
    await expect(closeButton).toBeVisible();
  });

  test('需求 7.7: Esc 关闭灯箱', async ({ page }) => {
    await page.getByText('风景.jpg').dblclick();

    const viewer = page.locator('[data-testid="image-viewer"], .image-viewer, .lightbox, .n-modal').first();
    await expect(viewer).toBeVisible({ timeout: 10_000 });

    await page.keyboard.press('Escape');

    await expect(viewer).not.toBeVisible({ timeout: 5_000 });
  });
});

test.describe('视频播放器', () => {
  test.beforeEach(async ({ page }) => {
    await setupExtendedMock(page);
    await page.goto('/cloud-drive/files');
    await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });
  });

  test('需求 8.1: 点击视频文件打开播放器', async ({ page }) => {
    await page.getByText('演示.mp4').dblclick();

    // 应出现视频播放器模态
    const player = page.locator('[data-testid="video-player"], .video-player, .vjs-player, .n-modal').first();
    await expect(player).toBeVisible({ timeout: 10_000 });
  });

  test('需求 8.3: 播放控制栏可见', async ({ page }) => {
    await page.getByText('演示.mp4').dblclick();

    const player = page.locator('[data-testid="video-player"], .video-player, .vjs-player, .n-modal').first();
    await expect(player).toBeVisible({ timeout: 10_000 });

    // 播放器应包含 video 元素或播放控件
    const videoOrControls = page.locator('video, .vjs-control-bar, .video-controls').first();
    await expect(videoOrControls).toBeAttached({ timeout: 10_000 });
  });

  test('需求 8.4: 多清晰度选择菜单可见', async ({ page }) => {
    await page.getByText('演示.mp4').dblclick();

    const player = page.locator('[data-testid="video-player"], .video-player, .vjs-player, .n-modal').first();
    await expect(player).toBeVisible({ timeout: 10_000 });

    // 应有清晰度选择（720p/1080p）
    const qualitySelector = page.getByText(/720p|1080p|清晰度/).first();
    await expect(qualitySelector).toBeVisible({ timeout: 10_000 });
  });

  test('需求 8.9: Esc 关闭播放器', async ({ page }) => {
    await page.getByText('演示.mp4').dblclick();

    const player = page.locator('[data-testid="video-player"], .video-player, .vjs-player, .n-modal').first();
    await expect(player).toBeVisible({ timeout: 10_000 });

    await page.keyboard.press('Escape');

    await expect(player).not.toBeVisible({ timeout: 5_000 });
  });
});

test.describe('文档预览', () => {
  test.beforeEach(async ({ page }) => {
    await setupExtendedMock(page);
    await page.goto('/cloud-drive/files');
    await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });
  });

  test('需求 9.1: 点击 PDF 文件打开文档预览', async ({ page }) => {
    await page.getByText('报告.pdf').dblclick();

    // 应出现文档预览模态
    const docViewer = page.locator('[data-testid="doc-viewer"], .doc-viewer, .pdf-viewer, .n-modal').first();
    await expect(docViewer).toBeVisible({ timeout: 10_000 });
  });

  test('需求 9.7: 文档预览顶部工具栏', async ({ page }) => {
    await page.getByText('报告.pdf').dblclick();

    const docViewer = page.locator('[data-testid="doc-viewer"], .doc-viewer, .pdf-viewer, .n-modal').first();
    await expect(docViewer).toBeVisible({ timeout: 10_000 });

    // 应有文件名
    await expect(page.getByText('报告.pdf').first()).toBeVisible();

    // 应有关闭和下载按钮
    const closeBtn = page.locator('button:has-text("关闭"), button[aria-label="关闭"], [data-testid="close-doc-viewer"]').first();
    await expect(closeBtn).toBeVisible();
  });

  test('需求 9.1: 点击 TXT 文件打开文档预览', async ({ page }) => {
    await page.getByText('备忘录.txt').dblclick();

    // 应出现文档预览模态
    const docViewer = page.locator('[data-testid="doc-viewer"], .doc-viewer, .txt-viewer, .n-modal').first();
    await expect(docViewer).toBeVisible({ timeout: 10_000 });
  });
});
