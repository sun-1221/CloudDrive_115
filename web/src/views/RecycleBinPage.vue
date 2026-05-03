<script setup lang="ts">
import { onMounted, computed, watch } from 'vue';
import { NButton, NEmpty, useDialog } from 'naive-ui';
import { Undo2, Trash2, Trash } from 'lucide-vue-next';
import FileTypeIcon from '@/components/common/FileTypeIcon.vue';
import SkeletonLoader from '@/components/common/SkeletonLoader.vue';
import ErrorState from '@/components/common/ErrorState.vue';
import { useRecycleBinStore } from '@/stores/recycleBinStore';
import { formatFileSize, formatRelativeDate } from '@/utils/formatters';
import { useRoute } from 'vue-router';

const route = useRoute();
const recycleBinStore = useRecycleBinStore();
const dialog = useDialog();

const hasSelection = computed(() => recycleBinStore.selectedFileIds.length > 0);

onMounted(() => {
  recycleBinStore.fetchItems();
});

watch(() => route.path, () => {
  if (route.path.includes('recycle-bin') && recycleBinStore.needsRefresh) {
    recycleBinStore.refresh();
  }
});

function handleRestore() {
  recycleBinStore.restore(recycleBinStore.selectedFileIds);
}

function handlePermanentDelete() {
  dialog.warning({
    title: '永久删除',
    content: `确定要永久删除选中的 ${recycleBinStore.selectedFileIds.length} 个项目吗？此操作无法撤销。`,
    positiveText: '永久删除',
    negativeText: '取消',
    onPositiveClick: () => {
      recycleBinStore.permanentDelete(recycleBinStore.selectedFileIds);
    },
  });
}

function handleClearAll() {
  dialog.error({
    title: '清空回收站',
    content: '确定要清空回收站吗？所有文件将被永久删除，此操作无法撤销。',
    positiveText: '清空',
    negativeText: '取消',
    onPositiveClick: () => {
      recycleBinStore.clearAll();
    },
  });
}

function toggleSelect(fileId: string) {
  recycleBinStore.toggleSelect(fileId);
}

function handleScroll(event: Event) {
  const target = event.target as HTMLElement;
  if (!target) return;
  const { scrollTop, scrollHeight, clientHeight } = target;
  if (scrollHeight - scrollTop - clientHeight < 200) {
    recycleBinStore.fetchMore();
  }
}
</script>

<template>
  <div class="flex flex-col h-full">
    <!-- 工具栏 -->
    <div class="flex items-center gap-2 px-4 h-12 border-b border-white/10 shrink-0">
      <h2 class="text-sm font-medium text-white/80">回收站</h2>
      <div class="flex-1" />

      <template v-if="hasSelection">
        <NButton size="small" quaternary @click="handleRestore">
          <template #icon><Undo2 :size="16" /></template>
          恢复
        </NButton>
        <NButton size="small" quaternary type="error" @click="handlePermanentDelete">
          <template #icon><Trash2 :size="16" /></template>
          永久删除
        </NButton>
      </template>

      <NButton
        v-if="recycleBinStore.items.length > 0"
        size="small"
        quaternary
        type="error"
        @click="handleClearAll"
      >
        <template #icon><Trash :size="16" /></template>
        清空回收站
      </NButton>
    </div>

    <!-- 加载状态 -->
    <SkeletonLoader v-if="recycleBinStore.loading" mode="list" />

    <!-- 错误状态 -->
    <ErrorState
      v-else-if="recycleBinStore.error"
      :message="recycleBinStore.error"
      @retry="recycleBinStore.fetchItems()"
    />

    <!-- 空状态 -->
    <div
      v-else-if="recycleBinStore.items.length === 0"
      class="flex-1 flex items-center justify-center"
    >
      <NEmpty description="回收站为空" />
    </div>

    <!-- 回收站列表 -->
    <div v-else class="flex-1 overflow-y-auto" @scroll="handleScroll">
      <div class="divide-y divide-white/5">
        <div
          v-for="item in recycleBinStore.items"
          :key="item.fileId"
          class="flex items-center gap-3 px-4 py-2.5 cursor-pointer transition-colors"
          :class="recycleBinStore.selectedFileIds.includes(item.fileId) ? 'bg-indigo-500/15' : 'hover:bg-white/5'"
          @click="toggleSelect(item.fileId)"
        >
          <FileTypeIcon :file-name="item.name" :is-directory="item.isDirectory" />
          <span class="flex-1 text-sm text-white/90 truncate min-w-0">{{ item.name }}</span>
          <span class="text-xs text-white/40 w-20 text-right shrink-0">
            {{ item.isDirectory ? '文件夹' : formatFileSize(item.size) }}
          </span>
          <span class="text-xs text-white/40 w-28 text-right shrink-0">
            {{ formatRelativeDate(item.deletedTime) }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>
