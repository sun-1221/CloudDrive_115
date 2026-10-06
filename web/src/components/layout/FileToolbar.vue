<script setup lang="ts">
import { computed } from 'vue';
import { NButton, NButtonGroup, NDropdown, useMessage } from 'naive-ui';
import {
  List,
  LayoutGrid,
  FolderPlus,
  RefreshCw,
  ArrowUpDown,
  Filter,
  Download,
  Copy,
} from 'lucide-vue-next';
import { useFileStore } from '@/stores/fileStore';
import { isCapabilityEnabled } from '@/capabilities/registry';
import * as fileOperations from '@/services/fileOperationService';
import { validateFileName } from '@/utils/fileNameValidator';

const fileStore = useFileStore();
const message = useMessage();

const hasSelection = computed(() => fileStore.selectedFileIds.length > 0);
const selectionCount = computed(() => fileStore.selectedFileIds.length);
const canCopy = computed(() => selectionCount.value === 1 && isCapabilityEnabled('copySingle'));
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
  if (!isCapabilityEnabled('createFolder')) return;
  const name = window.prompt('新建文件夹名称');
  if (!name) return;
  const validation = validateFileName(name);
  if (!validation.valid) { message.error(validation.reason ?? '文件名无效'); return; }
  void fileOperations.createFolder(fileStore.currentDirectoryId, name)
    .then((result) => { if (!result.accepted) { message.success('文件夹已创建'); return fileStore.refreshDirectory(); } })
    .catch((error: unknown) => message.error(error instanceof Error ? error.message : '新建文件夹失败'));
}

async function handleCopy() {
  if (!canCopy.value) return;
  const file = fileStore.selectedFiles[0];
  if (!file) return;
  try {
    const result = await fileOperations.copySingle(file.fileId, fileStore.currentDirectoryId);
    if (!result.accepted) { message.success('文件已复制'); await fileStore.refreshDirectory(); }
  } catch (error) { message.error(error instanceof Error ? error.message : '复制失败'); }
}

async function handleDownload() {
  if (!allSelectedAreFiles.value || !isCapabilityEnabled('download')) return;
  for (const file of fileStore.selectedFiles) {
    if (!file.pickCode) continue;
    try {
      const result = await fileOperations.download(file.pickCode);
      const anchor = document.createElement('a');
      anchor.href = result.downloadUrl;
      anchor.download = result.fileName;
      anchor.rel = 'noopener';
      anchor.click();
    } catch (error) { message.error(error instanceof Error ? error.message : '下载失败'); }
  }
}

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

    <div class="flex-1" />

    <!-- 批量操作 -->
    <template v-if="hasSelection">
      <span class="text-xs text-white/50">已选 {{ selectionCount }} 项</span>
      <NButton v-if="canCopy" size="small" quaternary @click="handleCopy">
        <template #icon><Copy :size="16" /></template>
        复制
      </NButton>
      <NButton
        v-if="allSelectedAreFiles"
        size="small"
        quaternary
        @click="handleDownload"
      >
        <template #icon><Download :size="16" /></template>
        下载
      </NButton>
    </template>

    <!-- 操作按钮 -->
    <NButton size="small" quaternary @click="handleCreateFolder">
      <template #icon><FolderPlus :size="16" /></template>
    </NButton>
    <NButton size="small" quaternary @click="handleRefresh">
      <template #icon><RefreshCw :size="16" /></template>
    </NButton>
  </div>
</template>
