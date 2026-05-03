import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import axios from 'axios';
import type {
  CloudDriveAccountSummaryDto,
  CloudDriveUserInfoDto,
  ApiProblemDetails,
} from '@/types';

function isServiceUnavailableError(e: unknown): boolean {
  return axios.isAxiosError(e) && e.response?.status === 503;
}

function extractDetailFromError(e: unknown): string {
  if (axios.isAxiosError(e) && e.response?.data) {
    const data = e.response.data as Record<string, unknown>;
    if (typeof data.detail === 'string') return data.detail;
  }
  return '功能不可用';
}

export const useAuthStore = defineStore('auth', () => {
  const currentAccountUid = ref<string | null>(null);
  const accounts = ref<CloudDriveAccountSummaryDto[]>([]);
  const authStatus = ref<'unknown' | 'unauthorized' | 'authorized' | 'disabled'>('unknown');
  const requiresReauthorization = ref(false);
  const featureUnavailableReason = ref<string | null>(null);
  const loading = ref(false);
  const currentAccount = ref<CloudDriveUserInfoDto | null>(null);

  const isAuthorized = computed(() => authStatus.value === 'authorized');
  const hasMultipleAccounts = computed(() => accounts.value.length > 1);

  async function redirectToAuth(): Promise<void> {
    loading.value = true;
    try {
      const { createPkceSession, getAuthorizationUrl } = await import('@/api/authApi');

      // 1. 创建 PKCE 会话
      const pkceSession = await createPkceSession();
      sessionStorage.setItem('pkceSessionId', pkceSession.sessionId);

      // 2. 生成随机 state 防止 CSRF
      const state = crypto.randomUUID();
      sessionStorage.setItem('oauthState', state);

      // 3. 获取授权 URL 并重定向
      const authUrl = await getAuthorizationUrl();
      const separator = authUrl.includes('?') ? '&' : '?';
      window.location.href = `${authUrl}${separator}state=${encodeURIComponent(state)}`;
    } finally {
      loading.value = false;
    }
  }

  async function handleCallback(code: string, pkceSessionId?: string): Promise<void> {
    loading.value = true;
    try {
      const { exchangeCode } = await import('@/api/authApi');
      const result = await exchangeCode({
        code,
        flowType: pkceSessionId ? 'AuthorizationCodeWithPkce' : 'AuthorizationCode',
        pkceSessionId: pkceSessionId ?? undefined,
      });

      currentAccountUid.value = result.cloudAccountUid;
      await bootstrapAuthState();
    } finally {
      loading.value = false;
    }
  }

  async function bootstrapAuthState(): Promise<void> {
    loading.value = true;
    try {
      const { listAccounts } = await import('@/api/authApi');
      const accountList = await listAccounts();
      accounts.value = accountList;

      if (accountList.length === 0) {
        authStatus.value = 'unauthorized';
        return;
      }

      // 基于已保存的 currentAccountUid 或默认账号或首个账号
      const savedUid = currentAccountUid.value;
      const matched = savedUid ? accountList.find(a => a.cloudAccountUid === savedUid) : null;
      const defaultAccount = accountList.find(a => a.isActive) ?? accountList[0];
      const selected = matched ?? defaultAccount;

      if (selected) {
        currentAccountUid.value = selected.cloudAccountUid;
        await fetchUserInfo();
        authStatus.value = 'authorized';
      } else {
        authStatus.value = 'unauthorized';
      }
    } catch (e: unknown) {
      // 503 → 功能不可用
      if (isServiceUnavailableError(e)) {
        authStatus.value = 'disabled';
        featureUnavailableReason.value = extractDetailFromError(e);
      } else {
        authStatus.value = 'unauthorized';
      }
    } finally {
      loading.value = false;
    }
  }

  async function fetchUserInfo(): Promise<void> {
    if (!currentAccountUid.value) return;
    try {
      const { getUserInfo } = await import('@/api/authApi');
      const info = await getUserInfo(currentAccountUid.value);
      currentAccount.value = info;
    } catch {
      // 用户信息获取失败不阻塞流程
      currentAccount.value = null;
    }
  }

  async function switchAccount(_cloudAccountUid: string): Promise<void> {
    // 将在 AuthStore 完善任务中实现
    // 顺序：更新 currentAccountUid → 拉取账号信息 → 清空搜索 → 刷新目录 → 刷新任务 → 关闭预览
    currentAccountUid.value = _cloudAccountUid;
  }

  async function disconnect(): Promise<void> {
    if (currentAccountUid.value) {
      try {
        const { disconnect: disconnectApi } = await import('@/api/authApi');
        await disconnectApi(currentAccountUid.value);
      } catch {
        // 断开连接失败不阻塞前端状态清理
      }
    }
    currentAccountUid.value = null;
    currentAccount.value = null;
    accounts.value = [];
    authStatus.value = 'unauthorized';
    requiresReauthorization.value = false;
  }

  function handleTokenExpired(_problem?: ApiProblemDetails): void {
    // 将在 AuthStore 完善任务中实现
    // 清理当前账号上下文，展示 Toast "授权已过期，请重新连接"
    // 自动切换到授权连接界面
    authStatus.value = 'unauthorized';
    currentAccount.value = null;
    requiresReauthorization.value = true;
  }

  return {
    currentAccountUid,
    accounts,
    authStatus,
    requiresReauthorization,
    featureUnavailableReason,
    loading,
    currentAccount,
    isAuthorized,
    hasMultipleAccounts,
    redirectToAuth,
    handleCallback,
    bootstrapAuthState,
    fetchUserInfo,
    switchAccount,
    disconnect,
    handleTokenExpired,
  };
});
