import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import type { ClipboardItem } from '@/types';

// 必须在 import clipboardService 之前 mock uiStore
const mockClipboard = { value: null as ClipboardItem | null };
const mockSetClipboard = vi.fn((item: ClipboardItem | null) => { mockClipboard.value = item; });

vi.mock('@/stores/uiStore', () => ({
  useUIStore: () => ({
    clipboard: mockClipboard.value,
    setClipboard: mockSetClipboard,
  }),
}));

import * as clipboardService from '@/services/clipboardService';

beforeEach(() => {
  setActivePinia(createPinia());
  mockClipboard.value = null;
  vi.clearAllMocks();
});

describe('需求 4.4: 应用内剪贴板——复制', () => {
  it('copy 将文件 ID 和源目录写入剪贴板', () => {
    clipboardService.copy(['f1', 'f2'], 'dir-0');

    expect(mockSetClipboard).toHaveBeenCalledWith({
      fileIds: ['f1', 'f2'],
      sourceDirectoryId: 'dir-0',
      operation: 'copy',
    });
  });
});

describe('需求 4.4: 应用内剪贴板——剪切', () => {
  it('cut 将文件 ID 和源目录以 cut 模式写入', () => {
    clipboardService.cut(['f3'], 'dir-1');

    expect(mockSetClipboard).toHaveBeenCalledWith({
      fileIds: ['f3'],
      sourceDirectoryId: 'dir-1',
      operation: 'cut',
    });
  });
});

describe('需求 4.4: 粘贴条件——hasClipboardData', () => {
  it('剪贴板为空时返回 false', () => {
    mockClipboard.value = null;
    expect(clipboardService.hasClipboardData()).toBe(false);
  });

  it('剪贴板有数据时返回 true', () => {
    mockClipboard.value = { fileIds: ['f1'], sourceDirectoryId: 'dir-0', operation: 'copy' };
    expect(clipboardService.hasClipboardData()).toBe(true);
  });
});

describe('clear 清空剪贴板', () => {
  it('clear 调用 setClipboard(null)', () => {
    clipboardService.clear();
    expect(mockSetClipboard).toHaveBeenCalledWith(null);
  });
});
