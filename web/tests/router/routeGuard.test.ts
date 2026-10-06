import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { createRouter, createMemoryHistory, type RouteRecordRaw } from 'vue-router';
import { useAuthStore } from '@/stores/authStore';

const routes: RouteRecordRaw[] = [
  {
    path: '/cloud-drive',
    component: { template: '<router-view />' },
    children: [
      { path: 'auth', name: 'AuthCallback', component: { template: '<div>Auth</div>' }, meta: { requiresAuth: false } },
      { path: 'files/:directoryId?', name: 'FileBrowser', component: { template: '<div>Files</div>' }, meta: { requiresAuth: true } },
      { path: '', redirect: { name: 'FileBrowser' } },
    ],
  },
  { path: '/', redirect: '/cloud-drive/files' },
];

function createTestRouter() {
  const router = createRouter({ history: createMemoryHistory(), routes });

  router.beforeEach(async (to) => {
    if (to.meta.requiresAuth) {
      const authStore = useAuthStore();
      if (authStore.authStatus === 'unknown') {
        await authStore.bootstrapAuthState();
      }
    }
    return true;
  });

  return router;
}

beforeEach(() => {
  setActivePinia(createPinia());
});

describe('Property 2: 路由守卫必须区分未授权与功能不可用', () => {
  it('受保护路由进入时触发 bootstrapAuthState', async () => {
    const authStore = useAuthStore();
    const spy = vi.spyOn(authStore, 'bootstrapAuthState').mockResolvedValue();

    const router = createTestRouter();
    await router.push('/cloud-drive/files');

    expect(spy).toHaveBeenCalledOnce();
  });

  it('authStatus 非 unknown 时不重复调用 bootstrapAuthState', async () => {
    const authStore = useAuthStore();
    authStore.authStatus = 'authorized';
    const spy = vi.spyOn(authStore, 'bootstrapAuthState').mockResolvedValue();

    const router = createTestRouter();
    await router.push('/cloud-drive/files');

    expect(spy).not.toHaveBeenCalled();
  });

  it('AuthCallback 路由（requiresAuth=false）不触发守卫', async () => {
    const authStore = useAuthStore();
    const spy = vi.spyOn(authStore, 'bootstrapAuthState').mockResolvedValue();

    const router = createTestRouter();
    await router.push('/cloud-drive/auth');

    expect(spy).not.toHaveBeenCalled();
  });

  it('unauthorized 与 disabled 是互斥状态', () => {
    const authStore = useAuthStore();

    authStore.authStatus = 'unauthorized';
    expect(authStore.isAuthorized).toBe(false);
    expect(authStore.featureUnavailableReason).toBeNull();

    authStore.authStatus = 'disabled';
    authStore.featureUnavailableReason = '115 网盘功能未启用';
    expect(authStore.isAuthorized).toBe(false);
    expect(authStore.authStatus).not.toBe('unauthorized');
  });

  it('handleTokenExpired 清理登录检查结果并转为 unauthorized', () => {
    const authStore = useAuthStore();
    authStore.authStatus = 'authorized';
    authStore.loginCheckResult = { isAuthenticated: true, isLoggedIn: true, lastCheckedAt: '' };

    authStore.handleTokenExpired();

    expect(authStore.authStatus).toBe('unauthorized');
    expect(authStore.loginCheckResult).toBeNull();
  });
});
