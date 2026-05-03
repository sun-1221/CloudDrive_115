import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

// Mock searchApi
const mockSearchFiles = vi.fn();
vi.mock('@/api/searchApi', () => ({
  searchFiles: (...args: unknown[]) => mockSearchFiles(...args),
}));

// Mock authStore
vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    currentAccountUid: 'test-uid',
  }),
}));

// Mock fileStore
const mockFetchFiles = vi.fn();
vi.mock('@/stores/fileStore', () => ({
  useFileStore: () => ({
    currentDirectoryId: 'dir-0',
    fetchFiles: mockFetchFiles,
  }),
}));

import { useSearchStore } from '@/stores/searchStore';

beforeEach(() => {
  setActivePinia(createPinia());
  vi.clearAllMocks();
});

describe('需求 5.1-5.2: 搜索功能', () => {
  it('search 调用后端搜索接口并展示结果', async () => {
    mockSearchFiles.mockResolvedValue({
      items: [
        { fileId: 'f1', name: '报告.pdf', size: 1024, fileType: 'pdf', isDirectory: false, modifiedTime: '2024-01-01', path: '/文档' },
      ],
      totalCount: 1,
    });

    const store = useSearchStore();
    await store.search('报告');

    expect(mockSearchFiles).toHaveBeenCalledWith({
      cloudAccountUid: 'test-uid',
      keyword: '报告',
      offset: 0,
      limit: 50,
    });
    expect(store.results).toHaveLength(1);
    expect(store.results[0]?.name).toBe('报告.pdf');
    expect(store.keyword).toBe('报告');
  });

  it('search 更新面包屑标签为搜索结果', async () => {
    mockSearchFiles.mockResolvedValue({ items: [], totalCount: 0 });

    const store = useSearchStore();
    await store.search('测试关键词');

    expect(store.breadcrumbLabel).toBe('"测试关键词" 的搜索结果');
  });
});

describe('需求 5.3-5.4: 搜索结果展示', () => {
  it('搜索结果包含 path 字段', async () => {
    mockSearchFiles.mockResolvedValue({
      items: [
        { fileId: 'f1', name: '文件.txt', size: 100, fileType: 'txt', isDirectory: false, modifiedTime: '2024-01-01', path: '/文档/子目录' },
      ],
      totalCount: 1,
    });

    const store = useSearchStore();
    await store.search('文件');

    expect(store.results[0]).toHaveProperty('path');
  });
});

describe('需求 5.5: 搜索无结果', () => {
  it('无匹配时 emptyStateVisible 为 true', async () => {
    mockSearchFiles.mockResolvedValue({ items: [], totalCount: 0 });

    const store = useSearchStore();
    await store.search('不存在的关键词');

    expect(store.emptyStateVisible).toBe(true);
    expect(store.results).toHaveLength(0);
  });
});

describe('需求 5.6: 空关键词阻止搜索', () => {
  it('空字符串不触发搜索', async () => {
    const store = useSearchStore();
    await store.search('');

    expect(mockSearchFiles).not.toHaveBeenCalled();
  });

  it('纯空白不触发搜索', async () => {
    const store = useSearchStore();
    await store.search('   ');

    expect(mockSearchFiles).not.toHaveBeenCalled();
  });
});

describe('需求 5.7: 返回浏览恢复目录', () => {
  it('restorePreviousDirectory 恢复搜索前目录并清空搜索', async () => {
    mockSearchFiles.mockResolvedValue({ items: [{ fileId: 'f1', name: 'a.txt', size: 0, fileType: 'txt', isDirectory: false, modifiedTime: '' }], totalCount: 1 });
    mockFetchFiles.mockResolvedValue(undefined);

    const store = useSearchStore();
    await store.search('a');

    expect(store.previousDirectoryId).toBe('dir-0');

    await store.restorePreviousDirectory();

    expect(mockFetchFiles).toHaveBeenCalledWith('dir-0');
    expect(store.keyword).toBe('');
    expect(store.results).toHaveLength(0);
  });
});

describe('搜索分页加载', () => {
  it('fetchMore 加载更多结果并追加', async () => {
    mockSearchFiles
      .mockResolvedValueOnce({
        items: Array.from({ length: 50 }, (_, i) => ({
          fileId: `f${i}`, name: `file${i}.txt`, size: 100, fileType: 'txt', isDirectory: false, modifiedTime: '2024-01-01',
        })),
        totalCount: 80,
      })
      .mockResolvedValueOnce({
        items: Array.from({ length: 30 }, (_, i) => ({
          fileId: `f${50 + i}`, name: `file${50 + i}.txt`, size: 100, fileType: 'txt', isDirectory: false, modifiedTime: '2024-01-01',
        })),
        totalCount: 80,
      });

    const store = useSearchStore();
    await store.search('file');

    expect(store.hasMore).toBe(true);
    expect(store.results).toHaveLength(50);

    await store.fetchMore();

    expect(store.results).toHaveLength(80);
    expect(store.hasMore).toBe(false);
  });

  it('fetchMore 在 loading 时不重复请求', async () => {
    const store = useSearchStore();
    store.hasMore = true;
    (store as any).loading = true;

    await store.fetchMore();

    expect(mockSearchFiles).not.toHaveBeenCalled();
  });
});

describe('clearSearch 重置状态', () => {
  it('clearSearch 清空所有搜索状态', async () => {
    mockSearchFiles.mockResolvedValue({ items: [{ fileId: 'f1', name: 'a.txt', size: 0, fileType: 'txt', isDirectory: false, modifiedTime: '' }], totalCount: 1 });

    const store = useSearchStore();
    await store.search('test');

    store.clearSearch();

    expect(store.keyword).toBe('');
    expect(store.results).toHaveLength(0);
    expect(store.totalCount).toBe(0);
    expect(store.emptyStateVisible).toBe(false);
  });
});
