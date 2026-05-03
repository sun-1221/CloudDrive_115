import { describe, it, expect, beforeEach } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { useUIStore } from '@/stores/uiStore';

beforeEach(() => {
  setActivePinia(createPinia());
});

describe('需求 12: 响应式断点计算', () => {
  it('宽度 >= 1024 为 desktop', () => {
    const store = useUIStore();
    Object.defineProperty(window, 'innerWidth', { value: 1280, writable: true });
    store.updateBreakpoint();
    expect(store.breakpoint).toBe('desktop');
    expect(store.isDesktop).toBe(true);
  });

  it('宽度 768-1023 为 tablet', () => {
    const store = useUIStore();
    Object.defineProperty(window, 'innerWidth', { value: 800, writable: true });
    store.updateBreakpoint();
    expect(store.breakpoint).toBe('tablet');
    expect(store.isTablet).toBe(true);
    expect(store.isDesktop).toBe(false);
  });

  it('宽度 < 768 为 mobile', () => {
    const store = useUIStore();
    Object.defineProperty(window, 'innerWidth', { value: 375, writable: true });
    store.updateBreakpoint();
    expect(store.breakpoint).toBe('mobile');
    expect(store.isMobile).toBe(true);
  });
});

describe('需求 14.5: 网络状态', () => {
  it('setOnlineStatus(false) 表示离线', () => {
    const store = useUIStore();
    store.setOnlineStatus(false);
    expect(store.isOnline).toBe(false);
  });

  it('setOnlineStatus(true) 表示在线', () => {
    const store = useUIStore();
    store.setOnlineStatus(false);
    store.setOnlineStatus(true);
    expect(store.isOnline).toBe(true);
  });
});

describe('需求 13.3: 模态窗口管理', () => {
  it('setActiveModal 设置当前模态', () => {
    const store = useUIStore();
    store.setActiveModal('imageViewer');
    expect(store.activeModal).toBe('imageViewer');
  });

  it('setActiveModal(null) 关闭模态', () => {
    const store = useUIStore();
    store.setActiveModal('videoPlayer');
    store.setActiveModal(null);
    expect(store.activeModal).toBeNull();
  });
});

describe('需求 13.6: 键盘挂起', () => {
  it('suspendKeyboard 挂起键盘快捷键', () => {
    const store = useUIStore();
    store.suspendKeyboard();
    expect(store.keyboardSuspended).toBe(true);
  });

  it('resumeKeyboard 恢复键盘快捷键', () => {
    const store = useUIStore();
    store.suspendKeyboard();
    store.resumeKeyboard();
    expect(store.keyboardSuspended).toBe(false);
  });
});

describe('需求 4.4: 应用内剪贴板', () => {
  it('setClipboard 设置剪贴板数据', () => {
    const store = useUIStore();
    store.setClipboard({ fileIds: ['f1'], sourceDirectoryId: 'dir-0', operation: 'copy' });
    expect(store.clipboard).toEqual({ fileIds: ['f1'], sourceDirectoryId: 'dir-0', operation: 'copy' });
  });

  it('setClipboard(null) 清空剪贴板', () => {
    const store = useUIStore();
    store.setClipboard({ fileIds: ['f1'], sourceDirectoryId: 'dir-0', operation: 'cut' });
    store.setClipboard(null);
    expect(store.clipboard).toBeNull();
  });
});

describe('侧边栏与移动端菜单', () => {
  it('toggleSidebar 切换侧边栏', () => {
    const store = useUIStore();
    expect(store.sidebarExpanded).toBe(true);

    store.toggleSidebar();
    expect(store.sidebarExpanded).toBe(false);

    store.toggleSidebar();
    expect(store.sidebarExpanded).toBe(true);
  });

  it('toggleMobileActionMenu 切换移动端操作菜单', () => {
    const store = useUIStore();
    expect(store.mobileActionMenuVisible).toBe(false);

    store.toggleMobileActionMenu();
    expect(store.mobileActionMenuVisible).toBe(true);
  });
});
