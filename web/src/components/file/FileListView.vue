<script setup lang="ts">
import { NButton } from 'naive-ui';
import { ArrowUp, ArrowDown } from 'lucide-vue-next';
import FileItem from './FileItem.vue';
import { useFileStore } from '@/stores/fileStore';
import type { CloudDriveFileDto } from '@/types';

const fileStore = useFileStore();

const emit = defineEmits<{
  open: [file: CloudDriveFileDto];
  contextmenu: [event: MouseEvent, file: CloudDriveFileDto];
}>();

function handleClick(file: CloudDriveFileDto, event: MouseEvent) {
  fileStore.toggleSelect(file.fileId, event.ctrlKey || event.metaKey, event.shiftKey);
}

function handleDblClick(file: CloudDriveFileDto) {
  emit('open', file);
}

function handleContextMenu(event: MouseEvent, file: CloudDriveFileDto) {
  emit('contextmenu', event, file);
}

function handleSort(field: 'name' | 'size' | 'modifiedTime') {
  const newOrder = fileStore.sortField === field && fileStore.sortOrder === 'asc' ? 'desc' : 'asc';
  fileStore.setSort(field, newOrder);
}

function getSortIcon(field: string) {
  if (fileStore.sortField !== field) return null;
  return fileStore.sortOrder === 'asc' ? ArrowUp : ArrowDown;
}
</script>

<template>
  <div>
    <!-- 表头 -->
    <div class="flex items-center gap-3 px-4 py-2 border-b border-white/10 text-xs text-white/50">
      <div class="w-5 shrink-0" />
      <div class="w-8 shrink-0" />
      <NButton quaternary size="tiny" class="flex-1 !justify-start" @click="handleSort('name')">
        名称
        <template v-if="getSortIcon('name')" #icon>
          <component :is="getSortIcon('name')!" :size="12" />
        </template>
      </NButton>
      <NButton quaternary size="tiny" class="w-20 !justify-end" @click="handleSort('size')">
        大小
        <template v-if="getSortIcon('size')" #icon>
          <component :is="getSortIcon('size')!" :size="12" />
        </template>
      </NButton>
      <span class="w-16 text-right shrink-0">类型</span>
      <NButton quaternary size="tiny" class="w-28 !justify-end" @click="handleSort('modifiedTime')">
        修改时间
        <template v-if="getSortIcon('modifiedTime')" #icon>
          <component :is="getSortIcon('modifiedTime')!" :size="12" />
        </template>
      </NButton>
    </div>

    <!-- 文件列表 -->
    <div class="divide-y divide-white/5">
      <FileItem
        v-for="file in fileStore.displayedFiles"
        :key="file.fileId"
        :file="file"
        mode="list"
        :selected="fileStore.selectedFileIds.includes(file.fileId)"
        :focused="fileStore.focusedFileId === file.fileId"
        @click="handleClick(file, $event)"
        @dblclick="handleDblClick(file)"
        @contextmenu="handleContextMenu($event, file)"
      />
    </div>
  </div>
</template>
