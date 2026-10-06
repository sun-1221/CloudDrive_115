import { defineStore } from 'pinia';
import { ref } from 'vue';
import * as api from '@/api/offlineApi';
import type { CloudDriveOfflineQuotaDto, CloudDriveOfflineStatsDto, CloudDriveOfflineTaskListDto } from '@/types';

export const useOfflineStore = defineStore('offline', () => {
  const tasks = ref<CloudDriveOfflineTaskListDto | null>(null); const stats = ref<CloudDriveOfflineStatsDto | null>(null); const quota = ref<CloudDriveOfflineQuotaDto | null>(null);
  const loading = ref(false); const errors = ref<{ tasks?: string; stats?: string; quota?: string }>({});
  async function load() {
    loading.value = true; errors.value = {};
    const results = await Promise.allSettled([api.listTasks(), api.getStats(), api.getQuota()]);
    const [a,b,c] = results;
    if (a.status === 'fulfilled') tasks.value = a.value; else errors.value.tasks = '任务列表加载失败';
    if (b.status === 'fulfilled') stats.value = b.value; else errors.value.stats = '统计加载失败';
    if (c.status === 'fulfilled') quota.value = c.value; else errors.value.quota = '配额加载失败';
    loading.value = false;
  }
  return { tasks, stats, quota, loading, errors, load };
});
