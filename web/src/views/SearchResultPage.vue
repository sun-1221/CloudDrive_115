<script setup lang="ts">
import { useRouter } from 'vue-router';
import { NEmpty } from 'naive-ui';
import BreadcrumbNav from '@/components/file/BreadcrumbNav.vue';
import SkeletonLoader from '@/components/common/SkeletonLoader.vue';
import FileTypeIcon from '@/components/common/FileTypeIcon.vue';
import { useSearchStore } from '@/stores/searchStore';
import { useFileStore } from '@/stores/fileStore';
import { formatFileSize, formatRelativeDate } from '@/utils/formatters';
import type { CloudDriveSearchResultDto } from '@/types';

const router = useRouter();
const searchStore = useSearchStore();
const fileStore = useFileStore();

function handleOpenResult(result: CloudDriveSearchResultDto) {
  if (result.isDirectory) {
    fileStore.navigateTo(result.fileId, result.name);
    router.push(`/cloud-drive/files/${result.fileId}`);
  }
  // 文件预览在后续任务中实现
}

function handleScroll(event: Event) {
  const target = event.target as HTMLElement;
  if (!target) return;
  const { scrollTop, scrollHeight, clientHeight } = target;
  if (scrollHeight - scrollTop - clientHeight < 200) {
    searchStore.fetchMore();
  }
}
</script>

<template>
  <div class="flex flex-col h-full">
    <BreadcrumbNav />

    <!-- 加载状态 -->
    <SkeletonLoader v-if="searchStore.loading" mode="list" />

    <!-- 空状态 -->
    <div
      v-else-if="searchStore.emptyStateVisible"
      class="flex-1 flex items-center justify-center"
    >
      <NEmpty description="未找到匹配的文件" />
    </div>

    <!-- 搜索结果列表 -->
    <div v-else class="flex-1 overflow-y-auto" @scroll="handleScroll">
      <div class="divide-y divide-white/5">
        <div
          v-for="result in searchStore.results"
          :key="result.fileId"
          class="flex items-center gap-3 px-4 py-2.5 cursor-pointer hover:bg-white/5 transition-colors"
          @click="handleOpenResult(result)"
        >
          <FileTypeIcon :file-name="result.name" :is-directory="result.isDirectory" />
          <div class="flex-1 min-w-0">
            <p class="text-sm text-white/90 truncate">{{ result.name }}</p>
            <p v-if="result.parentPath" class="text-xs text-white/40 truncate">
              {{ result.parentPath }}
            </p>
          </div>
          <span class="text-xs text-white/40 shrink-0">
            {{ result.isDirectory ? '文件夹' : formatFileSize(result.size) }}
          </span>
          <span class="text-xs text-white/40 w-28 text-right shrink-0">
            {{ formatRelativeDate(result.modifiedTime) }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>
