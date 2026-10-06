<script setup lang="ts">
import { computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { NButton } from 'naive-ui';
import { FolderOpen, Trash2, ChevronLeft, ChevronRight, Sun, Moon, Download, Share2, UserRound } from 'lucide-vue-next';
import { useUIStore } from '@/stores/uiStore';

const router = useRouter();
const route = useRoute();
const uiStore = useUIStore();

const navItems = [
  { label: '文件浏览', icon: FolderOpen, path: '/cloud-drive/files' },
  { label: '回收站', icon: Trash2, path: '/cloud-drive/recycle-bin' },
  { label: '云下载', icon: Download, path: '/cloud-drive/offline' },
  { label: '我的分享', icon: Share2, path: '/cloud-drive/share' },
  { label: '用户中心', icon: UserRound, path: '/cloud-drive/user' },
];

const isCollapsed = computed(() => !uiStore.sidebarExpanded);

function isActive(path: string): boolean {
  return route.path.startsWith(path);
}

function navigateTo(path: string): void {
  router.push(path);
}
</script>

<template>
  <aside
    class="h-screen flex flex-col border-r border-white/10 bg-[#0a0a0f]/80 backdrop-blur-sm transition-all duration-300"
    :class="isCollapsed ? 'w-16' : 'w-56'"
  >
    <!-- Logo 区域 -->
    <div class="flex items-center justify-between px-4 h-14 border-b border-white/10">
      <span v-if="!isCollapsed" class="text-white/90 font-semibold text-sm truncate">
        115 网盘
      </span>
      <NButton quaternary size="tiny" circle @click="uiStore.toggleSidebar()">
        <template #icon>
          <component :is="isCollapsed ? ChevronRight : ChevronLeft" :size="14" />
        </template>
      </NButton>
    </div>

    <!-- 导航项 -->
    <nav class="flex-1 py-2 space-y-1 px-2">
      <button
        v-for="item in navItems"
        :key="item.path"
        class="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg transition-colors duration-200"
        :class="isActive(item.path)
          ? 'bg-indigo-500/20 text-indigo-300'
          : 'text-white/60 hover:bg-white/5 hover:text-white/80'"
        @click="navigateTo(item.path)"
      >
        <component :is="item.icon" :size="18" class="shrink-0" />
        <span v-if="!isCollapsed" class="text-sm truncate">{{ item.label }}</span>
      </button>
    </nav>
    <div class="border-t border-white/10 p-2">
      <button
        type="button"
        class="w-full flex items-center gap-3 px-3 py-2 rounded-lg text-white/60 hover:bg-white/5 hover:text-white/90"
        :aria-label="uiStore.theme === 'dark' ? '切换浅色主题' : '切换深色主题'"
        @click="uiStore.toggleTheme()"
      >
        <component :is="uiStore.theme === 'dark' ? Sun : Moon" :size="18" />
        <span v-if="!isCollapsed" class="text-sm">{{ uiStore.theme === 'dark' ? '浅色主题' : '深色主题' }}</span>
      </button>
    </div>
  </aside>
</template>
