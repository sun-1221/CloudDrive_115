<script setup lang="ts">
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
</script>

<template>
  <div class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-3 p-4">
    <FileItem
      v-for="file in fileStore.displayedFiles"
      :key="file.fileId"
      :file="file"
      mode="grid"
      :selected="fileStore.selectedFileIds.includes(file.fileId)"
      :focused="fileStore.focusedFileId === file.fileId"
      @click="handleClick(file, $event)"
      @dblclick="handleDblClick(file)"
      @contextmenu="handleContextMenu($event, file)"
    />
  </div>
</template>
