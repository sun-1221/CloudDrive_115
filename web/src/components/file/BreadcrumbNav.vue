<script setup lang="ts">
import { computed } from 'vue';
import { NBreadcrumb, NBreadcrumbItem, NButton } from 'naive-ui';
import { ArrowLeft } from 'lucide-vue-next';
import { useFileStore } from '@/stores/fileStore';
import { useSearchStore } from '@/stores/searchStore';
import { useRoute } from 'vue-router';

const route = useRoute();
const fileStore = useFileStore();
const searchStore = useSearchStore();

const isSearchMode = computed(() => route.path.startsWith('/cloud-drive/search'));

function handleBreadcrumbClick(dirId: string) {
  fileStore.navigateTo(dirId);
}

function handleBackToBrowse() {
  searchStore.restorePreviousDirectory();
}
</script>

<template>
  <div class="flex items-center gap-2 px-4 py-2">
    <!-- 搜索结果模式 -->
    <template v-if="isSearchMode">
      <NButton size="small" quaternary @click="handleBackToBrowse">
        <template #icon><ArrowLeft :size="14" /></template>
        返回浏览
      </NButton>
      <span class="text-sm text-white/60">{{ searchStore.breadcrumbLabel }}</span>
    </template>

    <!-- 文件浏览模式 -->
    <NBreadcrumb v-else>
      <NBreadcrumbItem
        v-for="crumb in fileStore.breadcrumbs"
        :key="crumb.id"
        @click="handleBreadcrumbClick(crumb.id)"
      >
        {{ crumb.name }}
      </NBreadcrumbItem>
    </NBreadcrumb>
  </div>
</template>
