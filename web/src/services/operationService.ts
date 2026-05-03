import type {
  CloudDriveOperationResultDto,
  RefreshTarget,
  JobLocalContext,
} from '@/types';

export interface OperationOptions<T> {
  execute: () => Promise<T>;
  onSyncSuccess?: (result: T) => void | Promise<void>;
  refreshTargets?: RefreshTarget[];
  localContext?: JobLocalContext;
  toastMessage?: string;
}

export interface OperationResult<T> {
  accepted: boolean;
  jobId?: string;
  data?: T;
}

function isOperationResult(value: unknown): value is CloudDriveOperationResultDto {
  return (
    typeof value === 'object' &&
    value !== null &&
    'accepted' in value &&
    typeof (value as CloudDriveOperationResultDto).accepted === 'boolean'
  );
}

export async function executeOperation<T>(
  options: OperationOptions<T>,
): Promise<OperationResult<T>> {
  const result = await options.execute();

  if (isOperationResult(result) && result.accepted && result.jobId) {
    // 异步受理分支：写入任务面板
    const { useJobStore } = await import('@/stores/jobStore');
    const jobStore = useJobStore();
    jobStore.addJob(result.jobId, options.refreshTargets, options.localContext);

    // Toast "任务已开始"
    const { useMessage } = await import('naive-ui');
    const message = useMessage();
    message.info(options.toastMessage ?? '任务已开始');

    return { accepted: true, jobId: result.jobId, data: result as T };
  }

  // 同步完成分支
  if (options.onSyncSuccess) {
    await options.onSyncSuccess(result);
  }

  return { accepted: false, data: result };
}
