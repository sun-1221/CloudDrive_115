import { ref, computed } from 'vue';
import * as previewApi from '@/api/previewApi';
import { isPreviewableImage } from '@/utils/fileTypeMapping';
import { fetchWithRetry } from '@/services/temporaryResourceService';
import { useAuthStore } from '@/stores/authStore';
import type { CloudDriveFileDto } from '@/types';

export interface ImageViewerState {
  visible: boolean;
  currentIndex: number;
  imageUrl: string;
  fileName: string;
  loading: boolean;
  error: string;
  zoom: number;
  panX: number;
  panY: number;
}

export function useImageViewer() {
  const authStore = useAuthStore();

  const state = ref<ImageViewerState>({
    visible: false,
    currentIndex: 0,
    imageUrl: '',
    fileName: '',
    loading: false,
    error: '',
    zoom: 1,
    panX: 0,
    panY: 0,
  });

  const imageCollection = ref<CloudDriveFileDto[]>([]);

  const canPrev = computed(() => state.value.currentIndex > 0);
  const canNext = computed(() => state.value.currentIndex < imageCollection.value.length - 1);

  async function loadImage(index: number) {
    const file = imageCollection.value[index];
    if (!file) return;

    state.value.loading = true;
    state.value.error = '';
    state.value.fileName = file.name;
    state.value.currentIndex = index;
    resetTransform();

    const uid = authStore.currentAccountUid;
    if (!uid) {
      state.value.error = '未授权';
      state.value.loading = false;
      return;
    }

    const result = await fetchWithRetry({
      fetchUrl: () => previewApi.getImagePreview(uid, file.fileId),
      extractUrl: (data) => data.previewUrl,
    });

    if (result.success && result.url) {
      state.value.imageUrl = result.url;
    } else {
      state.value.error = result.error ?? '加载失败';
    }
    state.value.loading = false;
  }

  function open(files: CloudDriveFileDto[], startIndex: number = 0) {
    // 过滤 SVG 和非预览图片
    imageCollection.value = files.filter((f) => isPreviewableImage(f));
    if (imageCollection.value.length === 0) return;

    // 重新定位 startIndex
    const targetFile = files[startIndex];
    let adjustedIndex = 0;
    if (targetFile) {
      const found = imageCollection.value.findIndex((f) => f.fileId === targetFile.fileId);
      if (found >= 0) adjustedIndex = found;
    }

    state.value.visible = true;
    loadImage(adjustedIndex);
  }

  function next() {
    if (canNext.value) {
      loadImage(state.value.currentIndex + 1);
    }
  }

  function prev() {
    if (canPrev.value) {
      loadImage(state.value.currentIndex - 1);
    }
  }

  function zoomBy(delta: number) {
    const newZoom = Math.max(0.1, Math.min(10, state.value.zoom + delta));
    state.value.zoom = newZoom;
  }

  function resetZoom() {
    state.value.zoom = 1;
    state.value.panX = 0;
    state.value.panY = 0;
  }

  function panBy(dx: number, dy: number) {
    state.value.panX += dx;
    state.value.panY += dy;
  }

  function resetTransform() {
    state.value.zoom = 1;
    state.value.panX = 0;
    state.value.panY = 0;
  }

  function retry() {
    loadImage(state.value.currentIndex);
  }

  function close() {
    state.value.visible = false;
    state.value.imageUrl = '';
    state.value.error = '';
    imageCollection.value = [];
  }

  return {
    state,
    imageCollection,
    canPrev,
    canNext,
    open,
    next,
    prev,
    zoomBy,
    resetZoom,
    panBy,
    retry,
    close,
  };
}
