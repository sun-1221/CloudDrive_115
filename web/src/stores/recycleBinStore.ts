import { defineStore } from 'pinia';
import { ref } from 'vue';
import * as recycleBinApi from '@/api/recycleBinApi';
import type { CloudDriveRecycleBinItemDto } from '@/types';

const PAGE_SIZE = 50;

export const useRecycleBinStore = defineStore('recycleBin', () => {
  const items = ref<CloudDriveRecycleBinItemDto[]>([]);
  const totalCount = ref(0);
  const offset = ref(0);
  const hasMore = ref(false);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const needsRefresh = ref(false);
  const itemSchemaStatus = ref<'available' | 'not_collected'>('available');

  async function fetchItems(): Promise<void> {
    loading.value = true;
    error.value = null;
    offset.value = 0;

    try {
      const result = await recycleBinApi.getRecycleBinList({
        offset: 0,
        limit: PAGE_SIZE,
      });

      items.value = result.items;
      totalCount.value = result.totalCount;
      offset.value = result.items.length;
      hasMore.value = result.items.length < result.totalCount;
      itemSchemaStatus.value = result.itemSchemaStatus ?? 'available';
      needsRefresh.value = false;
    } catch (e) {
      error.value = e instanceof Error ? e.message : '加载回收站失败';
    } finally {
      loading.value = false;
    }
  }

  async function refresh(): Promise<void> {
    await fetchItems();
  }

  async function fetchMore(): Promise<void> {
    if (!hasMore.value || loading.value) return;

    try {
      const result = await recycleBinApi.getRecycleBinList({
        offset: offset.value,
        limit: PAGE_SIZE,
      });

      items.value = [...items.value, ...result.items];
      offset.value += result.items.length;
      hasMore.value = offset.value < result.totalCount;
    } catch {
      // 加载更多失败静默
    }
  }

  function setNeedsRefresh(): void {
    needsRefresh.value = true;
  }

  return {
    items,
    totalCount,
    offset,
    hasMore,
    loading,
    error,
    needsRefresh,
    itemSchemaStatus,
    fetchItems,
    refresh,
    fetchMore,
    setNeedsRefresh,
  };
});
