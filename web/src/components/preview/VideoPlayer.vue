<script setup lang="ts">
import { onUnmounted, ref, watch } from 'vue';
import { NButton, NDropdown } from 'naive-ui';
import { X, Download, Loader2, RefreshCw, ListTodo } from 'lucide-vue-next';
import { useVideoPlayer } from '@/composables/useVideoPlayer';
import { formatDuration } from '@/utils/formatters';

const player = useVideoPlayer();
const videoRef = ref<HTMLVideoElement | null>(null);

defineExpose({ initPlayer: player.initPlayer });

// 切换清晰度时保持播放进度
watch(() => player.state.value.playUrl, () => {
  if (videoRef.value && player.state.value.savedTime > 0) {
    videoRef.value.currentTime = player.state.value.savedTime;
  }
});

function handleBeforeQualitySwitch() {
  if (videoRef.value) {
    player.state.value.savedTime = videoRef.value.currentTime;
  }
}

function handleQualitySelect(quality: string) {
  handleBeforeQualitySwitch();
  player.switchQuality(quality);
}

const qualityDropdownOptions = ref<{ label: string; key: string }[]>([]);
watch(() => player.state.value.qualityOptions, (opts) => {
  qualityDropdownOptions.value = opts.map((o) => ({ label: o.quality, key: o.quality }));
}, { immediate: true });

const subtitleDropdownOptions = ref<{ label: string; key: string }[]>([]);
watch(() => player.state.value.subtitles, (subs) => {
  subtitleDropdownOptions.value = [
    { label: '关闭字幕', key: '__off__' },
    ...subs.map((s) => ({ label: s.language, key: s.subtitleUrl })),
  ];
}, { immediate: true });

const activeSubtitleUrl = ref('');

function handleSubtitleSelect(key: string) {
  activeSubtitleUrl.value = key === '__off__' ? '' : key;
}

function handleClose() {
  if (videoRef.value) {
    videoRef.value.pause();
  }
  player.destroy();
}

function handleKeydown(event: KeyboardEvent) {
  if (!player.state.value.visible) return;
  if (event.key === 'Escape') {
    handleClose();
  }
}

onUnmounted(() => {
  player.destroy();
});
</script>

<template>
  <Teleport to="body">
    <Transition name="fade">
      <div
        v-if="player.state.value.visible"
        class="fixed inset-0 z-[200] bg-black/95 flex flex-col"
        @keydown="handleKeydown"
        tabindex="0"
      >
        <!-- 顶部工具栏 -->
        <div class="flex items-center justify-between px-4 h-12 bg-black/40 shrink-0">
          <span class="text-sm text-white/80 truncate max-w-xs">
            {{ player.state.value.fileName }}
          </span>
          <div class="flex items-center gap-1">
            <!-- 清晰度切换 -->
            <NDropdown
              v-if="player.hasMultipleQualities.value"
              :options="qualityDropdownOptions"
              @select="handleQualitySelect"
            >
              <NButton size="small" quaternary>
                {{ player.state.value.currentQuality || '清晰度' }}
              </NButton>
            </NDropdown>

            <!-- 字幕切换 -->
            <NDropdown
              v-if="player.hasSubtitles.value"
              :options="subtitleDropdownOptions"
              @select="handleSubtitleSelect"
            >
              <NButton size="small" quaternary>字幕</NButton>
            </NDropdown>

            <NButton size="small" quaternary>
              <template #icon><Download :size="16" class="text-white/70" /></template>
            </NButton>
            <NButton size="small" quaternary @click="handleClose">
              <template #icon><X :size="16" class="text-white/70" /></template>
            </NButton>
          </div>
        </div>

        <!-- 主播放区域 -->
        <div class="flex-1 flex items-center justify-center overflow-hidden">
          <!-- 加载中 -->
          <div v-if="player.state.value.loading" class="flex flex-col items-center gap-3">
            <Loader2 :size="40" class="animate-spin text-white/40" />
            <span class="text-sm text-white/40">加载中...</span>
          </div>

          <!-- 错误态 -->
          <div v-else-if="player.state.value.error" class="flex flex-col items-center gap-3">
            <p class="text-red-400">{{ player.state.value.error }}</p>
            <NButton size="small" type="primary" @click="player.retry()">重试</NButton>
          </div>

          <!-- 转码中 -->
          <div
            v-else-if="player.state.value.status === 'Transcoding'"
            class="flex flex-col items-center gap-4 text-center"
          >
            <Loader2 :size="48" class="animate-spin text-indigo-400" />
            <p class="text-white/80">视频正在转码中，请稍后</p>
            <p class="text-sm text-white/40">
              预计时长：{{ formatDuration(player.state.value.duration) }}
            </p>
            <div class="flex gap-2">
              <NButton size="small" @click="player.refreshStatus()">
                <template #icon><RefreshCw :size="14" /></template>
                刷新状态
              </NButton>
              <NButton size="small" quaternary>
                <template #icon><ListTodo :size="14" /></template>
                查看任务
              </NButton>
            </div>
          </div>

          <!-- 播放器 -->
          <video
            v-else-if="player.state.value.playUrl"
            ref="videoRef"
            class="max-w-full max-h-full"
            controls
            autoplay
            :src="player.state.value.playUrl"
          >
            <track
              v-if="activeSubtitleUrl"
              kind="subtitles"
              :src="activeSubtitleUrl"
              default
            />
          </video>
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
