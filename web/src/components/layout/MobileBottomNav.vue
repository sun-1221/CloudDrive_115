<script setup lang="ts">
import { useRouter, useRoute } from 'vue-router';
import { FolderOpen, Trash2, Sun, Moon, Download, Share2, UserRound } from 'lucide-vue-next';
import { useUIStore } from '@/stores/uiStore';

const router = useRouter();
const route = useRoute();
const uiStore = useUIStore();

const navItems = [
  { label: '文件', icon: FolderOpen, path: '/cloud-drive/files' },
  { label: '回收站', icon: Trash2, path: '/cloud-drive/recycle-bin' },
  { label: '云下载', icon: Download, path: '/cloud-drive/offline' },
  { label: '分享', icon: Share2, path: '/cloud-drive/share' },
  { label: '我的', icon: UserRound, path: '/cloud-drive/user' },
];

function isActive(path: string): boolean {
  return route.path.startsWith(path);
}

function navigateTo(path: string): void {
  router.push(path);
}
</script>

<template>
  <nav class="fixed bottom-0 inset-x-0 z-50 flex items-center justify-around h-14 border-t border-white/10 bg-[#0a0a0f]/95 backdrop-blur-md safe-area-pb">
    <button
      v-for="item in navItems"
      :key="item.path"
      class="flex flex-col items-center gap-0.5 py-1 px-3 transition-colors duration-200"
      :class="isActive(item.path) ? 'text-indigo-400' : 'text-white/50'"
      @click="navigateTo(item.path)"
    >
      <component :is="item.icon" :size="20" />
      <span class="text-[10px]">{{ item.label }}</span>
    </button>

    <button
      class="flex flex-col items-center gap-0.5 py-1 px-3 text-white/50"
      :aria-label="uiStore.theme === 'dark' ? '切换浅色主题' : '切换深色主题'"
      @click="uiStore.toggleTheme()"
    >
      <component :is="uiStore.theme === 'dark' ? Sun : Moon" :size="20" />
      <span class="text-[10px]">主题</span>
    </button>
  </nav>
</template>
