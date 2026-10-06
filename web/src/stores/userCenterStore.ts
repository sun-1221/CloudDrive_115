import { defineStore } from 'pinia';
import { ref } from 'vue';
import * as api from '@/api/userApi';
import type { CloudDriveBalanceDto, CloudDriveSpaceInfoDto, CloudDriveVipInfoDto } from '@/types';
export const useUserCenterStore = defineStore('userCenter', () => {
  const vip = ref<CloudDriveVipInfoDto | null>(null); const balance = ref<CloudDriveBalanceDto | null>(null); const storage = ref<CloudDriveSpaceInfoDto | null>(null);
  const loading = ref({ vip: false, balance: false, storage: false }); const errors = ref<{ vip?: string; balance?: string; storage?: string }>({});
  async function loadVip() { loading.value.vip = true; try { vip.value = await api.getVip(); } catch { errors.value.vip = 'VIP 信息加载失败'; } finally { loading.value.vip = false; } }
  async function loadBalance() { loading.value.balance = true; try { balance.value = await api.getBalance(); } catch { errors.value.balance = '余额加载失败'; } finally { loading.value.balance = false; } }
  async function loadStorage() { loading.value.storage = true; try { storage.value = await api.getStorage(); } catch { errors.value.storage = '空间信息加载失败'; } finally { loading.value.storage = false; } }
  async function load() { errors.value = {}; await Promise.all([loadVip(), loadBalance(), loadStorage()]); }
  return { vip, balance, storage, loading, errors, load, loadVip, loadBalance, loadStorage };
});
