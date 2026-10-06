import type { CloudDriveJobStatus } from '@/types';

const NORMAL_INTERVAL = 5000;
const REDUCED_INTERVAL = 30000;

let timerId: ReturnType<typeof setInterval> | null = null;
let currentInterval = NORMAL_INTERVAL;
let visibilityListenerAttached = false;
const previousStatusMap = new Map<string, CloudDriveJobStatus>();

type PollCallback = () => Promise<void>;
type StatusChangeCallback = (jobId: string, description: string, newStatus: CloudDriveJobStatus) => void;

let pollFn: PollCallback | null = null;
let statusChangeFn: StatusChangeCallback | null = null;

export function configure(
  onPoll: PollCallback,
  onStatusChange: StatusChangeCallback,
): void {
  pollFn = onPoll;
  statusChangeFn = onStatusChange;
}

export function startPolling(): void {
  if (timerId !== null) return;
  currentInterval = evaluatePollRate();
  scheduleNextPoll();
  setupVisibilityListener();
}

export function stopPolling(): void {
  if (timerId !== null) {
    clearTimeout(timerId);
    timerId = null;
  }
  teardownVisibilityListener();
  previousStatusMap.clear();
}

export function updatePreviousStatus(jobId: string, status: CloudDriveJobStatus): void {
  previousStatusMap.set(jobId, status);
}

export function detectStatusTransitions(
  jobs: Array<{ id: string; status: CloudDriveJobStatus; description: string }>,
): void {
  const terminalStatuses: CloudDriveJobStatus[] = ['Completed', 'Failed'];

  for (const job of jobs) {
    const prev = previousStatusMap.get(job.id);
    if (prev && !terminalStatuses.includes(prev) && terminalStatuses.includes(job.status)) {
      statusChangeFn?.(job.id, job.description, job.status);
    }
    previousStatusMap.set(job.id, job.status);
  }
}

function evaluatePollRate(): number {
  // 页面不可见时暂停
  if (document.visibilityState === 'hidden') return 0;

  // 通过动态导入检查 JobStore 状态
  // 此处简化实现，实际由调用方通过 hasRunningJobs 参数控制
  return NORMAL_INTERVAL;
}

export function adjustPollRate(hasRunningJobs: boolean, panelVisible: boolean): void {
  if (document.visibilityState === 'hidden') {
    pausePolling();
    return;
  }

  const shouldReduce = !hasRunningJobs || !panelVisible;
  const newInterval = shouldReduce ? REDUCED_INTERVAL : NORMAL_INTERVAL;

  if (newInterval !== currentInterval) {
    currentInterval = newInterval;
    restartPolling();
  }
}

function scheduleNextPoll(): void {
  if (currentInterval <= 0) return;
  timerId = setTimeout(async () => {
    timerId = null;
    if (pollFn) {
      await pollFn();
    }
    if (currentInterval > 0) {
      scheduleNextPoll();
    }
  }, currentInterval);
}

function restartPolling(): void {
  if (timerId !== null) {
    clearTimeout(timerId);
    timerId = null;
  }
  if (currentInterval > 0) {
    scheduleNextPoll();
  }
}

function pausePolling(): void {
  if (timerId !== null) {
    clearTimeout(timerId);
    timerId = null;
  }
}

function handleVisibilityChange(): void {
  if (document.visibilityState === 'visible') {
    currentInterval = evaluatePollRate();
    if (timerId === null && currentInterval > 0) {
      scheduleNextPoll();
    }
  } else {
    pausePolling();
  }
}

function setupVisibilityListener(): void {
  if (visibilityListenerAttached) return;
  document.addEventListener('visibilitychange', handleVisibilityChange);
  visibilityListenerAttached = true;
}

function teardownVisibilityListener(): void {
  if (!visibilityListenerAttached) return;
  document.removeEventListener('visibilitychange', handleVisibilityChange);
  visibilityListenerAttached = false;
}
