<script setup lang="ts">
import { computed, onMounted, onUnmounted } from 'vue';
import {
  FolderOpen,
  Eye,
  Play,
  Pencil,
  Move,
  Copy,
  Trash2,
  Download,
  Info,
  FolderPlus,
  Upload,
  RefreshCw,
  ClipboardPaste,
} from 'lucide-vue-next';
import type { CloudDriveFileDto } from '@/types';
import { getFileExtension, extensionCategoryMap } from '@/utils/fileTypeMapping';

const props = defineProps<{
  x: number;
  y: number;
  files: CloudDriveFileDto[];
  hasClipboard: boolean;
}>();

const emit = defineEmits<{
  close: [];
  action: [action: string, files: CloudDriveFileDto[]];
}>();

const isSingle = computed(() => props.files.length === 1);
const isMulti = computed(() => props.files.length > 1);
const isEmpty = computed(() => props.files.length === 0);
const singleFile = computed(() => isSingle.value ? props.files[0] : null);
const isFolder = computed(() => singleFile.value?.isDirectory ?? false);
const allAreFiles = computed(() => props.files.every((f) => !f.isDirectory));

const isPreviewable = computed(() => {
  if (!singleFile.value || singleFile.value.isDirectory) return false;
  const ext = getFileExtension(singleFile.value.name);
  const cat = extensionCategoryMap[ext];
  return cat === 'image' || cat === 'document';
});

const isPlayable = computed(() => {
  if (!singleFile.value || singleFile.value.isDirectory) return false;
  const ext = getFileExtension(singleFile.value.name);
  return extensionCategoryMap[ext] === 'video';
});

interface MenuItem {
  label: string;
  action: string;
  icon: typeof FolderOpen;
  danger?: boolean;
  show: boolean;
}

const menuItems = computed<MenuItem[]>(() => {
  if (isEmpty.value) {
    return [
      { label: '新建文件夹', action: 'createFolder', icon: FolderPlus, show: true },
      { label: '上传文件', action: 'upload', icon: Upload, show: true },
      { label: '刷新', action: 'refresh', icon: RefreshCw, show: true },
      { label: '粘贴', action: 'paste', icon: ClipboardPaste, show: props.hasClipboard },
    ];
  }

  return [
    { label: '打开', action: 'open', icon: FolderOpen, show: isSingle.value && isFolder.value },
    { label: '预览', action: 'preview', icon: Eye, show: isSingle.value && isPreviewable.value },
    { label: '播放', action: 'play', icon: Play, show: isSingle.value && isPlayable.value },
    { label: '重命名', action: 'rename', icon: Pencil, show: isSingle.value },
    { label: '移动', action: 'move', icon: Move, show: true },
    { label: '复制', action: 'copy', icon: Copy, show: true },
    { label: '删除', action: 'delete', icon: Trash2, danger: true, show: true },
    { label: '下载', action: 'download', icon: Download, show: isSingle.value && !isFolder.value },
    { label: '逐个下载', action: 'downloadMulti', icon: Download, show: isMulti.value && allAreFiles.value },
    { label: '详情', action: 'detail', icon: Info, show: isSingle.value },
  ];
});

const visibleItems = computed(() => menuItems.value.filter((i) => i.show));

function handleAction(action: string) {
  emit('close');
  emit('action', action, props.files);
}

function handleClickOutside(event: MouseEvent) {
  const target = event.target as HTMLElement;
  if (!target.closest('.context-menu')) {
    emit('close');
  }
}

function handleEscape(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    emit('close');
  }
}

onMounted(() => {
  document.addEventListener('click', handleClickOutside, true);
  document.addEventListener('keydown', handleEscape);
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside, true);
  document.removeEventListener('keydown', handleEscape);
});
</script>

<template>
  <Teleport to="body">
    <div
      class="context-menu fixed z-[100] min-w-[180px] py-1 rounded-lg glass-card border border-white/10 shadow-xl"
      :style="{ left: `${x}px`, top: `${y}px` }"
    >
      <button
        v-for="item in visibleItems"
        :key="item.action"
        class="w-full flex items-center gap-3 px-4 py-2 text-sm transition-colors duration-150"
        :class="item.danger ? 'text-red-400 hover:bg-red-500/10' : 'text-white/80 hover:bg-white/5'"
        @click="handleAction(item.action)"
      >
        <component :is="item.icon" :size="14" class="shrink-0" />
        {{ item.label }}
      </button>
    </div>
  </Teleport>
</template>
