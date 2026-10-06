import { onMounted, onUnmounted } from 'vue';
import * as keyboardService from '@/services/keyboardService';
import { useFileStore } from '@/stores/fileStore';
import { useUIStore } from '@/stores/uiStore';
import { isCapabilityEnabled } from '@/capabilities/registry';

export function useFileBrowserKeyboard(callbacks: {
  onRename: () => void;
  onOpen: () => void;
  onGoUp: () => void;
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
    // 单项复制快捷键仅在能力启用且确实只选中一项时注册。
    if (isCapabilityEnabled('copySingle')) {
      keyboardService.register({
        key: 'c',
        ctrl: true,
        scope: 'fileBrowser',
        handler: () => {
          if (fileStore.selectedFileIds.length === 1) {
            uiStore.setClipboard({
              fileIds: [...fileStore.selectedFileIds],
              sourceDirectoryId: fileStore.currentDirectoryId,
              operation: 'copy',
            });
          }
        },
      });
    }

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
