<script setup lang="ts">
import { useRouter, useRoute } from 'vue-router';
import { FolderOpen, Trash2, Search, ListTodo } from 'lucide-vue-next';
import { useJobStore } from '@/stores/jobStore';

const router = useRouter();
const route = useRoute();
const jobStore = useJobStore();

const navItems = [
  { label: '文件', icon: FolderOpen, path: '/cloud-drive/files' },
  { label: '搜索', icon: Search, path: '/cloud-drive/search' },
  { label: '回收站', icon: Trash2, path: '/cloud-drive/recycle-bin' },
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
      class="flex flex-col items-center gap-0.5 py-1 px-3 transition-colors duration-200"
      :class="jobStore.panelVisible ? 'text-indigo-400' : 'text-white/50'"
      @click="jobStore.togglePanel()"
    >
      <div class="relative">
        <ListTodo :size="20" />
        <span
          v-if="jobStore.runningJobCount > 0"
          class="absolute -top-1 -right-1 w-3.5 h-3.5 rounded-full bg-indigo-500 text-[8px] text-white flex items-center justify-center"
        >
          {{ jobStore.runningJobCount }}
        </span>
      </div>
      <span class="text-[10px]">任务</span>
    </button>
  </nav>
</template>
