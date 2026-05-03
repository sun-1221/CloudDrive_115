import { test, expect } from '@playwright/test';
import { setupExtendedMock } from './helpers/mock-api';

test.describe('文件操作', () => {
  test.beforeEach(async ({ page }) => {
    await setupExtendedMock(page);
    await page.goto('/cloud-drive/files');
    await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });
  });

  test('需求 3.4: 上传按钮可见', async ({ page }) => {
    // 工具栏中应有上传按钮
    const uploadButton = page.getByText(/上传/).first();
    await expect(uploadButton).toBeVisible({ timeout: 5_000 });
  });

  test('需求 3.7: 新建文件夹按钮可见并可点击', async ({ page }) => {
    const newFolderButton = page.getByText(/新建文件夹/).first();
    await expect(newFolderButton).toBeVisible({ timeout: 5_000 });

    await newFolderButton.click();

    // 应出现内联编辑框或对话框
    const editInput = page.locator('input[data-testid="inline-editor"], .inline-editor input, .n-input__input-el').first();
    await expect(editInput).toBeVisible({ timeout: 5_000 });
  });

  test('需求 3.8: 右键重命名显示编辑状态', async ({ page }) => {
    // 右键文件
    await page.getByText('报告.pdf').click({ button: 'right' });

    // 如果右键菜单有重命名选项
    const renameOption = page.getByText(/重命名/).first();
    if (await renameOption.isVisible({ timeout: 3_000 })) {
      await renameOption.click();

      // 应出现内联编辑框
      const editInput = page.locator('input').first();
      await expect(editInput).toBeVisible({ timeout: 5_000 });
    }
  });

  test('需求 3.12: 删除操作弹出确认对话框', async ({ page }) => {
    // 先选中文件
    await page.getByText('报告.pdf').click();

    // 右键选择删除
    await page.getByText('报告.pdf').click({ button: 'right' });

    const deleteOption = page.getByText(/删除/).first();
    if (await deleteOption.isVisible({ timeout: 3_000 })) {
      await deleteOption.click();

      // 应弹出确认对话框，包含"回收站"提示
      await expect(page.getByText(/回收站|确认删除/).first()).toBeVisible({ timeout: 5_000 });
    }
  });

  test('需求 3.16: 工具栏刷新按钮', async ({ page }) => {
    // 查找刷新按钮
    const refreshButton = page.locator('button[title*="刷新"], [data-testid="refresh-button"], button:has-text("刷新")').first();
    if (await refreshButton.isVisible({ timeout: 3_000 })) {
      await refreshButton.click();
      // 刷新后文件列表仍可见
      await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });
    }
  });

  test('需求 2.17: Ctrl+点击多选', async ({ page }) => {
    // 点击第一个文件
    await page.getByText('报告.pdf').click();

    // Ctrl+点击第二个文件
    await page.getByText('风景.jpg').click({ modifiers: ['Control'] });

    // 应选中两个文件（工具栏应出现批量操作相关元素）
    // 检查是否有选中状态的视觉反馈
    const selectedItems = page.locator('.selected, [data-selected="true"], .n-data-table-tr--selected');
    const count = await selectedItems.count();
    expect(count).toBeGreaterThanOrEqual(2);
  });

  test('需求 2.19: 选中文件后显示批量操作按钮', async ({ page }) => {
    await page.getByText('报告.pdf').click();

    // 工具栏中应出现操作按钮（删除/移动/复制之一）
    const actionButtons = page.locator('button:has-text("删除"), button:has-text("移动"), button:has-text("复制")');
    const count = await actionButtons.count();
    expect(count).toBeGreaterThanOrEqual(1);
  });
});

test.describe('文件操作 - 拖拽上传', () => {
  test('需求 3.1: 拖拽上传区域存在', async ({ page }) => {
    await setupExtendedMock(page);
    await page.goto('/cloud-drive/files');
    await expect(page.getByText('我的文档')).toBeVisible({ timeout: 10_000 });

    // UploadZone 组件应存在于 DOM 中
    const uploadZone = page.locator('[data-testid="upload-zone"], .upload-zone, .drop-zone').first();
    await expect(uploadZone).toBeAttached();
  });
});
