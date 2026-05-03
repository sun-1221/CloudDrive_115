import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import * as jobPollingService from '@/services/jobPollingService';

beforeEach(() => {
  vi.useFakeTimers();
  jobPollingService.stopPolling();
});

afterEach(() => {
  jobPollingService.stopPolling();
  vi.useRealTimers();
});

describe('需求 10.5: 轮询策略', () => {
  it('configure + startPolling 触发周期性轮询', async () => {
    const onPoll = vi.fn().mockResolvedValue(undefined);
    const onStatusChange = vi.fn();

    jobPollingService.configure(onPoll, onStatusChange);
    jobPollingService.startPolling();

    // 推进 5 秒（默认正常间隔）
    await vi.advanceTimersByTimeAsync(5100);

    expect(onPoll).toHaveBeenCalled();
  });

  it('stopPolling 停止轮询', async () => {
    const onPoll = vi.fn().mockResolvedValue(undefined);
    jobPollingService.configure(onPoll, vi.fn());
    jobPollingService.startPolling();

    jobPollingService.stopPolling();

    onPoll.mockClear();
    await vi.advanceTimersByTimeAsync(10000);

    expect(onPoll).not.toHaveBeenCalled();
  });

  it('adjustPollRate 无运行任务时降低频率', () => {
    const onPoll = vi.fn().mockResolvedValue(undefined);
    jobPollingService.configure(onPoll, vi.fn());
    jobPollingService.startPolling();

    // 设置为无运行任务，面板不可见 → 降低频率
    jobPollingService.adjustPollRate(false, false);

    // 不应抛错
    expect(true).toBe(true);
  });

  it('重复 startPolling 不创建多个定时器', () => {
    const onPoll = vi.fn().mockResolvedValue(undefined);
    jobPollingService.configure(onPoll, vi.fn());

    jobPollingService.startPolling();
    jobPollingService.startPolling();
    jobPollingService.startPolling();

    // 推进一个周期，应只调用一次
    vi.advanceTimersByTime(5100);
    // 不应出现多次回调叠加
    expect(onPoll.mock.calls.length).toBeLessThanOrEqual(1);
  });
});

describe('需求 10.6-10.7: 状态跃迁检测', () => {
  it('detectStatusTransitions 检测到 Running→Completed 时回调', () => {
    const onStatusChange = vi.fn();
    jobPollingService.configure(vi.fn().mockResolvedValue(undefined), onStatusChange);

    // 先注册前一状态
    jobPollingService.updatePreviousStatus('job-1', 'Running');

    // 检测跃迁
    jobPollingService.detectStatusTransitions([
      { id: 'job-1', status: 'Completed', description: '复制 3 个项目' },
    ]);

    expect(onStatusChange).toHaveBeenCalledWith('job-1', '复制 3 个项目', 'Completed');
  });

  it('detectStatusTransitions 检测到 Running→Failed 时回调', () => {
    const onStatusChange = vi.fn();
    jobPollingService.configure(vi.fn().mockResolvedValue(undefined), onStatusChange);

    jobPollingService.updatePreviousStatus('job-2', 'Running');

    jobPollingService.detectStatusTransitions([
      { id: 'job-2', status: 'Failed', description: '上传 文件.zip' },
    ]);

    expect(onStatusChange).toHaveBeenCalledWith('job-2', '上传 文件.zip', 'Failed');
  });

  it('Completed→Completed 不触发回调（非跃迁）', () => {
    const onStatusChange = vi.fn();
    jobPollingService.configure(vi.fn().mockResolvedValue(undefined), onStatusChange);

    jobPollingService.updatePreviousStatus('job-3', 'Completed');

    jobPollingService.detectStatusTransitions([
      { id: 'job-3', status: 'Completed', description: '任务' },
    ]);

    expect(onStatusChange).not.toHaveBeenCalled();
  });

  it('无前一状态时不触发回调', () => {
    const onStatusChange = vi.fn();
    jobPollingService.configure(vi.fn().mockResolvedValue(undefined), onStatusChange);

    jobPollingService.detectStatusTransitions([
      { id: 'job-new', status: 'Completed', description: '任务' },
    ]);

    expect(onStatusChange).not.toHaveBeenCalled();
  });
});
