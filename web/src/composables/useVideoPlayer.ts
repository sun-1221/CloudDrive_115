import { ref, computed } from 'vue';
import * as videoApi from '@/api/videoApi';
import { fetchWithRetry } from '@/services/temporaryResourceService';
import type {
  CloudDriveVideoPlayDto,
  CloudDriveSubtitleDto,
  CloudDriveVideoHistoryDto,
} from '@/types';

export interface VideoPlayerState {
  visible: boolean;
  fileName: string;
  pickCode: string;
  loading: boolean;
  error: string;
  status: 'Ready' | 'Transcoding' | 'Idle' | 'SchemaNotCollected';
  playUrl: string;
  currentQuality: string;
  qualityOptions: { quality: string; playUrl: string }[];
  subtitles: CloudDriveSubtitleDto[];
  subtitleSchemaStatus: 'available' | 'not_collected';
  duration: number;
  savedTime: number;
  history: CloudDriveVideoHistoryDto | null;
}

export function useVideoPlayer() {
  const state = ref<VideoPlayerState>({
    visible: false,
    fileName: '',
    pickCode: '',
    loading: false,
    error: '',
    status: 'Idle',
    playUrl: '',
    currentQuality: '',
    qualityOptions: [],
    subtitles: [],
    subtitleSchemaStatus: 'not_collected',
    duration: 0,
    savedTime: 0,
    history: null,
  });

  const hasMultipleQualities = computed(() => state.value.qualityOptions.length > 1);
  const hasSubtitles = computed(() => state.value.subtitles.length > 0);

  async function initPlayer(pickCode: string, fileName: string) {
    state.value.visible = true;
    state.value.fileName = fileName;
    state.value.pickCode = pickCode;
    state.value.loading = true;
    state.value.error = '';
    state.value.savedTime = 0;

    try {
      const result = await fetchWithRetry<CloudDriveVideoPlayDto>({
        fetchUrl: () => videoApi.getPlayUrl({ pickCode }),
        extractUrl: (data) => data.downloadUrl ?? data.qualityOptions[0]?.playUrl ?? '',
      });

      if (result.success && result.data) {
        applyPlayData(result.data);
      } else {
        state.value.error = result.error ?? '加载失败';
      }

      const [subtitles, history] = await Promise.allSettled([videoApi.getSubtitles(pickCode), videoApi.getHistory(pickCode)]);
      if (subtitles.status === 'fulfilled') {
        const value = subtitles.value as { items?: CloudDriveSubtitleDto[]; itemSchemaStatus?: 'available' | 'not_collected' } | CloudDriveSubtitleDto[];
        state.value.subtitles = Array.isArray(value) ? value : (value.items ?? []);
        state.value.subtitleSchemaStatus = Array.isArray(value) ? (state.value.subtitles.length ? 'available' : 'not_collected') : (value.itemSchemaStatus ?? 'not_collected');
      } else {
        state.value.subtitles = [];
        state.value.subtitleSchemaStatus = 'not_collected';
      }
      state.value.history = history.status === 'fulfilled' ? history.value : null;
      state.value.savedTime = state.value.history?.playedSeconds ?? state.value.history?.time ?? 0;
    } catch (e) {
      state.value.error = e instanceof Error ? e.message : '加载失败';
    } finally {
      state.value.loading = false;
    }
  }

  function applyPlayData(data: CloudDriveVideoPlayDto) {
    state.value.status = data.status;
    if (data.fileName) state.value.fileName = data.fileName;
    state.value.qualityOptions = data.qualityOptions;
    state.value.duration = data.duration ?? 0;

    if (data.status === 'Ready' && data.qualityOptions.length > 0) {
      const defaultOpt = data.qualityOptions[0]!;
      state.value.currentQuality = defaultOpt.quality;
      state.value.playUrl = defaultOpt.playUrl;
    } else if (data.status === 'Ready' && data.downloadUrl) {
      state.value.playUrl = data.downloadUrl;
    }
  }

  function switchQuality(quality: string) {
    const opt = state.value.qualityOptions.find((o) => o.quality === quality);
    if (!opt) return;

    state.value.currentQuality = quality;
    state.value.playUrl = opt.playUrl;
  }

  async function refreshStatus() {
    if (!state.value.pickCode) return;

    state.value.loading = true;
    try {
      const data = await videoApi.getPlayUrl({
        pickCode: state.value.pickCode,
      });
      applyPlayData(data);
    } catch (e) {
      state.value.error = e instanceof Error ? e.message : '刷新失败';
    } finally {
      state.value.loading = false;
    }
  }

  function retry() {
    initPlayer(state.value.pickCode, state.value.fileName);
  }

  function destroy() {
    state.value.visible = false;
    state.value.playUrl = '';
    state.value.error = '';
    state.value.status = 'Idle';
    state.value.qualityOptions = [];
    state.value.subtitles = [];
    state.value.subtitleSchemaStatus = 'not_collected';
    state.value.history = null;
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
