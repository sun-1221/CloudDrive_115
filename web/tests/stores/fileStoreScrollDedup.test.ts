import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { useFileStore } from '@/stores/fileStore';

// Mock fileApi
const mockGetFileList = vi.fn().mockResolvedValue({
  items: [],
  totalCount: 100,
  queryCapabilities: { serverSort: false, serverFilter: false },
});

vi.mock('@/api/fileApi', () => ({
  getFileList: (...args: unknown[]) => mockGetFileList(...args),
  refreshDirectory: vi.fn().mockResolvedValue(undefined),
}));

// Mock authStore 动态导入
vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    currentAccountUid: 'test-uid',
  }),
}));

beforeEach(() => {
  setActivePinia(createPinia());
  mockGetFileList.mockClear();
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
});

describe('Property 25: 无限滚动请求去重', () => {
  it('连续多次 fetchMore 调用防抖合并为一次请求', async () => {
    const store = useFileStore();

    store.files = [{ fileId: '1', name: 'a.txt', size: 100, fileType: 'txt', isDirectory: false, modifiedTime: '2024-01-01' } as any];
    store.hasMore = true;
    store.offset = 1;

    // 连续快速调用 3 次（每次会重置防抖计时器）
    store.fetchMore();
    store.fetchMore();
    store.fetchMore();

    // 推进防抖定时器（200ms）
    await vi.advanceTimersByTimeAsync(250);

    // 防抖：仅最后一次生效
    expect(mockGetFileList).toHaveBeenCalledTimes(1);
  });

  it('loadingMore=true 时 fetchMore 直接返回', async () => {
    const store = useFileStore();

    store.hasMore = true;
    store.loadingMore = true;

    await store.fetchMore();

    expect(mockGetFileList).not.toHaveBeenCalled();
  });

  it('hasMore=false 时 fetchMore 直接返回', async () => {
    const store = useFileStore();

    store.hasMore = false;

    await store.fetchMore();

    expect(mockGetFileList).not.toHaveBeenCalled();
  });
});
