<script setup lang="ts">
import { onUnmounted } from 'vue';
import { NButton } from 'naive-ui';
import { X, Download, Loader2, ExternalLink, ZoomIn, ZoomOut } from 'lucide-vue-next';
import { usePdfViewer } from '@/composables/usePdfViewer';

const viewer = usePdfViewer();

defineExpose({ initViewer: viewer.initViewer });

function handleClose() {
  viewer.destroy();
}

function handleKeydown(event: KeyboardEvent) {
  if (!viewer.state.value.visible) return;
  if (event.key === 'Escape') {
    handleClose();
  }
}

function openInNewWindow() {
  const url = viewer.state.value.thirdPartyPreviewUrl || viewer.state.value.previewUrl;
  if (url) window.open(url, '_blank');
}

onUnmounted(() => {
  viewer.destroy();
});
</script>

<template>
  <Teleport to="body">
    <Transition name="fade">
      <div
        v-if="viewer.state.value.visible"
        class="fixed inset-0 z-[200] bg-black/90 flex flex-col"
        @keydown="handleKeydown"
        tabindex="0"
      >
        <!-- 顶部工具栏 -->
        <div class="flex items-center justify-between px-4 h-12 bg-black/40 shrink-0">
          <span class="text-sm text-white/80 truncate max-w-xs">
            {{ viewer.state.value.fileName }}
          </span>
          <div class="flex items-center gap-1">
            <!-- 缩放控制（Direct 模式） -->
            <template v-if="viewer.state.value.renderMode === 'Direct'">
              <NButton size="small" quaternary @click="viewer.setScale(viewer.state.value.scale - 0.25)">
                <template #icon><ZoomOut :size="16" class="text-white/70" /></template>
              </NButton>
              <span class="text-xs text-white/50 w-12 text-center">
                {{ Math.round(viewer.state.value.scale * 100) }}%
              </span>
              <NButton size="small" quaternary @click="viewer.setScale(viewer.state.value.scale + 0.25)">
                <template #icon><ZoomIn :size="16" class="text-white/70" /></template>
              </NButton>
            </template>

            <NButton size="small" quaternary @click="openInNewWindow">
              <template #icon><ExternalLink :size="16" class="text-white/70" /></template>
            </NButton>
            <NButton size="small" quaternary>
              <template #icon><Download :size="16" class="text-white/70" /></template>
            </NButton>
            <NButton size="small" quaternary @click="handleClose">
              <template #icon><X :size="16" class="text-white/70" /></template>
            </NButton>
          </div>
        </div>

        <!-- 主内容区域 -->
        <div class="flex-1 flex items-center justify-center overflow-auto">
          <!-- 加载中 -->
          <div v-if="viewer.state.value.loading" class="flex flex-col items-center gap-3">
            <Loader2 :size="40" class="animate-spin text-white/40" />
            <span class="text-sm text-white/40">加载中...</span>
          </div>

          <!-- 错误态 -->
          <div v-else-if="viewer.state.value.error" class="flex flex-col items-center gap-4">
            <p class="text-red-400">{{ viewer.state.value.error }}</p>
            <div class="flex gap-2">
              <NButton size="small" type="primary" @click="viewer.retry()">重试</NButton>
              <NButton size="small" @click="openInNewWindow">
                <template #icon><ExternalLink :size="14" /></template>
                新窗口打开
              </NButton>
            </div>
          </div>

          <!-- NotAvailable 模式 -->
          <div
            v-else-if="viewer.state.value.renderMode === 'NotAvailable'"
            class="flex flex-col items-center gap-4 text-center"
          >
            <p class="text-white/60">预览服务未配置</p>
            <p class="text-sm text-white/40">此文件类型暂不支持在线预览</p>
            <NButton size="small">
              <template #icon><Download :size="14" /></template>
              下载查看
            </NButton>
          </div>

          <!-- ThirdParty 模式 -->
          <iframe
            v-else-if="viewer.state.value.renderMode === 'ThirdParty'"
            :src="viewer.state.value.thirdPartyPreviewUrl || viewer.state.value.previewUrl"
            class="w-full h-full border-0"
            sandbox="allow-scripts allow-same-origin"
          />

          <!-- Direct 模式 (PDF/TXT) -->
          <iframe
            v-else-if="viewer.state.value.renderMode === 'Direct'"
            :src="viewer.state.value.previewUrl"
            class="w-full h-full border-0 bg-white"
            :style="{ transform: `scale(${viewer.state.value.scale})`, transformOrigin: 'top center' }"
          />
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
