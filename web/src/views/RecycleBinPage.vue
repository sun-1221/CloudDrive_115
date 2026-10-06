<script setup lang="ts">
import { onMounted, watch } from 'vue';
import { NEmpty } from 'naive-ui';
import FileTypeIcon from '@/components/common/FileTypeIcon.vue';
import SkeletonLoader from '@/components/common/SkeletonLoader.vue';
import ErrorState from '@/components/common/ErrorState.vue';
import { useRecycleBinStore } from '@/stores/recycleBinStore';
import { useRoute } from 'vue-router';

const route = useRoute();
const recycleBinStore = useRecycleBinStore();

onMounted(() => {
  recycleBinStore.fetchItems();
});

watch(() => route.path, () => {
  if (route.path.includes('recycle-bin') && recycleBinStore.needsRefresh) {
    recycleBinStore.refresh();
  }
});

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

    </div>

    <!-- 加载状态 -->
    <SkeletonLoader v-if="recycleBinStore.loading" mode="list" />

    <!-- 错误状态 -->
    <ErrorState
      v-else-if="recycleBinStore.error"
      :message="recycleBinStore.error"
      @retry="recycleBinStore.fetchItems()"
    />

    <!-- 明细未采集态与真实空态必须区分 -->
    <div
      v-else-if="recycleBinStore.itemSchemaStatus === 'not_collected'"
      class="flex-1 flex items-center justify-center"
    >
      <NEmpty description="明细暂不可用" />
    </div>
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
          :key="item.id"
          class="flex items-center gap-3 px-4 py-2.5"
        >
          <FileTypeIcon :file-name="item.fileName" :is-directory="item.type === 'folder'" />
          <span class="flex-1 text-sm text-white/90 truncate min-w-0">{{ item.fileName }}</span>
          <span class="text-xs text-white/40 w-20 text-right shrink-0">
            {{ item.fileSize || '-' }}
          </span>
          <span class="text-xs text-white/40 w-32 text-right shrink-0 truncate" :title="item.parentName">
            {{ item.parentName || '-' }}
          </span>
          <span class="text-xs text-white/40 w-28 text-right shrink-0">
            {{ item.dtime || '-' }}
          </span>
          <span class="text-xs text-white/40 w-16 text-right shrink-0">{{ item.status || '-' }}</span>
          <span class="text-xs text-white/40 w-16 text-right shrink-0">{{ item.type || '-' }}</span>
        </div>
      </div>
    </div>
  </div>
</template>
