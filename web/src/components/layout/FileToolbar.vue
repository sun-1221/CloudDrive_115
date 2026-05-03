<script setup lang="ts">
import { computed } from 'vue';
import { NButton, NButtonGroup, NDropdown } from 'naive-ui';
import {
  List,
  LayoutGrid,
  FolderPlus,
  Upload,
  RefreshCw,
  ArrowUpDown,
  Filter,
  Download,
  Move,
  Copy,
  Trash2,
} from 'lucide-vue-next';
import { useFileStore } from '@/stores/fileStore';
import { useUploadStore } from '@/stores/uploadStore';
import SearchBar from '@/components/common/SearchBar.vue';

const fileStore = useFileStore();
const uploadStore = useUploadStore();

const hasSelection = computed(() => fileStore.selectedFileIds.length > 0);
const selectionCount = computed(() => fileStore.selectedFileIds.length);
const allSelectedAreFiles = computed(() =>
  fileStore.selectedFiles.every((f) => !f.isDirectory),
);

const sortOptions = [
  { label: '名称升序', key: 'name-asc' },
  { label: '名称降序', key: 'name-desc' },
  { label: '大小升序', key: 'size-asc' },
  { label: '大小降序', key: 'size-desc' },
  { label: '修改时间升序', key: 'modifiedTime-asc' },
  { label: '修改时间降序', key: 'modifiedTime-desc' },
];

const filterOptions = [
  { label: '全部', key: '' },
  { label: '文件夹', key: 'folder' },
  { label: '图片', key: 'image' },
  { label: '视频', key: 'video' },
  { label: '文档', key: 'document' },
  { label: '音频', key: 'audio' },
  { label: '压缩包', key: 'archive' },
];

function handleSortSelect(key: string) {
  const [field, order] = key.split('-') as ['name' | 'size' | 'modifiedTime', 'asc' | 'desc'];
  fileStore.setSort(field, order);
}

function handleFilterSelect(key: string) {
  fileStore.setFilter(key);
}

function handleRefresh() {
  fileStore.refreshDirectory();
}

function handleCreateFolder() {
  // 由父组件或模态框处理
}

function handleUploadClick() {
  const input = document.createElement('input');
  input.type = 'file';
  input.multiple = true;
  input.onchange = () => {
    if (input.files && input.files.length > 0) {
      uploadStore.enqueue(Array.from(input.files), fileStore.currentDirectoryId);
    }
  };
  input.click();
}

function handleSearch(keyword: string) {
  const { useSearchStore } = require('@/stores/searchStore');
  const searchStore = useSearchStore();
  searchStore.search(keyword);
}

const emit = defineEmits<{
  createFolder: [];
  move: [fileIds: string[]];
  copy: [fileIds: string[]];
  delete: [fileIds: string[]];
  download: [fileIds: string[]];
}>();
</script>

<template>
  <div class="flex items-center gap-2 px-4 h-12 border-b border-white/10 bg-[#0a0a0f]/60 backdrop-blur-sm shrink-0">
    <!-- 视图切换 -->
    <NButtonGroup size="small">
      <NButton
        :type="fileStore.viewMode === 'list' ? 'primary' : 'default'"
        quaternary
        @click="fileStore.setViewMode('list')"
      >
        <template #icon><List :size="16" /></template>
      </NButton>
      <NButton
        :type="fileStore.viewMode === 'grid' ? 'primary' : 'default'"
        quaternary
        @click="fileStore.setViewMode('grid')"
      >
        <template #icon><LayoutGrid :size="16" /></template>
      </NButton>
    </NButtonGroup>

    <!-- 排序 -->
    <NDropdown :options="sortOptions" @select="handleSortSelect">
      <NButton size="small" quaternary>
        <template #icon><ArrowUpDown :size="16" /></template>
      </NButton>
    </NDropdown>

    <!-- 筛选 -->
    <NDropdown :options="filterOptions" @select="handleFilterSelect">
      <NButton size="small" quaternary>
        <template #icon><Filter :size="16" /></template>
      </NButton>
    </NDropdown>

    <!-- 搜索栏 -->
    <div class="flex-1 max-w-xs">
      <SearchBar @search="handleSearch" />
    </div>

    <div class="flex-1" />

    <!-- 批量操作 -->
    <template v-if="hasSelection">
      <span class="text-xs text-white/50">已选 {{ selectionCount }} 项</span>
      <NButton size="small" quaternary @click="emit('move', fileStore.selectedFileIds)">
        <template #icon><Move :size="16" /></template>
        移动
      </NButton>
      <NButton size="small" quaternary @click="emit('copy', fileStore.selectedFileIds)">
        <template #icon><Copy :size="16" /></template>
        复制
      </NButton>
      <NButton size="small" quaternary type="error" @click="emit('delete', fileStore.selectedFileIds)">
        <template #icon><Trash2 :size="16" /></template>
        删除
      </NButton>
      <NButton
        v-if="allSelectedAreFiles"
        size="small"
        quaternary
        @click="emit('download', fileStore.selectedFileIds)"
      >
        <template #icon><Download :size="16" /></template>
        下载
      </NButton>
    </template>

    <!-- 操作按钮 -->
    <NButton size="small" quaternary @click="handleCreateFolder">
      <template #icon><FolderPlus :size="16" /></template>
    </NButton>
    <NButton size="small" quaternary @click="handleUploadClick">
      <template #icon><Upload :size="16" /></template>
    </NButton>
    <NButton size="small" quaternary @click="handleRefresh">
      <template #icon><RefreshCw :size="16" /></template>
    </NButton>
  </div>
</template>
