import { describe, it, expect, vi, beforeEach } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

// Mock jobStore
const mockAddJob = vi.fn();
vi.mock('@/stores/jobStore', () => ({
  useJobStore: () => ({ addJob: mockAddJob }),
}));

// Mock naive-ui message
const mockMessageInfo = vi.fn();
vi.mock('naive-ui', () => ({
  useMessage: () => ({ info: mockMessageInfo, success: vi.fn(), error: vi.fn() }),
}));

import { executeOperation } from '@/services/operationService';

beforeEach(() => {
  setActivePinia(createPinia());
  vi.clearAllMocks();
});

describe('operationService 双态统一处理', () => {
  it('同步完成分支：调用 onSyncSuccess，不写入任务面板', async () => {
    const onSyncSuccess = vi.fn();

    const result = await executeOperation({
      execute: async () => ({ success: true }),
      onSyncSuccess,
    });

    expect(result.accepted).toBe(false);
    expect(onSyncSuccess).toHaveBeenCalledWith({ success: true });
    expect(mockAddJob).not.toHaveBeenCalled();
    expect(mockMessageInfo).not.toHaveBeenCalled();
  });

  it('异步受理分支：写入任务面板并 Toast "任务已开始"', async () => {
    const result = await executeOperation({
      execute: async () => ({ accepted: true, jobId: 'job-123', success: true }),
      refreshTargets: [{ type: 'directory', directoryId: 'dir-0' }],
      localContext: { kind: 'copy', fileCount: 5 },
    });

    expect(result.accepted).toBe(true);
    expect(result.jobId).toBe('job-123');
    expect(mockAddJob).toHaveBeenCalledWith(
      'job-123',
      [{ type: 'directory', directoryId: 'dir-0' }],
      { kind: 'copy', fileCount: 5 },
    );
    expect(mockMessageInfo).toHaveBeenCalledWith('任务已开始');
  });

  it('异步受理分支：自定义 Toast 消息', async () => {
    await executeOperation({
      execute: async () => ({ accepted: true, jobId: 'job-456', success: true }),
      toastMessage: '清空回收站已开始',
    });

    expect(mockMessageInfo).toHaveBeenCalledWith('清空回收站已开始');
  });

  it('accepted=false 且有 jobId 时仍为同步分支', async () => {
    const onSyncSuccess = vi.fn();

    await executeOperation({
      execute: async () => ({ accepted: false, jobId: 'job-789', success: true }),
      onSyncSuccess,
    });

    // accepted=false 走同步分支
    expect(onSyncSuccess).toHaveBeenCalled();
    expect(mockAddJob).not.toHaveBeenCalled();
  });
});
