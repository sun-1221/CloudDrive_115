import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { useAuthStore } from '@/stores/authStore';

const mockCheckLoginStatus = vi.fn();
const mockGetCurrentUser = vi.fn();
const mockLogin = vi.fn();
const mockRegister = vi.fn();
const mockRefresh = vi.fn();
const mockLogout = vi.fn();
const mockGetAccounts = vi.fn();
const mockBindAccount = vi.fn();
const mockSelectAccount = vi.fn();
const mockUnbindAccount = vi.fn();
const storage = new Map<string, string>();
const localStorageMock = {
  getItem: (key: string) => storage.get(key) ?? null,
  setItem: (key: string, value: string) => storage.set(key, value),
  removeItem: (key: string) => storage.delete(key),
  clear: () => storage.clear(),
};
vi.mock('@/api/authApi', () => ({
  checkLoginStatus: (...args: unknown[]) => mockCheckLoginStatus(...args),
  getCurrentUser: (...args: unknown[]) => mockGetCurrentUser(...args),
  login: (...args: unknown[]) => mockLogin(...args),
  register: (...args: unknown[]) => mockRegister(...args),
  refresh: (...args: unknown[]) => mockRefresh(...args),
  logout: (...args: unknown[]) => mockLogout(...args),
}));
vi.mock('@/api/accountApi', () => ({
  getAccounts: (...args: unknown[]) => mockGetAccounts(...args),
  bindAccount: (...args: unknown[]) => mockBindAccount(...args),
  selectAccount: (...args: unknown[]) => mockSelectAccount(...args),
  unbindAccount: (...args: unknown[]) => mockUnbindAccount(...args),
}));

beforeEach(() => {
  setActivePinia(createPinia());
  vi.clearAllMocks();
  vi.stubGlobal('localStorage', localStorageMock);
  localStorage.clear();
});

const checked = (isAuthenticated: boolean, isLoggedIn: boolean | null) => ({
  isAuthenticated,
  isLoggedIn,
  lastCheckedAt: '2026-10-05T00:00:00Z',
});

describe('登录态只读检查', () => {
  it('本地 JWT 有效时读取当前用户并进入 authorized', async () => {
    localStorage.setItem('115web.accessToken', 'header.payload.signature');
    mockGetCurrentUser.mockResolvedValue({ userId: 'user-1', userName: 'tester', currentCloudAccountUid: null, accounts: [] });
    const store = useAuthStore();

    await store.bootstrapAuthState();

    expect(mockCheckLoginStatus).not.toHaveBeenCalled();
    expect(mockGetCurrentUser).toHaveBeenCalledOnce();
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

describe('JWT 会话与多账号上下文', () => {
  it('注册或登录成功后保存 JWT 和过期时间并读取当前用户', async () => {
    mockLogin.mockResolvedValue({ accessToken: 'header.payload.signature', tokenType: 'Bearer', expiresAt: '2026-10-06T00:00:00Z', userId: 'user-1' });
    mockGetCurrentUser.mockResolvedValue({ userId: 'user-1', userName: 'tester', currentCloudAccountUid: null, accounts: [] });
    const store = useAuthStore();

    await store.signIn('tester', 'password');

    expect(mockLogin).toHaveBeenCalledWith('tester', 'password');
    expect(localStorage.getItem('115web.accessToken')).toBe('header.payload.signature');
    expect(store.expiresAt).toBe('2026-10-06T00:00:00Z');
    expect(store.currentUser?.userId).toBe('user-1');
  });

  it('刷新会话后更新 token 并支持退出清理', async () => {
    localStorage.setItem('115web.accessToken', 'header.payload.signature');
    mockRefresh.mockResolvedValue({ accessToken: 'header.payload.next', tokenType: 'Bearer', expiresAt: '2026-10-07T00:00:00Z', userId: 'user-1' });
    mockGetCurrentUser.mockResolvedValue({ userId: 'user-1', userName: 'tester', currentCloudAccountUid: null, accounts: [] });
    mockLogout.mockResolvedValue(undefined);
    const store = useAuthStore();

    await store.refreshSession();
    expect(localStorage.getItem('115web.accessToken')).toBe('header.payload.next');
    await store.signOut();
    expect(localStorage.getItem('115web.accessToken')).toBeNull();
    expect(store.authStatus).toBe('unauthorized');
  });

  it('账号列表、绑定、选择和解绑均通过 Store 同步当前用户', async () => {
    localStorage.setItem('115web.accessToken', 'header.payload.signature');
    mockGetCurrentUser
      .mockResolvedValueOnce({ userId: 'user-1', userName: 'tester', currentCloudAccountUid: null, accounts: [] })
      .mockResolvedValueOnce({ userId: 'user-1', userName: 'tester', currentCloudAccountUid: 'account-1', accounts: [{ cloudAccountUid: 'account-1', status: 'Active', creationTime: '2026-10-01T00:00:00Z' }] })
      .mockResolvedValueOnce({ userId: 'user-1', userName: 'tester', currentCloudAccountUid: 'account-2', accounts: [{ cloudAccountUid: 'account-2', status: 'Active', creationTime: '2026-10-01T00:00:00Z' }] });
    mockGetAccounts.mockResolvedValue([{ cloudAccountUid: 'account-1', status: 'Active', creationTime: '2026-10-01T00:00:00Z' }]);
    mockBindAccount.mockResolvedValue(undefined);
    mockSelectAccount.mockResolvedValue(undefined);
    mockUnbindAccount.mockResolvedValue(undefined);
    mockCheckLoginStatus.mockResolvedValue(checked(true, true));
    const store = useAuthStore();
    await store.bootstrapAuthState();
    await store.loadAccounts();
    await store.bindAccount({ cloudAccountUid: 'account-1', uid: 'u', cid: 'c', ussid: 's' });
    await store.selectAccount('account-2');
    await store.unbindAccount('account-1');

    expect(mockGetAccounts).toHaveBeenCalledOnce();
    expect(mockBindAccount).toHaveBeenCalledWith({ cloudAccountUid: 'account-1', uid: 'u', cid: 'c', ussid: 's' });
    expect(mockSelectAccount).toHaveBeenCalledWith('account-2');
    expect(mockUnbindAccount).toHaveBeenCalledWith('account-1');
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
