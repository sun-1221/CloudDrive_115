<script setup lang="ts">
import { onMounted, watch, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { NAlert } from 'naive-ui';
import BreadcrumbNav from '@/components/file/BreadcrumbNav.vue';
import FileListView from '@/components/file/FileListView.vue';
import FileGridView from '@/components/file/FileGridView.vue';
import SkeletonLoader from '@/components/common/SkeletonLoader.vue';
import ErrorState from '@/components/common/ErrorState.vue';
import { useFileStore } from '@/stores/fileStore';
import type { CloudDriveFileDto } from '@/types';
import ContextMenu from '@/components/operation/ContextMenu.vue';
import * as fileOperations from '@/services/fileOperationService';
import { validateFileName } from '@/utils/fileNameValidator';
import { useMessage } from 'naive-ui';

const route = useRoute();
const router = useRouter();
const fileStore = useFileStore();
const message = useMessage();
const contextMenu = ref<{ x: number; y: number; files: CloudDriveFileDto[] } | null>(null);

onMounted(() => {
  const dirId = (route.params.directoryId as string) || '0';
  fileStore.fetchFiles(dirId);
});

watch(() => route.params.directoryId, (newDirId) => {
  if (newDirId !== undefined) {
    fileStore.fetchFiles(newDirId as string);
  }
});

function handleOpenFile(file: CloudDriveFileDto) {
  if (file.isDirectory) {
    fileStore.navigateTo(file.fileId, file.name);
    router.push({ params: { directoryId: file.fileId } });
  }
  // 文件预览在后续任务中实现
}

function handleContextMenu(event: MouseEvent, file: CloudDriveFileDto) {
  if (!fileStore.selectedFileIds.includes(file.fileId)) fileStore.toggleSelect(file.fileId);
  contextMenu.value = { x: event.clientX, y: event.clientY, files: fileStore.selectedFiles };
}

async function handleContextAction(action: string, files: CloudDriveFileDto[]) {
  contextMenu.value = null;
  const file = files[0];
  if (!file) return;
  try {
    if (action === 'refresh') return fileStore.refreshDirectory();
    if (action === 'star') {
      const result = await fileOperations.toggleStar(file.fileId, !file.isStar);
      if (!result.accepted) { message.success(file.isStar ? '已取消星标' : '已加星标'); await fileStore.refreshDirectory(); }
    } else if (action === 'rename') {
      const name = window.prompt('重命名', file.name);
      if (!name) return;
      const valid = validateFileName(name);
      if (!valid.valid) throw new Error(valid.reason);
      const result = await fileOperations.rename(file.fileId, name);
      if (!result.accepted) { message.success('已重命名'); await fileStore.refreshDirectory(); }
    } else if (action === 'copy') {
      const result = await fileOperations.copySingle(file.fileId, fileStore.currentDirectoryId);
      if (!result.accepted) { message.success('已复制'); await fileStore.refreshDirectory(); }
    } else if (action === 'download' && file.pickCode) {
      const result = await fileOperations.download(file.pickCode);
      const anchor = document.createElement('a');
      anchor.href = result.downloadUrl;
      anchor.download = result.fileName;
      anchor.rel = 'noopener';
      anchor.click();
    }
  } catch (error) {
    message.error(error instanceof Error ? error.message : '操作失败');
  }
}

function handleScroll(event: Event) {
  const target = event.target as HTMLElement;
  if (!target) return;
  const { scrollTop, scrollHeight, clientHeight } = target;
  if (scrollHeight - scrollTop - clientHeight < 200) {
    fileStore.fetchMore();
  }
}

function handleRetry() {
  fileStore.fetchFiles();
}
</script>

<template>
  <div class="flex flex-col h-full" @scroll="handleScroll">
    <BreadcrumbNav />

    <!-- 选择范围提示 -->
    <NAlert
      v-if="fileStore.selectionScopeNotice"
      type="info"
      closable
      class="mx-4 mb-2"
      @close="fileStore.acknowledgeSelectionScopeNotice()"
    >
      {{ fileStore.selectionScopeNotice }}
    </NAlert>

    <!-- 加载状态 -->
    <SkeletonLoader v-if="fileStore.loading" :mode="fileStore.viewMode" />

    <!-- 错误状态 -->
    <ErrorState
      v-else-if="fileStore.error"
      :message="fileStore.error"
      @retry="handleRetry"
    />

    <!-- 空状态 -->
    <div
      v-else-if="fileStore.displayedFiles.length === 0"
      class="flex-1 flex items-center justify-center"
    >
      <p class="text-white/40 text-sm">当前目录为空</p>
    </div>

    <!-- 文件列表/网格 -->
    <div v-else class="flex-1 overflow-y-auto" @scroll="handleScroll">
      <FileListView
        v-if="fileStore.viewMode === 'list'"
        @open="handleOpenFile"
        @contextmenu="handleContextMenu"
      />
      <FileGridView
        v-else
        @open="handleOpenFile"
        @contextmenu="handleContextMenu"
      />

      <!-- 加载更多指示器 -->
      <div v-if="fileStore.loadingMore" class="py-4 text-center">
        <span class="text-xs text-white/40">加载更多...</span>
      </div>
    </div>
  </div>
  <ContextMenu
    v-if="contextMenu"
    :x="contextMenu.x"
    :y="contextMenu.y"
    :files="contextMenu.files"
    :has-clipboard="false"
    @close="contextMenu = null"
    @action="handleContextAction"
  />
</template>
