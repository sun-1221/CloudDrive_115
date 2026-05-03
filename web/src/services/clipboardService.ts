import { useUIStore } from '@/stores/uiStore';
import type { ClipboardItem } from '@/types';

export function copy(fileIds: string[], sourceDirectoryId: string): void {
  const uiStore = useUIStore();
  uiStore.setClipboard({ fileIds, sourceDirectoryId, operation: 'copy' });
}

export function cut(fileIds: string[], sourceDirectoryId: string): void {
  const uiStore = useUIStore();
  uiStore.setClipboard({ fileIds, sourceDirectoryId, operation: 'cut' });
}

export function getClipboard(): ClipboardItem | null {
  const uiStore = useUIStore();
  return uiStore.clipboard;
}

export function hasClipboardData(): boolean {
  return getClipboard() !== null;
}

export function clear(): void {
  const uiStore = useUIStore();
  uiStore.setClipboard(null);
}
