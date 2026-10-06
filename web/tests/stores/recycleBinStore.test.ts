import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

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

import { useRecycleBinStore } from '@/stores/recycleBinStore';

beforeEach(() => {
  setActivePinia(createPinia());
  vi.clearAllMocks();
});

const item = (id: string) => ({
  cid: id,
  dtime: '2024-06-01',
  fileName: `${id}.txt`,
  fileSize: '100',
  id,
  parentName: '/文档',
  status: 'deleted',
  type: 'file',
});

describe('回收站只读行为', () => {
  it('fetchItems 获取列表并保留原始 item 字段', async () => {
    mockGetRecycleBinList.mockResolvedValue({
      items: [item('r1'), item('r2')],
      totalCount: 2,
      itemSchemaStatus: 'available',
    });

    const store = useRecycleBinStore();
    await store.fetchItems();

    expect(mockGetRecycleBinList).toHaveBeenCalledWith({ offset: 0, limit: 50 });
    expect(store.items).toHaveLength(2);
    expect(store.items[0]).toMatchObject({ id: 'r1', fileName: 'r1.txt', parentName: '/文档' });
    expect(store.itemSchemaStatus).toBe('available');
    expect(store.loading).toBe(false);
    expect(store.error).toBeNull();
  });

  it('列表加载失败时设置 error', async () => {
    mockGetRecycleBinList.mockRejectedValue(new Error('网络错误'));

    const store = useRecycleBinStore();
    await store.fetchItems();

    expect(store.error).toBe('网络错误');
    expect(store.loading).toBe(false);
  });

  it('空列表是真实空态，分页只读追加', async () => {
    mockGetRecycleBinList
      .mockResolvedValueOnce({ items: [item('r1')], totalCount: 2, itemSchemaStatus: 'available' })
      .mockResolvedValueOnce({ items: [item('r2')], totalCount: 2, itemSchemaStatus: 'available' });

    const store = useRecycleBinStore();
    await store.fetchItems();
    await store.fetchMore();

    expect(store.items.map((entry) => entry.id)).toEqual(['r1', 'r2']);
    expect(mockGetRecycleBinList).toHaveBeenNthCalledWith(2, { offset: 1, limit: 50 });
  });
});

describe('回收站写操作 disabled 契约', () => {
  it('不暴露还原、彻底删除和清空动作，且不会调用写 API', () => {
    const store = useRecycleBinStore();

    expect(store).not.toHaveProperty('restore');
    expect(store).not.toHaveProperty('permanentDelete');
    expect(store).not.toHaveProperty('clearAll');
    expect(mockRestoreFiles).not.toHaveBeenCalled();
    expect(mockPermanentDelete).not.toHaveBeenCalled();
    expect(mockClearRecycleBin).not.toHaveBeenCalled();
  });

  it('setNeedsRefresh 只标记刷新，不执行任何写操作', () => {
    const store = useRecycleBinStore();
    store.setNeedsRefresh();

    expect(store.needsRefresh).toBe(true);
    expect(mockRestoreFiles).not.toHaveBeenCalled();
    expect(mockPermanentDelete).not.toHaveBeenCalled();
    expect(mockClearRecycleBin).not.toHaveBeenCalled();
  });
});
