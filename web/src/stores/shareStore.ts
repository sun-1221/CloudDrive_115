import { defineStore } from 'pinia';
import { ref } from 'vue';
import * as api from '@/api/shareApi';
import type { CloudDriveShareListDto } from '@/types';
export const useShareStore = defineStore('share', () => {
  const result = ref<CloudDriveShareListDto | null>(null); const loading = ref(false); const error = ref('');
  async function load() { loading.value = true; error.value = ''; try { result.value = await api.listMyShares(); } catch { error.value = '分享列表加载失败'; } finally { loading.value = false; } }
  function clear() { result.value = null; }
  return { result, loading, error, load, clear };
});
