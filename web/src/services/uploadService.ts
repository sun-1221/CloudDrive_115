import type { UploadQueueItem } from '@/types';

let uploadQueue: UploadQueueItem[] = [];
let idCounter = 0;

function generateLocalId(): string {
  return `upload-${Date.now()}-${++idCounter}`;
}

export async function enqueue(
  files: File[],
  directoryId: string,
): Promise<void> {
  for (const file of files) {
    const item: UploadQueueItem = {
      localId: generateLocalId(),
      fileName: file.name,
      size: file.size,
      directoryId,
      status: 'queued',
      progress: 0,
    };

    uploadQueue.push(item);
    item.status = 'failed';
    item.errorMessage = '上传功能暂未采集，当前不可用';
    notifyStoreUpdate();
  }
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
