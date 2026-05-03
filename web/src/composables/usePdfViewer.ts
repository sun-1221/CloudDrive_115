import { ref } from 'vue';
import * as previewApi from '@/api/previewApi';
import { fetchWithRetry } from '@/services/temporaryResourceService';
import { useAuthStore } from '@/stores/authStore';
import type { CloudDriveDocPreviewDto } from '@/types';

export interface PdfViewerState {
  visible: boolean;
  fileName: string;
  fileId: string;
  loading: boolean;
  error: string;
  renderMode: 'Direct' | 'ThirdParty' | 'NotAvailable' | null;
  previewUrl: string;
  thirdPartyPreviewUrl: string;
  currentPage: number;
  totalPages: number;
  scale: number;
}

export function usePdfViewer() {
  const authStore = useAuthStore();

  const state = ref<PdfViewerState>({
    visible: false,
    fileName: '',
    fileId: '',
    loading: false,
    error: '',
    renderMode: null,
    previewUrl: '',
    thirdPartyPreviewUrl: '',
    currentPage: 1,
    totalPages: 0,
    scale: 1.0,
  });

  async function initViewer(fileId: string, fileName: string) {
    state.value.visible = true;
    state.value.fileName = fileName;
    state.value.fileId = fileId;
    state.value.loading = true;
    state.value.error = '';
    state.value.currentPage = 1;
    state.value.scale = 1.0;

    const uid = authStore.currentAccountUid;
    if (!uid) {
      state.value.error = '未授权';
      state.value.loading = false;
      return;
    }

    const result = await fetchWithRetry<CloudDriveDocPreviewDto>({
      fetchUrl: () => previewApi.getDocPreview(uid, fileId),
      extractUrl: (data) => data.previewUrl,
    });

    if (result.success && result.data) {
      state.value.renderMode = result.data.renderMode;
      state.value.previewUrl = result.data.previewUrl;
      state.value.thirdPartyPreviewUrl = result.data.thirdPartyPreviewUrl ?? '';
    } else {
      state.value.error = result.error ?? '加载失败';
    }
    state.value.loading = false;
  }

  function goToPage(page: number) {
    if (page < 1 || (state.value.totalPages > 0 && page > state.value.totalPages)) return;
    state.value.currentPage = page;
  }

  function setScale(scale: number) {
    state.value.scale = Math.max(0.25, Math.min(4, scale));
  }

  function retry() {
    initViewer(state.value.fileId, state.value.fileName);
  }

  function destroy() {
    state.value.visible = false;
    state.value.previewUrl = '';
    state.value.thirdPartyPreviewUrl = '';
    state.value.error = '';
    state.value.renderMode = null;
  }

  return {
    state,
    initViewer,
    goToPage,
    setScale,
    retry,
    destroy,
  };
}
