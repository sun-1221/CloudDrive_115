import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

// Mock authStore
vi.mock('@/stores/authStore', () => ({
  useAuthStore: () => ({
    currentAccountUid: 'test-uid',
  }),
}));

// Mock videoApi
const mockGetPlayUrl = vi.fn();
const mockGetSubtitles = vi.fn();
const mockGetHistory = vi.fn();
const mockSubmitTranscode = vi.fn();
vi.mock('@/api/videoApi', () => ({
  getPlayUrl: (...args: unknown[]) => mockGetPlayUrl(...args),
  getSubtitles: (...args: unknown[]) => mockGetSubtitles(...args),
  getHistory: (...args: unknown[]) => mockGetHistory(...args),
  submitTranscode: (...args: unknown[]) => mockSubmitTranscode(...args),
}));

// Mock temporaryResourceService
vi.mock('@/services/temporaryResourceService', () => ({
  fetchWithRetry: vi.fn(async ({ fetchUrl, extractUrl }: { fetchUrl: () => Promise<unknown>; extractUrl: (data: unknown) => string }) => {
    try {
      const data = await fetchUrl();
      return { success: true, url: extractUrl(data), data };
    } catch {
      try {
        const data2 = await fetchUrl();
        return { success: true, url: extractUrl(data2), data: data2 };
      } catch (e2: unknown) {
        return { success: false, error: e2 instanceof Error ? e2.message : '资源加载失败' };
      }
    }
  }),
}));

import { useVideoPlayer } from '@/composables/useVideoPlayer';

beforeEach(() => {
  setActivePinia(createPinia());
  vi.clearAllMocks();
  mockGetHistory.mockResolvedValue(null);
});

describe('需求 8.1: 视频播放器打开', () => {
  it('initPlayer 加载播放数据并设置 visible', async () => {
    mockGetPlayUrl.mockResolvedValue({
      status: 'Ready',
      qualityOptions: [{ quality: '720p', playUrl: 'https://example.com/720p.m3u8' }],
      duration: 120,
      isTemporary: true,
    });
    mockGetSubtitles.mockResolvedValue([]);

    const { state, initPlayer } = useVideoPlayer();
    await initPlayer('file-1', '测试视频.mp4');

    expect(state.value.visible).toBe(true);
    expect(state.value.status).toBe('Ready');
    expect(state.value.playUrl).toBe('https://example.com/720p.m3u8');
    expect(state.value.duration).toBe(120);
    expect(state.value.loading).toBe(false);
  });
});

describe('需求 8.4-8.5: 清晰度切换', () => {
  it('switchQuality 切换清晰度并保留进度', async () => {
    mockGetPlayUrl.mockResolvedValue({
      status: 'Ready',
      qualityOptions: [
        { quality: '720p', playUrl: 'https://example.com/720p.m3u8' },
        { quality: '1080p', playUrl: 'https://example.com/1080p.m3u8' },
      ],
      duration: 300,
      isTemporary: true,
    });
    mockGetSubtitles.mockResolvedValue([]);

    const { state, initPlayer, switchQuality, hasMultipleQualities } = useVideoPlayer();
    await initPlayer('file-1', '视频.mp4');

    expect(hasMultipleQualities.value).toBe(true);
    expect(state.value.currentQuality).toBe('720p');

    await switchQuality('1080p');

    expect(state.value.currentQuality).toBe('1080p');
    expect(state.value.playUrl).toBe('https://example.com/1080p.m3u8');
  });

  it('switchQuality 不存在的清晰度无效果', async () => {
    mockGetPlayUrl.mockResolvedValue({
      status: 'Ready',
      qualityOptions: [{ quality: '720p', playUrl: 'https://example.com/720p.m3u8' }],
      duration: 60,
      isTemporary: true,
    });
    mockGetSubtitles.mockResolvedValue([]);

    const { state, initPlayer, switchQuality } = useVideoPlayer();
    await initPlayer('file-1', '视频.mp4');

    await switchQuality('4K');

    expect(state.value.currentQuality).toBe('720p');
  });
});

describe('需求 8.6-8.7: 字幕', () => {
  it('有字幕时 hasSubtitles 为 true', async () => {
    mockGetPlayUrl.mockResolvedValue({
      status: 'Ready',
      qualityOptions: [{ quality: '720p', playUrl: 'https://example.com/720p.m3u8' }],
      duration: 60,
      isTemporary: true,
    });
    mockGetSubtitles.mockResolvedValue([
      { subtitleId: 's1', language: '中文', subtitleUrl: 'https://example.com/zh.vtt' },
    ]);

    const { hasSubtitles, initPlayer } = useVideoPlayer();
    await initPlayer('file-1', '视频.mp4');

    expect(hasSubtitles.value).toBe(true);
  });

  it('无字幕时 hasSubtitles 为 false', async () => {
    mockGetPlayUrl.mockResolvedValue({
      status: 'Ready',
      qualityOptions: [{ quality: '720p', playUrl: 'https://example.com/720p.m3u8' }],
      duration: 60,
      isTemporary: true,
    });
    mockGetSubtitles.mockResolvedValue([]);

    const { hasSubtitles, initPlayer } = useVideoPlayer();
    await initPlayer('file-1', '视频.mp4');

    expect(hasSubtitles.value).toBe(false);
  });

  it('字幕加载失败不阻塞播放', async () => {
    mockGetPlayUrl.mockResolvedValue({
      status: 'Ready',
      qualityOptions: [{ quality: '720p', playUrl: 'https://example.com/720p.m3u8' }],
      duration: 60,
      isTemporary: true,
    });
    mockGetSubtitles.mockRejectedValue(new Error('字幕加载失败'));

    const { state, initPlayer } = useVideoPlayer();
    await initPlayer('file-1', '视频.mp4');

    expect(state.value.subtitles).toEqual([]);
    expect(state.value.error).toBe('');
    expect(state.value.playUrl).toBeTruthy();
  });
});

describe('需求 8.8: 转码状态', () => {
  it('Transcoding 状态仅展示状态且不创建转码任务', async () => {
    mockGetPlayUrl.mockResolvedValue({
      status: 'Transcoding',
      qualityOptions: [],
      duration: 0,
      isTemporary: false,
    });
    mockGetSubtitles.mockResolvedValue([]);

    const { state, initPlayer } = useVideoPlayer();
    await initPlayer('file-1', '视频.mp4');

    expect(state.value.status).toBe('Transcoding');
    expect(state.value.playUrl).toBe('');
    expect(mockSubmitTranscode).not.toHaveBeenCalled();
  });
});

describe('需求 8.9: 关闭播放器', () => {
  it('destroy 重置所有状态', async () => {
    mockGetPlayUrl.mockResolvedValue({
      status: 'Ready',
      qualityOptions: [{ quality: '720p', playUrl: 'https://example.com/720p.m3u8' }],
      duration: 60,
      isTemporary: true,
    });
    mockGetSubtitles.mockResolvedValue([]);

    const { state, initPlayer, destroy } = useVideoPlayer();
    await initPlayer('file-1', '视频.mp4');

    destroy();

    expect(state.value.visible).toBe(false);
    expect(state.value.playUrl).toBe('');
    expect(state.value.qualityOptions).toEqual([]);
  });
});

describe('需求 8.11-8.12: 播放地址失败与重试', () => {
  it('加载失败设置 error', async () => {
    mockGetPlayUrl.mockRejectedValue(new Error('服务器错误'));
    mockGetSubtitles.mockResolvedValue([]);

    const { state, initPlayer } = useVideoPlayer();
    await initPlayer('file-1', '视频.mp4');

    expect(state.value.error).toBeTruthy();
  });

  it('retry 重新初始化播放器', async () => {
    mockGetPlayUrl
      .mockRejectedValueOnce(new Error('过期'))
      .mockRejectedValueOnce(new Error('过期'))
      .mockResolvedValue({
        status: 'Ready',
        qualityOptions: [{ quality: '720p', playUrl: 'https://example.com/new.m3u8' }],
        duration: 60,
        isTemporary: true,
      });
    mockGetSubtitles.mockResolvedValue([]);

    const { state, initPlayer, retry } = useVideoPlayer();
    await initPlayer('file-1', '视频.mp4');
    expect(state.value.error).toBeTruthy();

    retry();

    await vi.waitFor(() => expect(state.value.playUrl).toBe('https://example.com/new.m3u8'));
  });

  it('refreshStatus 刷新转码状态', async () => {
    mockGetPlayUrl
      .mockResolvedValueOnce({
        status: 'Transcoding', qualityOptions: [], duration: 0, isTemporary: false, jobId: 'j1',
      })
      .mockResolvedValueOnce({
        status: 'Ready',
        qualityOptions: [{ quality: '720p', playUrl: 'https://example.com/720p.m3u8' }],
        duration: 120,
        isTemporary: true,
      });
    mockGetSubtitles.mockResolvedValue([]);

    const { state, initPlayer, refreshStatus } = useVideoPlayer();
    await initPlayer('file-1', '视频.mp4');
    expect(state.value.status).toBe('Transcoding');

    await refreshStatus();
    expect(state.value.status).toBe('Ready');
  });
});
