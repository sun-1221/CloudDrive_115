import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

const mockSearchFiles = vi.fn();
vi.mock('@/api/searchApi', () => ({
  searchFiles: (...args: unknown[]) => mockSearchFiles(...args),
}));

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

describe('搜索能力 disabled 契约', () => {
  it('非空关键词只记录不可用状态，不调用搜索 API', async () => {
    const store = useSearchStore();

    await store.search('  测试关键词  ');

    expect(mockSearchFiles).not.toHaveBeenCalled();
    expect(store.keyword).toBe('测试关键词');
    expect(store.breadcrumbLabel).toBe('搜索暂不可用');
    expect(store.emptyStateVisible).toBe(true);
    expect(store.results).toEqual([]);
    expect(store.hasMore).toBe(false);
  });

  it('搜索结果不会由本地过滤伪造，分页也不发请求', async () => {
    const store = useSearchStore();
    await store.search('文件');
    await store.fetchMore();

    expect(mockSearchFiles).not.toHaveBeenCalled();
    expect(store.results).toEqual([]);
    expect(store.totalCount).toBe(0);
  });

  it('空关键词和纯空白不触发搜索，也不改变默认状态', async () => {
    const store = useSearchStore();
    await store.search('');
    await store.search('   ');

    expect(mockSearchFiles).not.toHaveBeenCalled();
    expect(store.keyword).toBe('');
    expect(store.breadcrumbLabel).toBe('搜索结果');
  });
});

describe('搜索状态恢复', () => {
  it('restorePreviousDirectory 恢复搜索前目录并清空状态', async () => {
    const store = useSearchStore();
    await store.search('文件');

    await store.restorePreviousDirectory();

    expect(mockFetchFiles).toHaveBeenCalledWith('dir-0');
    expect(store.keyword).toBe('');
    expect(store.results).toHaveLength(0);
    expect(store.breadcrumbLabel).toBe('搜索结果');
  });

  it('clearSearch 清空所有搜索状态', async () => {
    const store = useSearchStore();
    await store.search('文件');
    store.clearSearch();

    expect(store.keyword).toBe('');
    expect(store.results).toHaveLength(0);
    expect(store.totalCount).toBe(0);
    expect(store.emptyStateVisible).toBe(false);
  });
});
