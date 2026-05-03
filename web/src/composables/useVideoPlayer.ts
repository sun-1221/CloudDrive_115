import { ref, computed } from 'vue';
import * as videoApi from '@/api/videoApi';
import { fetchWithRetry } from '@/services/temporaryResourceService';
import { useAuthStore } from '@/stores/authStore';
import type {
  CloudDriveVideoPlayDto,
  CloudDriveSubtitleDto,
  VideoQualityOption,
} from '@/types';

export interface VideoPlayerState {
  visible: boolean;
  fileName: string;
  fileId: string;
  loading: boolean;
  error: string;
  status: 'Ready' | 'Transcoding' | 'Idle';
  jobId: string | null;
  playUrl: string;
  currentQuality: string;
  qualityOptions: VideoQualityOption[];
  subtitles: CloudDriveSubtitleDto[];
  duration: number;
  savedTime: number;
}

export function useVideoPlayer() {
  const authStore = useAuthStore();

  const state = ref<VideoPlayerState>({
    visible: false,
    fileName: '',
    fileId: '',
    loading: false,
    error: '',
    status: 'Idle',
    jobId: null,
    playUrl: '',
    currentQuality: '',
    qualityOptions: [],
    subtitles: [],
    duration: 0,
    savedTime: 0,
  });

  const hasMultipleQualities = computed(() => state.value.qualityOptions.length > 1);
  const hasSubtitles = computed(() => state.value.subtitles.length > 0);

  async function initPlayer(fileId: string, fileName: string) {
    state.value.visible = true;
    state.value.fileName = fileName;
    state.value.fileId = fileId;
    state.value.loading = true;
    state.value.error = '';
    state.value.savedTime = 0;

    const uid = authStore.currentAccountUid;
    if (!uid) {
      state.value.error = '未授权';
      state.value.loading = false;
      return;
    }

    try {
      const result = await fetchWithRetry<CloudDriveVideoPlayDto>({
        fetchUrl: () => videoApi.getPlayUrl({ cloudAccountUid: uid, fileId }),
        extractUrl: (data) => data.qualityOptions[0]?.playUrl ?? '',
      });

      if (result.success && result.data) {
        applyPlayData(result.data);
      } else {
        state.value.error = result.error ?? '加载失败';
      }

      // 并行加载字幕
      try {
        state.value.subtitles = await videoApi.getSubtitles(uid, fileId);
      } catch {
        state.value.subtitles = [];
      }
    } catch (e) {
      state.value.error = e instanceof Error ? e.message : '加载失败';
    } finally {
      state.value.loading = false;
    }
  }

  function applyPlayData(data: CloudDriveVideoPlayDto) {
    state.value.status = data.status;
    state.value.jobId = data.jobId ?? null;
    state.value.qualityOptions = data.qualityOptions;
    state.value.duration = data.duration;

    if (data.status === 'Ready' && data.qualityOptions.length > 0) {
      const defaultOpt = data.qualityOptions[0]!;
      state.value.currentQuality = defaultOpt.quality;
      state.value.playUrl = defaultOpt.playUrl;
    }
  }

  async function switchQuality(quality: string) {
    const opt = state.value.qualityOptions.find((o) => o.quality === quality);
    if (!opt) return;

    state.value.currentQuality = quality;
    state.value.playUrl = opt.playUrl;
  }

  async function refreshStatus() {
    const uid = authStore.currentAccountUid;
    if (!uid || !state.value.fileId) return;

    state.value.loading = true;
    try {
      const data = await videoApi.getPlayUrl({
        cloudAccountUid: uid,
        fileId: state.value.fileId,
      });
      applyPlayData(data);
    } catch (e) {
      state.value.error = e instanceof Error ? e.message : '刷新失败';
    } finally {
      state.value.loading = false;
    }
  }

  function retry() {
    initPlayer(state.value.fileId, state.value.fileName);
  }

  function destroy() {
    state.value.visible = false;
    state.value.playUrl = '';
    state.value.error = '';
    state.value.status = 'Idle';
    state.value.qualityOptions = [];
    state.value.subtitles = [];
  }

  return {
    state,
    hasMultipleQualities,
    hasSubtitles,
    initPlayer,
    switchQuality,
    refreshStatus,
    retry,
    destroy,
  };
}
