import { describe, it, expect, beforeEach, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { useJobStore } from '@/stores/jobStore';

vi.mock('@/api/jobApi', () => ({
  getJobList: vi.fn().mockResolvedValue({ items: [], totalCount: 0 }),
}));

vi.mock('@/services/jobPollingService', () => ({
  detectStatusTransitions: vi.fn(),
  adjustPollRate: vi.fn(),
  configure: vi.fn(),
  startPolling: vi.fn(),
  stopPolling: vi.fn(),
}));

beforeEach(() => {
  setActivePinia(createPinia());
  vi.clearAllMocks();
});

describe('任务列表使用服务端会话身份', () => {
  it('默认隐藏账号范围切换控件', () => {
    expect(useJobStore().showAccountScopeSwitch).toBe(false);
  });

  it.each(['currentAccount', 'allAccounts'] as const)('%s 范围不传入账号 UID', async (scope) => {
    const { getJobList } = await import('@/api/jobApi');
    const store = useJobStore();
    store.setAccountScope(scope);
    await store.fetchJobs();
    expect(getJobList).toHaveBeenLastCalledWith({ status: undefined, offset: 0, limit: 50 });
    expect(vi.mocked(getJobList).mock.calls.at(-1)?.[0]).not.toHaveProperty('cloudAccountUid');
  });
});
