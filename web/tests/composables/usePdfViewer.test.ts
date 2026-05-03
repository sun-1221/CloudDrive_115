import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

// Mock authStore
vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    currentAccountUid: 'test-uid',
  }),
}));

// Mock previewApi
const mockGetDocPreview = vi.fn();
vi.mock('@/api/previewApi', () => ({
  getDocPreview: (...args: unknown[]) => mockGetDocPreview(...args),
}));

// Mock temporaryResourceService
vi.mock('@/services/temporaryResourceService', () => ({
  fetchWithRetry: vi.fn(async ({ fetchUrl, extractUrl }: any) => {
    try {
      const data = await fetchUrl();
      return { success: true, url: extractUrl(data), data };
    } catch (e: any) {
      try {
        const data2 = await fetchUrl();
        return { success: true, url: extractUrl(data2), data: data2 };
      } catch (e2: any) {
        return { success: false, error: e2.message };
      }
    }
  }),
}));

import { usePdfViewer } from '@/composables/usePdfViewer';

beforeEach(() => {
  setActivePinia(createPinia());
  vi.clearAllMocks();
});

describe('需求 9.1: 文档预览打开', () => {
  it('initViewer 加载 PDF 直接渲染', async () => {
    mockGetDocPreview.mockResolvedValue({
      previewUrl: 'https://example.com/doc.pdf',
      fileName: '报告.pdf',
      fileType: 'pdf',
      renderMode: 'Direct',
      isTemporary: true,
    });

    const { state, initViewer } = usePdfViewer();
    await initViewer('file-1', '报告.pdf');

    expect(state.value.visible).toBe(true);
    expect(state.value.renderMode).toBe('Direct');
    expect(state.value.previewUrl).toBe('https://example.com/doc.pdf');
    expect(state.value.loading).toBe(false);
  });
});

describe('需求 9.4: Office 第三方预览', () => {
  it('ThirdParty 模式设置 thirdPartyPreviewUrl', async () => {
    mockGetDocPreview.mockResolvedValue({
      previewUrl: '',
      fileName: '表格.xlsx',
      fileType: 'xlsx',
      renderMode: 'ThirdParty',
      thirdPartyPreviewUrl: 'https://office-viewer.example.com/view?id=123',
      isTemporary: false,
    });

    const { state, initViewer } = usePdfViewer();
    await initViewer('file-2', '表格.xlsx');

    expect(state.value.renderMode).toBe('ThirdParty');
    expect(state.value.thirdPartyPreviewUrl).toBe('https://office-viewer.example.com/view?id=123');
  });
});

describe('需求 9.6: 预览服务未配置', () => {
  it('NotAvailable 模式正确设置', async () => {
    mockGetDocPreview.mockResolvedValue({
      previewUrl: '',
      fileName: '演示.pptx',
      fileType: 'pptx',
      renderMode: 'NotAvailable',
      isTemporary: false,
    });

    const { state, initViewer } = usePdfViewer();
    await initViewer('file-3', '演示.pptx');

    expect(state.value.renderMode).toBe('NotAvailable');
    expect(state.value.previewUrl).toBe('');
  });
});

describe('需求 9.2: PDF 页码导航与缩放', () => {
  it('goToPage 切换页码', async () => {
    mockGetDocPreview.mockResolvedValue({
      previewUrl: 'https://example.com/doc.pdf',
      renderMode: 'Direct',
      isTemporary: true,
    });

    const { state, initViewer, goToPage } = usePdfViewer();
    await initViewer('file-1', '报告.pdf');
    state.value.totalPages = 10;

    goToPage(5);
    expect(state.value.currentPage).toBe(5);

    // 不允许超出范围
    goToPage(0);
    expect(state.value.currentPage).toBe(5);

    goToPage(11);
    expect(state.value.currentPage).toBe(5);
  });

  it('setScale 设置缩放级别', () => {
    const { state, setScale } = usePdfViewer();

    setScale(2.0);
    expect(state.value.scale).toBe(2.0);

    // 限制范围 [0.25, 4]
    setScale(0.1);
    expect(state.value.scale).toBe(0.25);

    setScale(10);
    expect(state.value.scale).toBe(4);
  });
});

describe('需求 9.8: 文档关闭', () => {
  it('destroy 重置状态', async () => {
    mockGetDocPreview.mockResolvedValue({
      previewUrl: 'https://example.com/doc.pdf',
      renderMode: 'Direct',
      isTemporary: true,
    });

    const { state, initViewer, destroy } = usePdfViewer();
    await initViewer('file-1', '报告.pdf');

    destroy();

    expect(state.value.visible).toBe(false);
    expect(state.value.previewUrl).toBe('');
    expect(state.value.renderMode).toBeNull();
  });
});

describe('需求 9.9-9.10: 加载失败与重试', () => {
  it('加载失败设置 error', async () => {
    mockGetDocPreview.mockRejectedValue(new Error('过期'));

    const { state, initViewer } = usePdfViewer();
    await initViewer('file-1', '报告.pdf');

    expect(state.value.error).toBeTruthy();
  });

  it('retry 重新加载', async () => {
    mockGetDocPreview
      .mockRejectedValueOnce(new Error('过期'))
      .mockRejectedValueOnce(new Error('过期'))
      .mockResolvedValue({
        previewUrl: 'https://example.com/new.pdf',
        renderMode: 'Direct',
        isTemporary: true,
      });

    const { state, initViewer, retry } = usePdfViewer();
    await initViewer('file-1', '报告.pdf');
    expect(state.value.error).toBeTruthy();

    retry();

    await vi.waitFor(() => expect(state.value.previewUrl).toBe('https://example.com/new.pdf'));
  });
});
