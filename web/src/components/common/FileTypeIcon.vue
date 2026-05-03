<script setup lang="ts">
import { computed } from 'vue';
import {
  Folder,
  Image,
  Film,
  FileText,
  Music,
  Archive,
  File,
} from 'lucide-vue-next';
import { getFileExtension, extensionCategoryMap, getFileColor } from '@/utils/fileTypeMapping';
import type { FileCategory } from '@/types';

const props = defineProps<{
  fileName: string;
  isDirectory: boolean;
  size?: number;
}>();

const category = computed<FileCategory>(() => {
  if (props.isDirectory) return 'folder';
  const ext = getFileExtension(props.fileName);
  return extensionCategoryMap[ext] ?? 'other';
});

const color = computed(() => getFileColor(category.value));

const iconComponent = computed(() => {
  switch (category.value) {
    case 'folder': return Folder;
    case 'image': return Image;
    case 'video': return Film;
    case 'document': return FileText;
    case 'audio': return Music;
    case 'archive': return Archive;
    default: return File;
  }
});
</script>

<template>
  <component
    :is="iconComponent"
    :size="size ?? 20"
    :style="{ color }"
  />
</template>
