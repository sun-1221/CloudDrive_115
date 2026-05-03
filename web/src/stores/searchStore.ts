import { defineStore } from 'pinia';
import { ref } from 'vue';
import * as searchApi from '@/api/searchApi';
import type { CloudDriveSearchResultDto } from '@/types';

const PAGE_SIZE = 50;

export const useSearchStore = defineStore('search', () => {
  const keyword = ref('');
  const results = ref<CloudDriveSearchResultDto[]>([]);
  const totalCount = ref(0);
  const offset = ref(0);
  const hasMore = ref(false);
  const loading = ref(false);
  const breadcrumbLabel = ref('搜索结果');
  const emptyStateVisible = ref(false);
  const previousDirectoryId = ref<string | null>(null);

  async function search(newKeyword: string): Promise<void> {
    const trimmed = newKeyword.trim();
    if (!trimmed) return;

    keyword.value = trimmed;
    loading.value = true;
    emptyStateVisible.value = false;
    offset.value = 0;

    try {
      const { useAuthStore } = await import('@/stores/authStore');
      const authStore = useAuthStore();

      if (!authStore.currentAccountUid) return;

      // 保存当前目录 ID 用于返回浏览
      const { useFileStore } = await import('@/stores/fileStore');
      const fileStore = useFileStore();
      previousDirectoryId.value = fileStore.currentDirectoryId;

      const result = await searchApi.searchFiles({
        cloudAccountUid: authStore.currentAccountUid,
        keyword: trimmed,
        offset: 0,
        limit: PAGE_SIZE,
      });

      results.value = result.items;
      totalCount.value = result.totalCount;
      offset.value = result.items.length;
      hasMore.value = result.items.length < result.totalCount;
      breadcrumbLabel.value = `"${trimmed}" 的搜索结果`;
      emptyStateVisible.value = result.items.length === 0;
    } catch (e) {
      if (e instanceof DOMException && e.name === 'AbortError') return;
      emptyStateVisible.value = true;
    } finally {
      loading.value = false;
    }
  }

  async function fetchMore(): Promise<void> {
    if (!hasMore.value || loading.value) return;

    try {
      const { useAuthStore } = await import('@/stores/authStore');
      const authStore = useAuthStore();

      if (!authStore.currentAccountUid) return;

      const result = await searchApi.searchFiles({
        cloudAccountUid: authStore.currentAccountUid,
        keyword: keyword.value,
        offset: offset.value,
        limit: PAGE_SIZE,
      });

      results.value = [...results.value, ...result.items];
      offset.value += result.items.length;
      hasMore.value = offset.value < result.totalCount;
    } catch {
      // 静默失败
    }
  }

  async function restorePreviousDirectory(): Promise<void> {
    if (previousDirectoryId.value) {
      const { useFileStore } = await import('@/stores/fileStore');
      const fileStore = useFileStore();
      await fileStore.fetchFiles(previousDirectoryId.value);
    }
    clearSearch();
  }

  function clearSearch(): void {
    keyword.value = '';
    results.value = [];
    totalCount.value = 0;
    offset.value = 0;
    hasMore.value = false;
    emptyStateVisible.value = false;
    breadcrumbLabel.value = '搜索结果';
  }

  return {
    keyword,
    results,
    totalCount,
    offset,
    hasMore,
    loading,
    breadcrumbLabel,
    emptyStateVisible,
    previousDirectoryId,
    search,
    fetchMore,
    restorePreviousDirectory,
    clearSearch,
  };
});
