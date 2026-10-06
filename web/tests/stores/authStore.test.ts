import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { useAuthStore } from '@/stores/authStore';

const mockCheckLoginStatus = vi.fn();
const mockGetCurrentUser = vi.fn();
vi.mock('@/api/authApi', () => ({
  checkLoginStatus: (...args: unknown[]) => mockCheckLoginStatus(...args),
  getCurrentUser: (...args: unknown[]) => mockGetCurrentUser(...args),
}));

beforeEach(() => {
  setActivePinia(createPinia());
  vi.clearAllMocks();
  localStorage?.clear();
});

const checked = (isAuthenticated: boolean, isLoggedIn: boolean | null) => ({
  isAuthenticated,
  isLoggedIn,
  lastCheckedAt: '2026-10-05T00:00:00Z',
});

describe('登录态只读检查', () => {
  it('已认证且上游已登录时进入 authorized', async () => {
    mockCheckLoginStatus.mockResolvedValue(checked(true, true));
    const store = useAuthStore();

    await store.bootstrapAuthState();

    expect(mockCheckLoginStatus).toHaveBeenCalledOnce();
    expect(store.authStatus).toBe('authorized');
    expect(store.isAuthenticated).toBe(true);
    expect(store.loading).toBe(false);
  });

  it.each([[false, null], [true, false], [true, null]] as const)(
    '认证=%s、上游登录=%s 时保持 unauthorized',
    async (isAuthenticated, isLoggedIn) => {
      mockCheckLoginStatus.mockResolvedValue(checked(isAuthenticated, isLoggedIn));
      const store = useAuthStore();
      await store.bootstrapAuthState();
      expect(store.authStatus).toBe('unauthorized');
    },
  );

  it('503 响应进入 disabled 并保存可展示原因', async () => {
    mockCheckLoginStatus.mockRejectedValue({
      isAxiosError: true,
      response: { data: { status: 503, detail: '115 网盘功能未启用' } },
    });
    const store = useAuthStore();
    await store.bootstrapAuthState();
    expect(store.authStatus).toBe('disabled');
    expect(store.featureUnavailableReason).toBe('115 网盘功能未启用');
  });

  it('401 或普通失败进入 unauthorized', async () => {
    mockCheckLoginStatus.mockRejectedValue({ isAxiosError: true, response: { data: { status: 401 } } });
    const store = useAuthStore();
    await store.bootstrapAuthState();
    expect(store.authStatus).toBe('unauthorized');
    expect(store.featureUnavailableReason).toBeNull();
  });

  it('保留冷却时间并过滤敏感失败原因', async () => {
    mockCheckLoginStatus.mockResolvedValue({ ...checked(true, false), cooldownUntil: '2026-10-05T01:00:00Z', failureReason: 'cookie=secret' });
    const store = useAuthStore();
    await store.bootstrapAuthState();
    expect(store.cooldownUntil).toBe('2026-10-05T01:00:00Z');
    expect(store.failureReason).toBeNull();
  });
});

describe('OAuth 和多账号能力已移除', () => {
  it('不暴露 OAuth、断连与账号切换动作', () => {
    const store = useAuthStore();
    for (const action of ['redirectToAuth', 'handleCallback', 'disconnect', 'switchAccount']) {
      expect(store).not.toHaveProperty(action);
    }
  });

  it('会话失效清理检查结果并进入 unauthorized', () => {
    const store = useAuthStore();
    store.authStatus = 'authorized';
    store.loginCheckResult = checked(true, true);
    store.handleTokenExpired();
    expect(store.authStatus).toBe('unauthorized');
    expect(store.loginCheckResult).toBeNull();
    expect(store.isAuthenticated).toBe(false);
  });
});
