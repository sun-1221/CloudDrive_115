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
  if (typeof localStorage !== 'undefined') localStorage.removeItem('115web.accessToken');
}

export const useAuthStore = defineStore('auth', () => {
  const authStatus = ref<'unknown' | 'unauthorized' | 'authorized' | 'disabled'>('unknown');
  const featureUnavailableReason = ref<string | null>(null);
  const loading = ref(false);
  const loginCheckResult = ref<CloudDriveAuthCheckDto | null>(null);
  const currentUser = ref<CurrentUserDto | null>(null);

  const isAuthorized = computed(() => authStatus.value === 'authorized');
  const isAuthenticated = computed(() => authStatus.value === 'authorized');
  const failureReason = computed(() => loginCheckResult.value?.failureReason ?? null);
  const cooldownUntil = computed(() => loginCheckResult.value?.cooldownUntil ?? null);

  setTokenExpiredHandler(() => {
    handleTokenExpired();
  });

  async function bootstrapAuthState(): Promise<void> {
    loading.value = true;
    try {
      const { checkLoginStatus, getCurrentUser } = await import('@/api/authApi');
      if (getAccessToken()) {
        authStatus.value = 'authorized';
        currentUser.value = await getCurrentUser();
      } else {
        // `/auth/check` only probes the upstream 115 Cookie session. It is not
        // the local JWT session and must never authorize protected pages.
        const result = await checkLoginStatus();
        loginCheckResult.value = { ...result, failureReason: safeFailureReason(result.failureReason) ?? undefined };
        authStatus.value = 'unauthorized';
      }
    } catch (e: unknown) {
      const problem = parseProblemDetails(e);
      if (problem?.code === 'cloud_drive_api_not_collected' || problem?.status === 503) {
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
    clearAccessToken();
  }

  async function signIn(userName: string, password: string, isRegister = false): Promise<void> {
    const { login, register, getCurrentUser } = await import('@/api/authApi');
    const response: AuthResponseDto = isRegister ? await register(userName, password) : await login(userName, password);
    if (typeof localStorage !== 'undefined') localStorage.setItem('115web.accessToken', response.accessToken);
    authStatus.value = 'authorized';
    currentUser.value = await getCurrentUser();
  }

  async function refreshSession(): Promise<void> {
    const { refresh } = await import('@/api/authApi');
    const response = await refresh();
    if (typeof localStorage !== 'undefined') localStorage.setItem('115web.accessToken', response.accessToken);
    currentUser.value = await (await import('@/api/authApi')).getCurrentUser();
    authStatus.value = 'authorized';
  }

  async function signOut(): Promise<void> {
    try { await (await import('@/api/authApi')).logout(); } finally { handleTokenExpired(); }
  }

  async function selectAccount(cloudAccountUid: string): Promise<void> {
    await (await import('@/api/accountApi')).selectAccount(cloudAccountUid);
    if (currentUser.value) currentUser.value.currentCloudAccountUid = cloudAccountUid;
  }

  return {
    authStatus,
    featureUnavailableReason,
    loading,
    loginCheckResult,
    currentUser,
    isAuthorized,
    isAuthenticated,
    failureReason,
    cooldownUntil,
    bootstrapAuthState,
    handleTokenExpired,
    signIn,
    signOut,
    selectAccount,
    refreshSession,
  };
});
