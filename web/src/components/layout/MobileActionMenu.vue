<script setup lang="ts">
import { NDrawer, NDrawerContent } from 'naive-ui';
import {
  FolderPlus,
  ArrowUpDown,
  Filter,
  RefreshCw,
} from 'lucide-vue-next';
import { useUIStore } from '@/stores/uiStore';
import { useFileStore } from '@/stores/fileStore';

const uiStore = useUIStore();
const fileStore = useFileStore();

const emit = defineEmits<{
  createFolder: [];
}>();

function handleAction(action: string) {
  uiStore.toggleMobileActionMenu();
  switch (action) {
    case 'createFolder':
      emit('createFolder');
      break;
    case 'refresh':
      fileStore.refreshDirectory();
      break;
  }
}

const actions = [
  { label: '新建文件夹', icon: FolderPlus, action: 'createFolder' },
  { label: '排序', icon: ArrowUpDown, action: 'sort' },
  { label: '筛选', icon: Filter, action: 'filter' },
  { label: '刷新', icon: RefreshCw, action: 'refresh' },
];
</script>

<template>
  <NDrawer
    :show="uiStore.mobileActionMenuVisible"
    height="auto"
    placement="bottom"
    @update:show="uiStore.toggleMobileActionMenu()"
  >
    <NDrawerContent title="更多操作" closable>
      <div class="grid grid-cols-3 gap-4 py-2">
        <button
          v-for="item in actions"
          :key="item.action"
          class="flex flex-col items-center gap-2 py-3 rounded-lg hover:bg-white/5 transition-colors"
          @click="handleAction(item.action)"
        >
          <component :is="item.icon" :size="22" class="text-white/70" />
          <span class="text-xs text-white/60">{{ item.label }}</span>
        </button>
      </div>
    </NDrawerContent>
  </NDrawer>
</template>
