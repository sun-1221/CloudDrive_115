import { onMounted, onUnmounted } from 'vue';
import * as keyboardService from '@/services/keyboardService';
import { useFileStore } from '@/stores/fileStore';
import { useUIStore } from '@/stores/uiStore';

export function useFileBrowserKeyboard(callbacks: {
  onDelete: () => void;
  onRename: () => void;
  onOpen: () => void;
  onGoUp: () => void;
  onFocusSearch: () => void;
}) {
  const fileStore = useFileStore();
  const uiStore = useUIStore();

  onMounted(() => {
    // Ctrl+A 全选
    keyboardService.register({
      key: 'a',
      ctrl: true,
      scope: 'fileBrowser',
      handler: (e) => {
        e.preventDefault();
        fileStore.selectAll();
      },
    });

    // Delete 删除
    keyboardService.register({
      key: 'Delete',
      scope: 'fileBrowser',
      handler: () => {
        if (fileStore.selectedFileIds.length > 0) {
          callbacks.onDelete();
        }
      },
    });

    // F2 重命名
    keyboardService.register({
      key: 'F2',
      scope: 'fileBrowser',
      handler: () => {
        if (fileStore.selectedFileIds.length === 1) {
          callbacks.onRename();
        }
      },
    });

    // Ctrl+C 复制
    keyboardService.register({
      key: 'c',
      ctrl: true,
      scope: 'fileBrowser',
      handler: () => {
        if (fileStore.selectedFileIds.length > 0) {
          uiStore.setClipboard({
            fileIds: [...fileStore.selectedFileIds],
            sourceDirectoryId: fileStore.currentDirectoryId,
            operation: 'copy',
          });
        }
      },
    });

    // Ctrl+X 剪切
    keyboardService.register({
      key: 'x',
      ctrl: true,
      scope: 'fileBrowser',
      handler: () => {
        if (fileStore.selectedFileIds.length > 0) {
          uiStore.setClipboard({
            fileIds: [...fileStore.selectedFileIds],
            sourceDirectoryId: fileStore.currentDirectoryId,
            operation: 'cut',
          });
        }
      },
    });

    // Ctrl+V 粘贴
    keyboardService.register({
      key: 'v',
      ctrl: true,
      scope: 'fileBrowser',
      handler: () => {
        if (uiStore.clipboard) {
          // 由 FileBrowserPage 处理粘贴逻辑
        }
      },
    });

    // Enter 打开
    keyboardService.register({
      key: 'Enter',
      scope: 'fileBrowser',
      handler: () => {
        callbacks.onOpen();
      },
    });

    // Backspace 返回上级
    keyboardService.register({
      key: 'Backspace',
      scope: 'fileBrowser',
      handler: () => {
        callbacks.onGoUp();
      },
    });

    // Ctrl+F 聚焦搜索
    keyboardService.register({
      key: 'f',
      ctrl: true,
      scope: 'fileBrowser',
      handler: (e) => {
        e.preventDefault();
        callbacks.onFocusSearch();
      },
    });

    // 方向键导航
    keyboardService.register({
      key: 'ArrowDown',
      scope: 'fileBrowser',
      handler: (e) => {
        e.preventDefault();
        fileStore.moveFocusDown();
      },
    });

    keyboardService.register({
      key: 'ArrowUp',
      scope: 'fileBrowser',
      handler: (e) => {
        e.preventDefault();
        fileStore.moveFocusUp();
      },
    });
  });

  onUnmounted(() => {
    keyboardService.unregister('fileBrowser');
  });
}
