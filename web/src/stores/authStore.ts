import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import type { AuthResponseDto, CloudDriveAuthCheckDto, CurrentUserDto } from '@/types';
import { parseProblemDetails } from '@/api/problemDetails';
import { setTokenExpiredHandler } from '@/api/http';

function safeFailureReason(reason: string | undefined): string | null {
  if (!reason || /(cookie|token|signature|pick.?code|https?:\/\/)/i.test(reason)) return null;
  return reason.slice(0, 160);
}

function getAccessToken(): string | null {
  if (typeof localStorage === 'undefined') return null;
  try {
    const token = localStorage.getItem('115web.accessToken');
    return token && token.split('.').length === 3 ? token : null;
  } catch {
    return null;
  }
}

function clearAccessToken(): void {
  if (typeof localStorage !== 'undefined') {
    localStorage.removeItem('115web.accessToken');
    localStorage.removeItem('115web.expiresAt');
  }
}

function readExpiresAt(): string | null {
  if (typeof localStorage === 'undefined') return null;
  try {
    const value = localStorage.getItem('115web.expiresAt');
    return value && !Number.isNaN(Date.parse(value)) ? value : null;
  } catch {
    return null;
  }
}

export const useAuthStore = defineStore('auth', () => {
  const authStatus = ref<'unknown' | 'unauthorized' | 'authorized' | 'disabled'>('unknown');
  const featureUnavailableReason = ref<string | null>(null);
  const loading = ref(false);
  const accountLoading = ref(false);
  const expiresAt = ref<string | null>(readExpiresAt());
  const loginCheckResult = ref<CloudDriveAuthCheckDto | null>(null);
  const currentUser = ref<CurrentUserDto | null>(null);

  const isAuthorized = computed(() => authStatus.value === 'authorized');
  const isAuthenticated = computed(() => authStatus.value === 'authorized');
  const failureReason = computed(() => loginCheckResult.value?.failureReason ?? null);
  const cooldownUntil = computed(() => loginCheckResult.value?.cooldownUntil ?? null);

  setTokenExpiredHandler(() => {
    handleTokenExpired();
  });

  async function loadCurrentUser(): Promise<CurrentUserDto> {
    const { getCurrentUser } = await import('@/api/authApi');
    const user = await getCurrentUser();
    currentUser.value = user;
    return user;
  }

  async function refreshUpstreamStatus(): Promise<void> {
    const { checkLoginStatus } = await import('@/api/authApi');
    if (!currentUser.value?.accounts.length) {
      loginCheckResult.value = null;
      return;
    }
    try {
      const result = await checkLoginStatus();
      loginCheckResult.value = {
        ...result,
        failureReason: safeFailureReason(result.failureReason) ?? undefined,
      };
    } catch (error: unknown) {
      const problem = parseProblemDetails(error);
      if (problem?.code === 'cloud_drive_api_not_collected' || problem?.status === 503) {
        featureUnavailableReason.value = problem.detail || '115 网盘功能暂不可用';
        loginCheckResult.value = {
          isAuthenticated: false,
          isLoggedIn: false,
          lastCheckedAt: new Date().toISOString(),
          failureReason: safeFailureReason(problem.detail) ?? undefined,
        };
      } else {
        loginCheckResult.value = null;
      }
    }
  }

  function persistAuthResponse(response: AuthResponseDto): void {
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('115web.accessToken', response.accessToken);
      localStorage.setItem('115web.expiresAt', response.expiresAt);
    }
    expiresAt.value = response.expiresAt;
  }

  async function loadAccounts(): Promise<void> {
    if (!getAccessToken()) return;
    accountLoading.value = true;
    try {
      const { getAccounts } = await import('@/api/accountApi');
      const accounts = await getAccounts();
      if (currentUser.value) {
        currentUser.value = { ...currentUser.value, accounts };
      }
    } catch (error: unknown) {
      if (parseProblemDetails(error)?.status === 401) handleTokenExpired();
      throw error;
    } finally {
      accountLoading.value = false;
    }
  }

  async function bootstrapAuthState(): Promise<void> {
    loading.value = true;
    featureUnavailableReason.value = null;
    try {
      const { checkLoginStatus, getCurrentUser } = await import('@/api/authApi');
      const token = getAccessToken();
      if (token) {
        authStatus.value = 'authorized';
        currentUser.value = await getCurrentUser();
        // 本地 JWT 与上游 Cookie 会话是两个独立状态。只有存在绑定账号时
        // 才探测上游登录态，避免匿名启动时制造无意义的请求。
        await refreshUpstreamStatus();
      } else {
        // `/auth/check` only probes the upstream 115 Cookie session. It is not
        // the local JWT session and must never authorize protected pages.
        const result = await checkLoginStatus();
        loginCheckResult.value = { ...result, failureReason: safeFailureReason(result.failureReason) ?? undefined };
        authStatus.value = 'unauthorized';
      }
    } catch (e: unknown) {
      const problem = parseProblemDetails(e);
      if (problem?.status === 401) {
        handleTokenExpired();
      } else if (problem?.code === 'cloud_drive_api_not_collected' || problem?.status === 503) {
        authStatus.value = 'disabled';
        featureUnavailableReason.value = problem.detail || '115 网盘功能暂不可用';
      } else {
        authStatus.value = 'unauthorized';
        featureUnavailableReason.value = null;
      }
    } finally {
      loading.value = false;
    }
  }

  function handleTokenExpired(): void {
    authStatus.value = 'unauthorized';
    loginCheckResult.value = null;
    currentUser.value = null;
    expiresAt.value = null;
    clearAccessToken();
  }

  async function signIn(userName: string, password: string, isRegister = false): Promise<void> {
    loading.value = true;
    try {
      const { login, register } = await import('@/api/authApi');
      const response: AuthResponseDto = isRegister ? await register(userName, password) : await login(userName, password);
      persistAuthResponse(response);
      authStatus.value = 'authorized';
      await loadCurrentUser();
      await refreshUpstreamStatus();
    } catch (error: unknown) {
      const problem = parseProblemDetails(error);
      if (problem?.status === 401) handleTokenExpired();
      throw error;
    } finally {
      loading.value = false;
    }
  }

  async function refreshSession(): Promise<void> {
    loading.value = true;
    try {
      const { refresh } = await import('@/api/authApi');
      const response = await refresh();
      persistAuthResponse(response);
      await loadCurrentUser();
      await refreshUpstreamStatus();
      authStatus.value = 'authorized';
    } catch (error: unknown) {
      const problem = parseProblemDetails(error);
      if (problem?.status === 401) handleTokenExpired();
      throw error;
    } finally {
      loading.value = false;
    }
  }

  async function signOut(): Promise<void> {
    loading.value = true;
    try { await (await import('@/api/authApi')).logout(); } finally {
      handleTokenExpired();
      loading.value = false;
    }
  }

  async function selectAccount(cloudAccountUid: string): Promise<void> {
    accountLoading.value = true;
    try {
      await (await import('@/api/accountApi')).selectAccount(cloudAccountUid);
      if (currentUser.value) currentUser.value = { ...currentUser.value, currentCloudAccountUid: cloudAccountUid };
      await refreshUpstreamStatus();
    } catch (error: unknown) {
      if (parseProblemDetails(error)?.status === 401) handleTokenExpired();
      throw error;
    } finally {
      accountLoading.value = false;
    }
  }

  async function bindAccount(input: { cloudAccountUid: string; uid: string; cid: string; ussid: string }): Promise<void> {
    accountLoading.value = true;
    try {
      await (await import('@/api/accountApi')).bindAccount(input);
      await loadCurrentUser();
      await refreshUpstreamStatus();
    } catch (error: unknown) {
      if (parseProblemDetails(error)?.status === 401) handleTokenExpired();
      throw error;
    } finally {
      accountLoading.value = false;
    }
  }

  async function unbindAccount(cloudAccountUid: string): Promise<void> {
    accountLoading.value = true;
    try {
      await (await import('@/api/accountApi')).unbindAccount(cloudAccountUid);
      await loadCurrentUser();
      await refreshUpstreamStatus();
    } catch (error: unknown) {
      if (parseProblemDetails(error)?.status === 401) handleTokenExpired();
      throw error;
    } finally {
      accountLoading.value = false;
    }
  }

  return {
    authStatus,
    featureUnavailableReason,
    loading,
    accountLoading,
    expiresAt,
    loginCheckResult,
    currentUser,
    isAuthorized,
    isAuthenticated,
    failureReason,
    cooldownUntil,
    bootstrapAuthState,
    loadAccounts,
    handleTokenExpired,
    signIn,
    signOut,
    selectAccount,
    bindAccount,
    unbindAccount,
    refreshSession,
  };
});
