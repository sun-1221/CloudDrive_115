import { defineStore } from 'pinia';
import { ref } from 'vue';
import type { CloudDriveSearchResultDto } from '@/types';

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
      // 保存当前目录 ID 用于返回浏览
      const { useFileStore } = await import('@/stores/fileStore');
      const fileStore = useFileStore();
      previousDirectoryId.value = fileStore.currentDirectoryId;

      results.value = [];
      totalCount.value = 0;
      hasMore.value = false;
      breadcrumbLabel.value = '搜索暂不可用';
      emptyStateVisible.value = true;
    } catch (e) {
      if (e instanceof DOMException && e.name === 'AbortError') return;
      emptyStateVisible.value = true;
    } finally {
      loading.value = false;
    }
  }

  async function fetchMore(): Promise<void> {
    if (!hasMore.value || loading.value) return;

    // 搜索能力 disabled：不发起请求，也不把本地过滤冒充为服务端搜索。
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
