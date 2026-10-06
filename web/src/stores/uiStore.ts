import { defineStore } from 'pinia';
import { ref, computed } from 'vue';

const BREAKPOINT_TABLET = 768;
const BREAKPOINT_DESKTOP = 1024;

export const useUIStore = defineStore('ui', () => {
  const sidebarExpanded = ref(true);
  const breakpoint = ref<'desktop' | 'tablet' | 'mobile'>('desktop');
  const isOnline = ref(navigator.onLine);
  const clipboard = ref<import('@/types').ClipboardItem | null>(null);
  const fileBrowserFocused = ref(false);
  const activeModal = ref<'imageViewer' | 'videoPlayer' | 'docViewer' | null>(null);
  const mobileActionMenuVisible = ref(false);
  const keyboardSuspended = ref(false);
  const theme = ref<'dark' | 'light'>('dark');

  const isMobile = computed(() => breakpoint.value === 'mobile');
  const isTablet = computed(() => breakpoint.value === 'tablet');
  const isDesktop = computed(() => breakpoint.value === 'desktop');

  function toggleSidebar() {
    sidebarExpanded.value = !sidebarExpanded.value;
  }

  function updateBreakpoint() {
    const width = window.innerWidth;
    if (width >= BREAKPOINT_DESKTOP) {
      breakpoint.value = 'desktop';
    } else if (width >= BREAKPOINT_TABLET) {
      breakpoint.value = 'tablet';
    } else {
      breakpoint.value = 'mobile';
    }
  }

  function setOnlineStatus(online: boolean) {
    isOnline.value = online;
  }

  function setClipboard(item: import('@/types').ClipboardItem | null) {
    clipboard.value = item;
  }

  function setFileBrowserFocused(focused: boolean) {
    fileBrowserFocused.value = focused;
  }

  function setActiveModal(modal: 'imageViewer' | 'videoPlayer' | 'docViewer' | null) {
    activeModal.value = modal;
  }

  function toggleMobileActionMenu() {
    mobileActionMenuVisible.value = !mobileActionMenuVisible.value;
  }

  function suspendKeyboard() {
    keyboardSuspended.value = true;
  }

  function resumeKeyboard() {
    keyboardSuspended.value = false;
  }

  function toggleTheme(): void {
    theme.value = theme.value === 'dark' ? 'light' : 'dark';
    document.documentElement.dataset.theme = theme.value;
  }

  // 初始化断点和网络事件监听
  function initListeners() {
    updateBreakpoint();
    window.addEventListener('resize', updateBreakpoint);
    window.addEventListener('online', () => setOnlineStatus(true));
    window.addEventListener('offline', () => setOnlineStatus(false));
  }

  function destroyListeners() {
    window.removeEventListener('resize', updateBreakpoint);
    window.removeEventListener('online', () => setOnlineStatus(true));
    window.removeEventListener('offline', () => setOnlineStatus(false));
  }

  return {
    sidebarExpanded,
    breakpoint,
    isOnline,
    clipboard,
    fileBrowserFocused,
    activeModal,
    mobileActionMenuVisible,
    keyboardSuspended,
    theme,
    isMobile,
    isTablet,
    isDesktop,
    toggleSidebar,
    updateBreakpoint,
    setOnlineStatus,
    setClipboard,
    setFileBrowserFocused,
    setActiveModal,
    toggleMobileActionMenu,
    suspendKeyboard,
    resumeKeyboard,
    toggleTheme,
    initListeners,
    destroyListeners,
  };
});
