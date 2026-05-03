import { describe, it, expect, beforeEach, vi } from 'vitest';
import { fc, test as fcTest } from '@fast-check/vitest';
import { createPinia, setActivePinia } from 'pinia';
import { useJobStore } from '@/stores/jobStore';

// Mock jobApi 和 jobPollingService 避免实际网络请求
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
});

describe('Property 31: 任务面板账号范围切换语义稳定', () => {
  fcTest.prop([
    fc.constantFrom('currentAccount', 'allAccounts') as fc.Arbitrary<'currentAccount' | 'allAccounts'>,
  ])(
    '切换账号范围后 accountScope 立即更新',
    (scope) => {
      const store = useJobStore();
      store.setAccountScope(scope);
      expect(store.accountScope).toBe(scope);
    },
  );

  it('currentAccount 模式传递 cloudAccountUid 给 API', async () => {
    const { getJobList } = await import('@/api/jobApi');
    const jobStore = useJobStore();

    // 设置 authStore 的 currentAccountUid
    const { useAuthStore } = await import('@/stores/authStore');
    const authStore = useAuthStore();
    authStore.currentAccountUid = 'test-uid-123';

    jobStore.setAccountScope('currentAccount');
    await jobStore.fetchJobs();

    expect(getJobList).toHaveBeenCalledWith(
      expect.objectContaining({
        cloudAccountUid: 'test-uid-123',
      }),
    );
  });

  it('allAccounts 模式不传递 cloudAccountUid', async () => {
    const { getJobList } = await import('@/api/jobApi');
    const jobStore = useJobStore();

    jobStore.setAccountScope('allAccounts');
    await jobStore.fetchJobs();

    expect(getJobList).toHaveBeenCalledWith(
      expect.objectContaining({
        cloudAccountUid: undefined,
      }),
    );
  });
});
