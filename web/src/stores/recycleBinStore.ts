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
  const selectedFileIds = ref<string[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);
  const needsRefresh = ref(false);

  async function fetchItems(): Promise<void> {
    loading.value = true;
    error.value = null;
    offset.value = 0;

    try {
      const { useAuthStore } = await import('@/stores/authStore');
      const authStore = useAuthStore();

      if (!authStore.currentAccountUid) {
        error.value = '未选择账号';
        return;
      }

      const result = await recycleBinApi.getRecycleBinList({
        cloudAccountUid: authStore.currentAccountUid,
        offset: 0,
        limit: PAGE_SIZE,
      });

      items.value = result.items;
      totalCount.value = result.totalCount;
      offset.value = result.items.length;
      hasMore.value = result.items.length < result.totalCount;
      selectedFileIds.value = [];
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
      const { useAuthStore } = await import('@/stores/authStore');
      const authStore = useAuthStore();

      if (!authStore.currentAccountUid) return;

      const result = await recycleBinApi.getRecycleBinList({
        cloudAccountUid: authStore.currentAccountUid,
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

  async function restore(fileIds: string[]): Promise<void> {
    const { useAuthStore } = await import('@/stores/authStore');
    const authStore = useAuthStore();

    if (!authStore.currentAccountUid) return;

    const result = await recycleBinApi.restoreFiles({
      cloudAccountUid: authStore.currentAccountUid,
      fileIds,
    });

    if (result.accepted && result.jobId) {
      const { useJobStore } = await import('@/stores/jobStore');
      const jobStore = useJobStore();
      jobStore.addJob(
        result.jobId,
        [{ type: 'recycleBin' }],
        { kind: 'recycleRestore', fileCount: fileIds.length },
      );
    } else {
      // 同步完成 - 从列表中移除
      items.value = items.value.filter((item) => !fileIds.includes(item.fileId));
      selectedFileIds.value = selectedFileIds.value.filter((id) => !fileIds.includes(id));
    }
  }

  async function permanentDelete(fileIds: string[]): Promise<void> {
    const { useAuthStore } = await import('@/stores/authStore');
    const authStore = useAuthStore();

    if (!authStore.currentAccountUid) return;

    const result = await recycleBinApi.permanentDelete({
      cloudAccountUid: authStore.currentAccountUid,
      fileIds,
    });

    if (result.accepted && result.jobId) {
      const { useJobStore } = await import('@/stores/jobStore');
      const jobStore = useJobStore();
      jobStore.addJob(
        result.jobId,
        [{ type: 'recycleBin' }],
        { kind: 'recyclePermanentDelete', fileCount: fileIds.length },
      );
    } else {
      items.value = items.value.filter((item) => !fileIds.includes(item.fileId));
      selectedFileIds.value = selectedFileIds.value.filter((id) => !fileIds.includes(id));
    }
  }

  async function clearAll(): Promise<void> {
    const { useAuthStore } = await import('@/stores/authStore');
    const authStore = useAuthStore();

    if (!authStore.currentAccountUid) return;

    const result = await recycleBinApi.clearRecycleBin(authStore.currentAccountUid);

    if (result.jobId) {
      const { useJobStore } = await import('@/stores/jobStore');
      const jobStore = useJobStore();
      jobStore.addJob(
        result.jobId,
        [{ type: 'recycleBin' }],
        { kind: 'clearRecycleBin' },
      );
    }
  }

  function toggleSelect(fileId: string): void {
    const index = selectedFileIds.value.indexOf(fileId);
    if (index >= 0) {
      selectedFileIds.value = selectedFileIds.value.filter((id) => id !== fileId);
    } else {
      selectedFileIds.value = [...selectedFileIds.value, fileId];
    }
  }

  function selectAll(): void {
    selectedFileIds.value = items.value.map((item) => item.fileId);
  }

  function clearSelection(): void {
    selectedFileIds.value = [];
  }

  function setNeedsRefresh(): void {
    needsRefresh.value = true;
  }

  return {
    items,
    totalCount,
    offset,
    hasMore,
    selectedFileIds,
    loading,
    error,
    needsRefresh,
    fetchItems,
    refresh,
    fetchMore,
    restore,
    permanentDelete,
    clearAll,
    toggleSelect,
    selectAll,
    clearSelection,
    setNeedsRefresh,
  };
});
