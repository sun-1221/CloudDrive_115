<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { NModal, NButton, NEmpty } from 'naive-ui';
import { Folder, ChevronRight, ArrowUp } from 'lucide-vue-next';
import * as fileApi from '@/api/fileApi';
import { useAuthStore } from '@/stores/authStore';
import type { CloudDriveFileDto } from '@/types';

const props = defineProps<{
  show: boolean;
  title: string;
  excludeDirectoryIds?: string[];
}>();

const emit = defineEmits<{
  'update:show': [value: boolean];
  confirm: [directoryId: string];
}>();

const authStore = useAuthStore();
const currentDirId = ref('0');
const currentDirName = ref('根目录');
const folders = ref<CloudDriveFileDto[]>([]);
const loading = ref(false);
const parentStack = ref<{ id: string; name: string }[]>([]);

async function loadFolders(dirId: string, dirName: string) {
  loading.value = true;
  try {
    const uid = authStore.currentAccountUid;
    if (!uid) return;
    const result = await fileApi.getFileList({
      cloudAccountUid: uid,
      directoryId: dirId,
      offset: 0,
      limit: 200,
    });
    folders.value = result.items.filter(
      (f) => f.isDirectory && !(props.excludeDirectoryIds ?? []).includes(f.fileId),
    );
    currentDirId.value = dirId;
    currentDirName.value = dirName;
  } finally {
    loading.value = false;
  }
}

function enterFolder(folder: CloudDriveFileDto) {
  parentStack.value.push({ id: currentDirId.value, name: currentDirName.value });
  loadFolders(folder.fileId, folder.name);
}

function goUp() {
  const parent = parentStack.value.pop();
  if (parent) {
    loadFolders(parent.id, parent.name);
  }
}

function handleConfirm() {
  emit('confirm', currentDirId.value);
  emit('update:show', false);
}

function handleCancel() {
  emit('update:show', false);
}

onMounted(() => {
  loadFolders('0', '根目录');
});
</script>

<template>
  <NModal :show="show" preset="card" :title="title" style="width: 480px;" @update:show="emit('update:show', $event)">
    <div class="space-y-3">
      <!-- 当前路径 -->
      <div class="flex items-center gap-2 text-sm text-white/60">
        <NButton v-if="parentStack.length > 0" size="tiny" quaternary @click="goUp">
          <template #icon><ArrowUp :size="14" /></template>
        </NButton>
        <span>{{ currentDirName }}</span>
      </div>

      <!-- 文件夹列表 -->
      <div class="max-h-64 overflow-y-auto border border-white/10 rounded-lg">
        <NEmpty v-if="!loading && folders.length === 0" description="无子文件夹" size="small" class="py-8" />
        <div v-else class="divide-y divide-white/5">
          <button
            v-for="folder in folders"
            :key="folder.fileId"
            class="w-full flex items-center gap-3 px-4 py-2.5 text-left hover:bg-white/5 transition-colors"
            @click="enterFolder(folder)"
          >
            <Folder :size="16" class="text-yellow-400/80 shrink-0" />
            <span class="text-sm text-white/80 truncate flex-1">{{ folder.name }}</span>
            <ChevronRight :size="14" class="text-white/30 shrink-0" />
          </button>
        </div>
      </div>
    </div>

    <template #footer>
      <div class="flex justify-end gap-2">
        <NButton size="small" @click="handleCancel">取消</NButton>
        <NButton size="small" type="primary" @click="handleConfirm">
          选择此目录
        </NButton>
      </div>
    </template>
  </NModal>
</template>
