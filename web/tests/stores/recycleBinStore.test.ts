import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

// Mock recycleBinApi
const mockGetRecycleBinList = vi.fn();
const mockRestoreFiles = vi.fn();
const mockPermanentDelete = vi.fn();
const mockClearRecycleBin = vi.fn();

vi.mock('@/api/recycleBinApi', () => ({
  getRecycleBinList: (...args: unknown[]) => mockGetRecycleBinList(...args),
  restoreFiles: (...args: unknown[]) => mockRestoreFiles(...args),
  permanentDelete: (...args: unknown[]) => mockPermanentDelete(...args),
  clearRecycleBin: (...args: unknown[]) => mockClearRecycleBin(...args),
}));

// Mock authStore
vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    currentAccountUid: 'test-uid',
  }),
}));

// Mock jobStore
const mockAddJob = vi.fn();
vi.mock('@/stores/jobStore', () => ({
  useJobStore: () => ({ addJob: mockAddJob }),
}));

import { useRecycleBinStore } from '@/stores/recycleBinStore';

beforeEach(() => {
  setActivePinia(createPinia());
  vi.clearAllMocks();
});

describe('需求 6.2: 回收站列表展示', () => {
  it('fetchItems 获取回收站文件列表', async () => {
    mockGetRecycleBinList.mockResolvedValue({
      items: [
        { fileId: 'r1', name: '已删文件.txt', size: 100, fileType: 'txt', isDirectory: false, modifiedTime: '2024-01-01', deletedTime: '2024-06-01', originalPath: '/文档' },
        { fileId: 'r2', name: '已删文件夹', size: 0, fileType: 'folder', isDirectory: true, modifiedTime: '2024-01-01', deletedTime: '2024-06-02', originalPath: '/根目录' },
      ],
      totalCount: 2,
    });

    const store = useRecycleBinStore();
    await store.fetchItems();

    expect(store.items).toHaveLength(2);
    expect(store.items[0]?.name).toBe('已删文件.txt');
    expect(store.totalCount).toBe(2);
    expect(store.loading).toBe(false);
    expect(store.error).toBeNull();
  });

  it('fetchItems 失败时设置 error', async () => {
    mockGetRecycleBinList.mockRejectedValue(new Error('网络错误'));

    const store = useRecycleBinStore();
    await store.fetchItems();

    expect(store.error).toBe('网络错误');
    expect(store.loading).toBe(false);
  });
});

describe('需求 6.3: 选择操作', () => {
  it('toggleSelect 切换选择状态', () => {
    const store = useRecycleBinStore();
    store.items = [
      { fileId: 'r1', name: 'a.txt', size: 0, fileType: 'txt', isDirectory: false, modifiedTime: '', deletedTime: '' },
      { fileId: 'r2', name: 'b.txt', size: 0, fileType: 'txt', isDirectory: false, modifiedTime: '', deletedTime: '' },
    ] as any;

    store.toggleSelect('r1');
    expect(store.selectedFileIds).toEqual(['r1']);

    store.toggleSelect('r2');
    expect(store.selectedFileIds).toEqual(['r1', 'r2']);

    store.toggleSelect('r1');
    expect(store.selectedFileIds).toEqual(['r2']);
  });

  it('selectAll 选择所有项', () => {
    const store = useRecycleBinStore();
    store.items = [
      { fileId: 'r1', name: 'a.txt' },
      { fileId: 'r2', name: 'b.txt' },
    ] as any;

    store.selectAll();
    expect(store.selectedFileIds).toEqual(['r1', 'r2']);
  });

  it('clearSelection 清空选择', () => {
    const store = useRecycleBinStore();
    store.selectedFileIds = ['r1', 'r2'];

    store.clearSelection();
    expect(store.selectedFileIds).toEqual([]);
  });
});

describe('需求 6.4: 还原操作', () => {
  it('同步还原：从列表中移除已还原项', async () => {
    mockRestoreFiles.mockResolvedValue({ accepted: false });

    const store = useRecycleBinStore();
    store.items = [
      { fileId: 'r1', name: 'a.txt', size: 0, fileType: 'txt', isDirectory: false, modifiedTime: '', deletedTime: '' },
      { fileId: 'r2', name: 'b.txt', size: 0, fileType: 'txt', isDirectory: false, modifiedTime: '', deletedTime: '' },
    ] as any;
    store.selectedFileIds = ['r1'];

    await store.restore(['r1']);

    expect(store.items).toHaveLength(1);
    expect(store.items[0]?.fileId).toBe('r2');
    expect(mockAddJob).not.toHaveBeenCalled();
  });

  it('异步还原：写入任务面板', async () => {
    mockRestoreFiles.mockResolvedValue({ accepted: true, jobId: 'job-restore-1' });

    const store = useRecycleBinStore();
    store.items = [{ fileId: 'r1', name: 'a.txt' }] as any;

    await store.restore(['r1']);

    expect(mockAddJob).toHaveBeenCalledWith(
      'job-restore-1',
      [{ type: 'recycleBin' }],
      { kind: 'recycleRestore', fileCount: 1 },
    );
  });
});

describe('需求 6.5: 彻底删除操作', () => {
  it('同步彻底删除：从列表中移除', async () => {
    mockPermanentDelete.mockResolvedValue({ accepted: false });

    const store = useRecycleBinStore();
    store.items = [
      { fileId: 'r1', name: 'a.txt' },
      { fileId: 'r2', name: 'b.txt' },
    ] as any;

    await store.permanentDelete(['r1']);

    expect(store.items).toHaveLength(1);
  });

  it('异步彻底删除：写入任务面板', async () => {
    mockPermanentDelete.mockResolvedValue({ accepted: true, jobId: 'job-del-1' });

    const store = useRecycleBinStore();
    await store.permanentDelete(['r1', 'r2']);

    expect(mockAddJob).toHaveBeenCalledWith(
      'job-del-1',
      [{ type: 'recycleBin' }],
      { kind: 'recyclePermanentDelete', fileCount: 2 },
    );
  });
});

describe('需求 6.6/6.8: 清空回收站', () => {
  it('clearAll 写入任务面板', async () => {
    mockClearRecycleBin.mockResolvedValue({ jobId: 'job-clear-1' });

    const store = useRecycleBinStore();
    await store.clearAll();

    expect(mockAddJob).toHaveBeenCalledWith(
      'job-clear-1',
      [{ type: 'recycleBin' }],
      { kind: 'clearRecycleBin' },
    );
  });
});

describe('需求 6.7: 空回收站状态', () => {
  it('空列表时 items 为空数组', async () => {
    mockGetRecycleBinList.mockResolvedValue({ items: [], totalCount: 0 });

    const store = useRecycleBinStore();
    await store.fetchItems();

    expect(store.items).toHaveLength(0);
  });
});

describe('回收站分页', () => {
  it('fetchMore 追加更多项目', async () => {
    mockGetRecycleBinList
      .mockResolvedValueOnce({
        items: [{ fileId: 'r1', name: 'a.txt' }],
        totalCount: 2,
      })
      .mockResolvedValueOnce({
        items: [{ fileId: 'r2', name: 'b.txt' }],
        totalCount: 2,
      });

    const store = useRecycleBinStore();
    await store.fetchItems();
    expect(store.hasMore).toBe(true);

    await store.fetchMore();
    expect(store.items).toHaveLength(2);
  });

  it('hasMore=false 时 fetchMore 不请求', async () => {
    const store = useRecycleBinStore();
    store.hasMore = false;

    await store.fetchMore();
    expect(mockGetRecycleBinList).not.toHaveBeenCalled();
  });
});
