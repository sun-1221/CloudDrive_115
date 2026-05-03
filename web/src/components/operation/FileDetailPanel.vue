<script setup lang="ts">
import { ref, watch } from 'vue';
import { NDrawer, NDrawerContent, NDescriptions, NDescriptionsItem } from 'naive-ui';
import FileTypeIcon from '@/components/common/FileTypeIcon.vue';
import { formatFileSize, formatDate } from '@/utils/formatters';
import * as fileApi from '@/api/fileApi';
import { useAuthStore } from '@/stores/authStore';
import type { CloudDriveFileDetailDto } from '@/types';

const props = defineProps<{
  show: boolean;
  fileId: string | null;
}>();

const emit = defineEmits<{
  'update:show': [value: boolean];
}>();

const authStore = useAuthStore();
const detail = ref<CloudDriveFileDetailDto | null>(null);
const loading = ref(false);

watch(() => props.fileId, async (id) => {
  if (!id || !props.show) {
    detail.value = null;
    return;
  }
  loading.value = true;
  try {
    const uid = authStore.currentAccountUid;
    if (!uid) return;
    detail.value = await fileApi.getFileDetail(uid, id);
  } catch {
    detail.value = null;
  } finally {
    loading.value = false;
  }
}, { immediate: true });
</script>

<template>
  <NDrawer :show="show" :width="360" placement="right" @update:show="emit('update:show', $event)">
    <NDrawerContent title="文件详情" closable>
      <div v-if="loading" class="py-8 text-center text-white/40 text-sm">加载中...</div>
      <div v-else-if="!detail" class="py-8 text-center text-white/40 text-sm">无法加载文件详情</div>
      <div v-else class="space-y-6">
        <!-- 图标与文件名 -->
        <div class="flex flex-col items-center gap-3 py-4">
          <FileTypeIcon :file-name="detail.name" :is-directory="detail.isDirectory" :size="48" />
          <p class="text-white/90 font-medium text-center break-all">{{ detail.name }}</p>
        </div>

        <!-- 元数据 -->
        <NDescriptions :column="1" label-placement="left" size="small">
          <NDescriptionsItem label="类型">
            {{ detail.isDirectory ? '文件夹' : detail.fileType }}
          </NDescriptionsItem>
          <NDescriptionsItem v-if="!detail.isDirectory" label="大小">
            {{ formatFileSize(detail.size) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="修改时间">
            {{ formatDate(detail.modifiedTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="创建时间">
            {{ formatDate(detail.createdTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem v-if="detail.path" label="路径">
            {{ detail.path }}
          </NDescriptionsItem>
          <NDescriptionsItem v-if="detail.sha1" label="SHA1">
            <code class="text-xs text-white/50 break-all">{{ detail.sha1 }}</code>
          </NDescriptionsItem>
        </NDescriptions>
      </div>
    </NDrawerContent>
  </NDrawer>
</template>
