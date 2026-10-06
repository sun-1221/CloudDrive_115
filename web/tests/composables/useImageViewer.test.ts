import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import type { CloudDriveFileDto } from '@/types';

// Mock authStore
vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    currentAccountUid: 'test-uid',
  }),
}));

// Mock previewApi
const mockGetImagePreview = vi.fn();
vi.mock('@/api/previewApi', () => ({
  getImagePreview: (...args: unknown[]) => mockGetImagePreview(...args),
}));


// Mock fileTypeMapping
vi.mock('@/utils/fileTypeMapping', () => ({
  isPreviewableImage: (file: Pick<CloudDriveFileDto, 'isDirectory' | 'name'>) => {
    if (file.isDirectory) return false;
    const ext = file.name.split('.').pop()?.toLowerCase() ?? '';
    return ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'webp'].includes(ext);
  },
}));

import { useImageViewer } from '@/composables/useImageViewer';

beforeEach(() => {
  setActivePinia(createPinia());
  vi.clearAllMocks();
});

const makeImageFile = (id: string, name: string) => ({
  fileId: id, name, size: 1000, fileType: 'jpg', isDirectory: false, modifiedTime: '2024-01-01',
});

const makeFolderFile = (id: string, name: string) => ({
  fileId: id, name, size: 0, fileType: 'folder', isDirectory: true, modifiedTime: '2024-01-01',
});

describe('需求 7.1: 灯箱模式打开', () => {
  it('open 设置 visible=true 并加载图片', async () => {
    mockGetImagePreview.mockResolvedValue({ previewUrl: 'https://example.com/img.jpg', isTemporary: true });

    const { state, open } = useImageViewer();
    const files = [makeImageFile('f1', '风景.jpg'), makeImageFile('f2', '人像.png')];

    open(files as CloudDriveFileDto[], 0);

    // 等待异步加载
    await vi.waitFor(() => expect(state.value.loading).toBe(false));

    expect(state.value.visible).toBe(true);
    expect(state.value.imageUrl).toBe('https://example.com/img.jpg');
    expect(state.value.fileName).toBe('风景.jpg');
  });

  it('open 过滤非图片文件', () => {
    mockGetImagePreview.mockResolvedValue({ previewUrl: 'https://example.com/img.jpg' });

    const { imageCollection, open } = useImageViewer();
    const files = [
      makeFolderFile('d1', '文件夹'),
      makeImageFile('f1', '照片.jpg'),
      { fileId: 'f2', name: '文档.pdf', size: 100, fileType: 'pdf', isDirectory: false, modifiedTime: '' },
    ];

    open(files as CloudDriveFileDto[], 0);

    expect(imageCollection.value).toHaveLength(1);
    expect(imageCollection.value[0]?.name).toBe('照片.jpg');
  });
});

describe('需求 7.2: 左右切换', () => {
  it('next/prev 在图片集合中切换', async () => {
    mockGetImagePreview
      .mockResolvedValueOnce({ previewUrl: 'https://example.com/1.jpg' })
      .mockResolvedValueOnce({ previewUrl: 'https://example.com/2.jpg' })
      .mockResolvedValueOnce({ previewUrl: 'https://example.com/1.jpg' });

    const { state, open, next, prev, canPrev, canNext } = useImageViewer();
    const files = [makeImageFile('f1', 'a.jpg'), makeImageFile('f2', 'b.jpg')];

    open(files as CloudDriveFileDto[], 0);
    await vi.waitFor(() => expect(state.value.loading).toBe(false));

    expect(canPrev.value).toBe(false);
    expect(canNext.value).toBe(true);

    next();
    await vi.waitFor(() => expect(state.value.loading).toBe(false));
    expect(state.value.currentIndex).toBe(1);
    expect(canNext.value).toBe(false);

    prev();
    await vi.waitFor(() => expect(state.value.loading).toBe(false));
    expect(state.value.currentIndex).toBe(0);
  });
});

describe('需求 7.3: 缩放操作', () => {
  it('zoomBy 增减缩放级别', () => {
    const { state, zoomBy } = useImageViewer();

    zoomBy(0.5);
    expect(state.value.zoom).toBe(1.5);

    zoomBy(-1);
    expect(state.value.zoom).toBe(0.5);
  });

  it('zoomBy 不超出范围 [0.1, 10]', () => {
    const { state, zoomBy } = useImageViewer();

    zoomBy(20);
    expect(state.value.zoom).toBe(10);

    zoomBy(-20);
    expect(state.value.zoom).toBe(0.1);
  });

  it('resetZoom 恢复默认', () => {
    const { state, zoomBy, resetZoom } = useImageViewer();

    zoomBy(2);
    resetZoom();

    expect(state.value.zoom).toBe(1);
    expect(state.value.panX).toBe(0);
    expect(state.value.panY).toBe(0);
  });
});

describe('需求 7.4: 拖拽平移', () => {
  it('panBy 增量移动', () => {
    const { state, panBy } = useImageViewer();

    panBy(50, -30);
    expect(state.value.panX).toBe(50);
    expect(state.value.panY).toBe(-30);

    panBy(10, 20);
    expect(state.value.panX).toBe(60);
    expect(state.value.panY).toBe(-10);
  });
});

describe('需求 7.7: 关闭灯箱', () => {
  it('close 重置所有状态', () => {
    mockGetImagePreview.mockResolvedValue({ previewUrl: 'https://example.com/img.jpg' });

    const { state, open, close, imageCollection } = useImageViewer();
    open([makeImageFile('f1', 'a.jpg')] as CloudDriveFileDto[], 0);

    close();

    expect(state.value.visible).toBe(false);
    expect(state.value.imageUrl).toBe('');
    expect(imageCollection.value).toHaveLength(0);
  });
});

describe('需求 7.9-7.10: 加载失败与重试', () => {
  it('加载失败时展示错误信息', async () => {
    mockGetImagePreview.mockRejectedValue(new Error('过期'));

    const { state, open } = useImageViewer();
    open([makeImageFile('f1', 'a.jpg')] as CloudDriveFileDto[], 0);
    await vi.waitFor(() => expect(state.value.loading).toBe(false));

    expect(state.value.error).toBeTruthy();
  });

  it('retry 重新加载当前图片', async () => {
    mockGetImagePreview
      .mockRejectedValueOnce(new Error('过期'))
      .mockRejectedValueOnce(new Error('过期'))
      .mockResolvedValue({ previewUrl: 'https://example.com/new.jpg' });

    const { state, open, retry } = useImageViewer();
    open([makeImageFile('f1', 'a.jpg')] as CloudDriveFileDto[], 0);
    await vi.waitFor(() => expect(state.value.loading).toBe(false));

    retry();
    await vi.waitFor(() => expect(state.value.loading).toBe(false));

    expect(state.value.imageUrl).toBe('https://example.com/new.jpg');
  });
});
