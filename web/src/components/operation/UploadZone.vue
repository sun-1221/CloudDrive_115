<script setup lang="ts">
import { ref } from 'vue';
import { Upload } from 'lucide-vue-next';
import { useUploadStore } from '@/stores/uploadStore';
import { useFileStore } from '@/stores/fileStore';

const uploadStore = useUploadStore();
const fileStore = useFileStore();
const isDragging = ref(false);
let dragCounter = 0;

function handleDragEnter(event: DragEvent) {
  event.preventDefault();
  dragCounter++;
  if (event.dataTransfer?.types.includes('Files')) {
    isDragging.value = true;
  }
}

function handleDragLeave() {
  dragCounter--;
  if (dragCounter <= 0) {
    dragCounter = 0;
    isDragging.value = false;
  }
}

function handleDragOver(event: DragEvent) {
  event.preventDefault();
  if (event.dataTransfer) {
    event.dataTransfer.dropEffect = 'copy';
  }
}

function handleDrop(event: DragEvent) {
  event.preventDefault();
  dragCounter = 0;
  isDragging.value = false;

  const files = event.dataTransfer?.files;
  if (files && files.length > 0) {
    uploadStore.enqueue(Array.from(files), fileStore.currentDirectoryId);
  }
}
</script>

<template>
  <div
    class="relative"
    @dragenter="handleDragEnter"
    @dragleave="handleDragLeave"
    @dragover="handleDragOver"
    @drop="handleDrop"
  >
    <slot />

    <!-- 拖拽覆盖层 -->
    <Transition name="fade">
      <div
        v-if="isDragging"
        class="absolute inset-0 z-50 flex flex-col items-center justify-center bg-indigo-500/10 backdrop-blur-sm border-2 border-dashed border-indigo-400/40 rounded-lg"
      >
        <Upload :size="48" class="text-indigo-400 mb-3" />
        <p class="text-lg font-medium text-indigo-300">释放文件以上传</p>
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
