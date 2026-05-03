import { defineStore } from 'pinia';
import { ref } from 'vue';
import * as uploadService from '@/services/uploadService';
import type { UploadQueueItem } from '@/types';

export const useUploadStore = defineStore('upload', () => {
  const items = ref<UploadQueueItem[]>([]);
  const dragActive = ref(false);
  const overlayVisible = ref(false);

  async function enqueue(files: File[], directoryId: string): Promise<void> {
    const { useAuthStore } = await import('@/stores/authStore');
    const authStore = useAuthStore();

    if (!authStore.currentAccountUid) return;

    await uploadService.enqueue(files, authStore.currentAccountUid, directoryId);
    syncFromService();
  }

  function updateProgress(localId: string, progress: number): void {
    const item = items.value.find((i) => i.localId === localId);
    if (item) {
      item.progress = progress;
    }
  }

  function markAccepted(localId: string, jobId: string): void {
    const item = items.value.find((i) => i.localId === localId);
    if (item) {
      item.status = 'accepted';
      item.jobId = jobId;
    }
  }

  function markCompleted(localId: string): void {
    const item = items.value.find((i) => i.localId === localId);
    if (item) {
      item.status = 'completed';
      item.progress = 100;
    }
  }

  function markFailed(localId: string, errorMessage: string): void {
    const item = items.value.find((i) => i.localId === localId);
    if (item) {
      item.status = 'failed';
      item.errorMessage = errorMessage;
    }
  }

  function clearFinished(): void {
    uploadService.clearFinished();
    syncFromService();
  }

  function setDragActive(active: boolean): void {
    dragActive.value = active;
    overlayVisible.value = active;
  }

  function syncFromService(): void {
    items.value = [...uploadService.getQueue()];
  }

  return {
    items,
    dragActive,
    overlayVisible,
    enqueue,
    updateProgress,
    markAccepted,
    markCompleted,
    markFailed,
    clearFinished,
    setDragActive,
    syncFromService,
  };
});
