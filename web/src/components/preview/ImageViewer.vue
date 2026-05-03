<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue';
import { NButton } from 'naive-ui';
import {
  X,
  ChevronLeft,
  ChevronRight,
  ZoomIn,
  ZoomOut,
  RotateCcw,
  Download,
  Loader2,
} from 'lucide-vue-next';
import { useImageViewer } from '@/composables/useImageViewer';

const viewer = useImageViewer();

defineExpose({ open: viewer.open });

function handleWheel(event: WheelEvent) {
  event.preventDefault();
  const delta = event.deltaY > 0 ? -0.1 : 0.1;
  viewer.zoomBy(delta);
}

function handleDblClick() {
  if (viewer.state.value.zoom === 1) {
    viewer.zoomBy(1);
  } else {
    viewer.resetZoom();
  }
}

function handleKeydown(event: KeyboardEvent) {
  if (!viewer.state.value.visible) return;
  switch (event.key) {
    case 'ArrowLeft':
      viewer.prev();
      break;
    case 'ArrowRight':
      viewer.next();
      break;
    case 'Escape':
      viewer.close();
      break;
    case '+':
    case '=':
      viewer.zoomBy(0.2);
      break;
    case '-':
      viewer.zoomBy(-0.2);
      break;
    case '0':
      viewer.resetZoom();
      break;
  }
}

// 拖拽平移
let isDragging = false;
let lastX = 0;
let lastY = 0;

function handleMouseDown(event: MouseEvent) {
  if (viewer.state.value.zoom <= 1) return;
  isDragging = true;
  lastX = event.clientX;
  lastY = event.clientY;
}

function handleMouseMove(event: MouseEvent) {
  if (!isDragging) return;
  const dx = event.clientX - lastX;
  const dy = event.clientY - lastY;
  lastX = event.clientX;
  lastY = event.clientY;
  viewer.panBy(dx, dy);
}

function handleMouseUp() {
  isDragging = false;
}

onMounted(() => {
  document.addEventListener('keydown', handleKeydown);
});

onUnmounted(() => {
  document.removeEventListener('keydown', handleKeydown);
  viewer.close();
});
</script>

<template>
  <Teleport to="body">
    <Transition name="fade">
      <div
        v-if="viewer.state.value.visible"
        class="fixed inset-0 z-[200] bg-black/90 flex flex-col"
      >
        <!-- 顶部工具栏 -->
        <div class="flex items-center justify-between px-4 h-12 bg-black/40 shrink-0">
          <span class="text-sm text-white/80 truncate max-w-xs">
            {{ viewer.state.value.fileName }}
          </span>
          <div class="flex items-center gap-1">
            <NButton size="small" quaternary @click="viewer.zoomBy(-0.2)">
              <template #icon><ZoomOut :size="16" class="text-white/70" /></template>
            </NButton>
            <span class="text-xs text-white/50 w-12 text-center">
              {{ Math.round(viewer.state.value.zoom * 100) }}%
            </span>
            <NButton size="small" quaternary @click="viewer.zoomBy(0.2)">
              <template #icon><ZoomIn :size="16" class="text-white/70" /></template>
            </NButton>
            <NButton size="small" quaternary @click="viewer.resetZoom()">
              <template #icon><RotateCcw :size="16" class="text-white/70" /></template>
            </NButton>
            <NButton size="small" quaternary>
              <template #icon><Download :size="16" class="text-white/70" /></template>
            </NButton>
            <NButton size="small" quaternary @click="viewer.close()">
              <template #icon><X :size="16" class="text-white/70" /></template>
            </NButton>
          </div>
        </div>

        <!-- 主图片区域 -->
        <div
          class="flex-1 flex items-center justify-center overflow-hidden relative select-none"
          @wheel="handleWheel"
          @mousedown="handleMouseDown"
          @mousemove="handleMouseMove"
          @mouseup="handleMouseUp"
          @mouseleave="handleMouseUp"
        >
          <!-- 加载中 -->
          <div v-if="viewer.state.value.loading" class="flex flex-col items-center gap-3">
            <Loader2 :size="40" class="animate-spin text-white/40" />
            <span class="text-sm text-white/40">加载中...</span>
          </div>

          <!-- 错误态 -->
          <div v-else-if="viewer.state.value.error" class="flex flex-col items-center gap-3">
            <p class="text-red-400">{{ viewer.state.value.error }}</p>
            <NButton size="small" type="primary" @click="viewer.retry()">重试</NButton>
          </div>

          <!-- 图片 -->
          <img
            v-else-if="viewer.state.value.imageUrl"
            :src="viewer.state.value.imageUrl"
            :alt="viewer.state.value.fileName"
            class="max-w-full max-h-full object-contain transition-transform duration-150"
            :style="{
              transform: `translate(${viewer.state.value.panX}px, ${viewer.state.value.panY}px) scale(${viewer.state.value.zoom})`,
              cursor: viewer.state.value.zoom > 1 ? 'grab' : 'default',
            }"
            draggable="false"
            @dblclick="handleDblClick"
          />

          <!-- 左右切换按钮 -->
          <button
            v-if="viewer.canPrev.value"
            class="absolute left-4 top-1/2 -translate-y-1/2 w-10 h-10 rounded-full bg-black/40 flex items-center justify-center hover:bg-black/60 transition-colors"
            @click="viewer.prev()"
          >
            <ChevronLeft :size="24" class="text-white/70" />
          </button>
          <button
            v-if="viewer.canNext.value"
            class="absolute right-4 top-1/2 -translate-y-1/2 w-10 h-10 rounded-full bg-black/40 flex items-center justify-center hover:bg-black/60 transition-colors"
            @click="viewer.next()"
          >
            <ChevronRight :size="24" class="text-white/70" />
          </button>
        </div>

        <!-- 底部缩略图导航 -->
        <div
          v-if="viewer.imageCollection.value.length > 1"
          class="flex items-center gap-2 px-4 py-2 bg-black/40 overflow-x-auto shrink-0"
        >
          <button
            v-for="(img, idx) in viewer.imageCollection.value"
            :key="img.fileId"
            class="w-12 h-12 rounded border-2 shrink-0 overflow-hidden transition-colors"
            :class="idx === viewer.state.value.currentIndex ? 'border-indigo-400' : 'border-transparent opacity-60'"
            @click="viewer.open(viewer.imageCollection.value, idx)"
          >
            <img
              v-if="img.thumbnailUrl"
              :src="img.thumbnailUrl"
              :alt="img.name"
              class="w-full h-full object-cover"
            />
            <div v-else class="w-full h-full bg-white/10 flex items-center justify-center text-[8px] text-white/40">
              {{ idx + 1 }}
            </div>
          </button>
        </div>
      </div>
    </Transition>
  </Teleport>
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
