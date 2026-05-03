import * as fileApi from '@/api/fileApi';
import type { UploadQueueItem, UploadProgressEvent } from '@/types';

let uploadQueue: UploadQueueItem[] = [];
let idCounter = 0;
let refreshDebounceTimer: ReturnType<typeof setTimeout> | null = null;

function generateLocalId(): string {
  return `upload-${Date.now()}-${++idCounter}`;
}

export async function enqueue(
  files: File[],
  cloudAccountUid: string,
  directoryId: string,
): Promise<void> {
  for (const file of files) {
    const item: UploadQueueItem = {
      localId: generateLocalId(),
      cloudAccountUid,
      fileName: file.name,
      size: file.size,
      directoryId,
      status: 'queued',
      progress: 0,
    };

    uploadQueue.push(item);
    processUpload(item, file);
  }
}

async function processUpload(item: UploadQueueItem, file: File): Promise<void> {
  const controller = new AbortController();

  try {
    item.status = 'uploading';
    notifyStoreUpdate();

    const result = await fileApi.upload(
      file,
      item.cloudAccountUid,
      item.directoryId,
      false,
      (event: UploadProgressEvent) => {
        item.progress = event.percentage ?? 0;
        notifyStoreUpdate();
      },
      controller.signal,
    );

    if (result.accepted && result.jobId) {
      // 异步受理
      item.status = 'accepted';
      item.jobId = result.jobId;
      notifyStoreUpdate();

      // 写入任务面板
      const { useJobStore } = await import('@/stores/jobStore');
      const jobStore = useJobStore();
      jobStore.addJob(
        result.jobId,
        [{ type: 'directory', directoryId: item.directoryId }],
        { kind: 'upload', fileName: item.fileName },
      );
    } else {
      // 同步完成
      item.status = 'completed';
      item.progress = 100;
      notifyStoreUpdate();
      scheduleDirectoryRefresh(item.directoryId);
    }
  } catch (error) {
    item.status = 'failed';
    item.errorMessage = error instanceof Error ? error.message : '上传失败';
    notifyStoreUpdate();
  }
}

function scheduleDirectoryRefresh(directoryId: string): void {
  if (refreshDebounceTimer !== null) {
    clearTimeout(refreshDebounceTimer);
  }

  refreshDebounceTimer = setTimeout(async () => {
    refreshDebounceTimer = null;
    const { useFileStore } = await import('@/stores/fileStore');
    const fileStore = useFileStore();
    if (fileStore.currentDirectoryId === directoryId) {
      await fileStore.refreshDirectory();
    }
  }, 500);
}

function notifyStoreUpdate(): void {
  // UploadStore 将通过 reactive 引用自动响应变更
  // 此处预留通知钩子
}

export function cancel(localId: string): void {
  const item = uploadQueue.find((i) => i.localId === localId);
  if (item && (item.status === 'queued' || item.status === 'uploading')) {
    item.status = 'cancelled';
    notifyStoreUpdate();
  }
}

export function clearFinished(): void {
  uploadQueue = uploadQueue.filter(
    (item) => item.status !== 'completed' && item.status !== 'failed' && item.status !== 'cancelled',
  );
  notifyStoreUpdate();
}

export function getQueue(): UploadQueueItem[] {
  return uploadQueue;
}
