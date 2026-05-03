import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { useAuthStore } from '@/stores/authStore';

// Mock authApi
const mockListAccounts = vi.fn();
const mockGetUserInfo = vi.fn();
const mockExchangeCode = vi.fn();
const mockCreatePkceSession = vi.fn();
const mockGetAuthorizationUrl = vi.fn();
const mockDisconnectApi = vi.fn();

vi.mock('@/api/authApi', () => ({
  listAccounts: (...args: unknown[]) => mockListAccounts(...args),
  getUserInfo: (...args: unknown[]) => mockGetUserInfo(...args),
  exchangeCode: (...args: unknown[]) => mockExchangeCode(...args),
  createPkceSession: (...args: unknown[]) => mockCreatePkceSession(...args),
  getAuthorizationUrl: (...args: unknown[]) => mockGetAuthorizationUrl(...args),
  disconnect: (...args: unknown[]) => mockDisconnectApi(...args),
}));

beforeEach(() => {
  setActivePinia(createPinia());
  vi.clearAllMocks();
});

describe('需求 1.2: PKCE 会话模式授权流程', () => {
  it('redirectToAuth 创建 PKCE 会话并保存 sessionId 到 sessionStorage', async () => {
    mockCreatePkceSession.mockResolvedValue({ sessionId: 'pkce-123', qrCodeContent: '', expiresAt: '' });
    mockGetAuthorizationUrl.mockResolvedValue('https://auth.115.com/authorize');

    const store = useAuthStore();

    // Mock window.location.href 赋值
    const hrefSetter = vi.fn();
    Object.defineProperty(window, 'location', {
      value: { href: '', ...window.location },
      writable: true,
    });
    Object.defineProperty(window.location, 'href', { set: hrefSetter, get: () => '' });

    // Mock crypto.randomUUID
    vi.spyOn(crypto, 'randomUUID').mockReturnValue('random-state-uuid' as `${string}-${string}-${string}-${string}-${string}`);

    const sessionSetItem = vi.spyOn(Storage.prototype, 'setItem');

    await store.redirectToAuth();

    expect(mockCreatePkceSession).toHaveBeenCalledOnce();
    expect(sessionSetItem).toHaveBeenCalledWith('pkceSessionId', 'pkce-123');
    expect(sessionSetItem).toHaveBeenCalledWith('oauthState', 'random-state-uuid');

    sessionSetItem.mockRestore();
  });
});

describe('需求 1.3: 授权回调令牌交换', () => {
  it('handleCallback 携带 code 和 pkceSessionId 完成交换', async () => {
    mockExchangeCode.mockResolvedValue({ cloudAccountUid: 'uid-001' });
    mockListAccounts.mockResolvedValue([
      { cloudAccountUid: 'uid-001', nickname: '测试', isActive: true, creationTime: '2024-01-01' },
    ]);
    mockGetUserInfo.mockResolvedValue({ cloudAccountUid: 'uid-001', nickname: '测试' });

    const store = useAuthStore();
    await store.handleCallback('auth-code-123', 'pkce-session-456');

    expect(mockExchangeCode).toHaveBeenCalledWith({
      code: 'auth-code-123',
      flowType: 'AuthorizationCodeWithPkce',
      pkceSessionId: 'pkce-session-456',
    });
    expect(store.currentAccountUid).toBe('uid-001');
    expect(store.authStatus).toBe('authorized');
  });

  it('handleCallback 无 pkceSessionId 时使用 AuthorizationCode 流程', async () => {
    mockExchangeCode.mockResolvedValue({ cloudAccountUid: 'uid-002' });
    mockListAccounts.mockResolvedValue([
      { cloudAccountUid: 'uid-002', isActive: true, creationTime: '2024-01-01' },
    ]);
    mockGetUserInfo.mockResolvedValue({ cloudAccountUid: 'uid-002', nickname: '测试2' });

    const store = useAuthStore();
    await store.handleCallback('auth-code-789');

    expect(mockExchangeCode).toHaveBeenCalledWith({
      code: 'auth-code-789',
      flowType: 'AuthorizationCode',
      pkceSessionId: undefined,
    });
  });
});

describe('需求 1.5: 断开连接', () => {
  it('disconnect 调用后端并清理前端状态', async () => {
    mockDisconnectApi.mockResolvedValue(undefined);

    const store = useAuthStore();
    store.currentAccountUid = 'uid-001';
    store.authStatus = 'authorized';

    await store.disconnect();

    expect(mockDisconnectApi).toHaveBeenCalledWith('uid-001');
    expect(store.currentAccountUid).toBeNull();
    expect(store.currentAccount).toBeNull();
    expect(store.accounts).toEqual([]);
    expect(store.authStatus).toBe('unauthorized');
  });

  it('disconnect 即使后端失败也清理前端状态', async () => {
    mockDisconnectApi.mockRejectedValue(new Error('network error'));

    const store = useAuthStore();
    store.currentAccountUid = 'uid-001';
    store.authStatus = 'authorized';

    await store.disconnect();

    expect(store.authStatus).toBe('unauthorized');
    expect(store.currentAccountUid).toBeNull();
  });
});

describe('需求 1.7: 令牌失效自动切换到授权界面', () => {
  it('handleTokenExpired 将状态设为 unauthorized 并标记需重授权', () => {
    const store = useAuthStore();
    store.authStatus = 'authorized';
    store.currentAccountUid = 'uid-1';

    store.handleTokenExpired();

    expect(store.authStatus).toBe('unauthorized');
    expect(store.requiresReauthorization).toBe(true);
    expect(store.currentAccount).toBeNull();
  });
});

describe('需求 1.8: 多账号切换', () => {
  it('switchAccount 更新当前账号 UID', async () => {
    const store = useAuthStore();
    store.currentAccountUid = 'uid-001';

    await store.switchAccount('uid-002');

    expect(store.currentAccountUid).toBe('uid-002');
  });

  it('hasMultipleAccounts 正确计算', () => {
    const store = useAuthStore();
    store.accounts = [
      { cloudAccountUid: 'uid-001', isActive: true, creationTime: '2024-01-01' },
      { cloudAccountUid: 'uid-002', isActive: false, creationTime: '2024-01-02' },
    ];

    expect(store.hasMultipleAccounts).toBe(true);
  });

  it('单账号时 hasMultipleAccounts 为 false', () => {
    const store = useAuthStore();
    store.accounts = [
      { cloudAccountUid: 'uid-001', isActive: true, creationTime: '2024-01-01' },
    ];

    expect(store.hasMultipleAccounts).toBe(false);
  });
});

describe('需求 1.9: 启动时恢复账号状态', () => {
  it('bootstrapAuthState 恢复已保存的账号', async () => {
    mockListAccounts.mockResolvedValue([
      { cloudAccountUid: 'uid-001', isActive: true, creationTime: '2024-01-01' },
      { cloudAccountUid: 'uid-002', isActive: false, creationTime: '2024-01-02' },
    ]);
    mockGetUserInfo.mockResolvedValue({ cloudAccountUid: 'uid-002', nickname: '账号2' });

    const store = useAuthStore();
    store.currentAccountUid = 'uid-002'; // 之前保存的

    await store.bootstrapAuthState();

    expect(store.currentAccountUid).toBe('uid-002');
    expect(store.authStatus).toBe('authorized');
  });

  it('bootstrapAuthState 已保存账号不存在时选择首个', async () => {
    mockListAccounts.mockResolvedValue([
      { cloudAccountUid: 'uid-003', isActive: true, creationTime: '2024-01-01' },
    ]);
    mockGetUserInfo.mockResolvedValue({ cloudAccountUid: 'uid-003', nickname: '账号3' });

    const store = useAuthStore();
    store.currentAccountUid = 'uid-not-exist';

    await store.bootstrapAuthState();

    expect(store.currentAccountUid).toBe('uid-003');
    expect(store.authStatus).toBe('authorized');
  });

  it('bootstrapAuthState 空账号列表设为 unauthorized', async () => {
    mockListAccounts.mockResolvedValue([]);

    const store = useAuthStore();
    await store.bootstrapAuthState();

    expect(store.authStatus).toBe('unauthorized');
  });

  it('bootstrapAuthState 503 错误设为 disabled', async () => {
    const axiosError = {
      isAxiosError: true,
      response: { status: 503, data: { detail: '115 网盘功能未启用' } },
    };
    // 模拟 axios.isAxiosError 返回 true
    const axios = await import('axios');
    vi.spyOn(axios.default, 'isAxiosError').mockReturnValue(true);
    mockListAccounts.mockRejectedValue(axiosError);

    const store = useAuthStore();
    await store.bootstrapAuthState();

    expect(store.authStatus).toBe('disabled');
    expect(store.featureUnavailableReason).toBe('115 网盘功能未启用');

    vi.restoreAllMocks();
  });
});

describe('需求 1.10: 功能不可用状态', () => {
  it('disabled 状态下 isAuthorized 为 false', () => {
    const store = useAuthStore();
    store.authStatus = 'disabled';

    expect(store.isAuthorized).toBe(false);
  });
});
