import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import * as jobApi from '@/api/jobApi';
import * as jobPollingService from '@/services/jobPollingService';
import type {
  CloudDriveJobDto,
  CloudDriveJobStatus,
  JobListItem,
  JobLocalContext,
  RefreshTarget,
} from '@/types';

export const useJobStore = defineStore('job', () => {
  const jobs = ref<JobListItem[]>([]);
  const totalCount = ref(0);
  const statusFilter = ref<CloudDriveJobStatus | null>(null);
  const accountScope = ref<'currentAccount' | 'allAccounts'>('currentAccount');
  const showAccountScopeSwitch = ref(false);
  const panelVisible = ref(false);
  const pollTimerId = ref<number | null>(null);
  const pendingRefreshMap = ref(new Map<string, RefreshTarget[]>());
  const jobLocalContextMap = ref(new Map<string, JobLocalContext>());

  const hasRunningJobs = computed(() =>
    jobs.value.some((j) => j.job.status === 'Running' || j.job.status === 'Pending'),
  );

  const runningJobCount = computed(() =>
    jobs.value.filter((j) => j.job.status === 'Running' || j.job.status === 'Pending').length,
  );

  function buildDescription(job: CloudDriveJobDto): string {
    const localCtx = jobLocalContextMap.value.get(job.id);
    if (localCtx) {
      switch (localCtx.kind) {
        case 'upload':
          return `上传 ${localCtx.fileName ?? '文件'}`;
        case 'copy':
          return `复制 ${localCtx.fileCount ?? ''} 个项目`;
        case 'move':
          return `移动 ${localCtx.fileCount ?? ''} 个项目`;
        case 'delete':
          return `删除 ${localCtx.fileCount ?? ''} 个项目`;
        case 'recycleRestore':
          return `还原 ${localCtx.fileCount ?? ''} 个项目`;
        case 'recyclePermanentDelete':
          return `彻底删除 ${localCtx.fileCount ?? ''} 个项目`;
        case 'clearRecycleBin':
          return '清空回收站';
        case 'transcode':
          return `转码 ${localCtx.fileName ?? '视频'}`;
      }
    }

    // 回退到基于 jobType 的默认描述
    switch (job.jobType) {
      case 'Upload': return '上传文件';
      case 'BatchCopy': return '批量复制';
      case 'BatchMove': return '批量移动';
      case 'BatchDelete': return '批量删除';
      case 'ClearRecycleBin': return '清空回收站';
      case 'Transcode': return '视频转码';
      default: return '后台任务';
    }
  }

  async function fetchJobs(): Promise<void> {
    const { useAuthStore } = await import('@/stores/authStore');
    const authStore = useAuthStore();

    const input = {
      cloudAccountUid: accountScope.value === 'currentAccount'
        ? (authStore.currentAccountUid ?? undefined)
        : undefined,
      status: statusFilter.value ?? undefined,
      offset: 0,
      limit: 50,
    };

    const result = await jobApi.getJobList(input);
    totalCount.value = result.totalCount;

    jobs.value = result.items.map((job) => ({
      job,
      description: buildDescription(job),
      localContext: jobLocalContextMap.value.get(job.id),
      accountBadge: job.cloudAccountUid,
    }));

    // 检测状态跃迁
    jobPollingService.detectStatusTransitions(
      jobs.value.map((j) => ({
        id: j.job.id,
        status: j.job.status,
        description: j.description,
      })),
    );

    // 调整轮询频率
    jobPollingService.adjustPollRate(hasRunningJobs.value, panelVisible.value);
  }

  function addJob(
    jobId: string,
    refreshTargets?: RefreshTarget[],
    localContext?: JobLocalContext,
  ): void {
    if (refreshTargets && refreshTargets.length > 0) {
      pendingRefreshMap.value.set(jobId, refreshTargets);
    }
    if (localContext) {
      jobLocalContextMap.value.set(jobId, localContext);
    }
  }

  async function consumePendingRefresh(jobId: string): Promise<void> {
    const targets = pendingRefreshMap.value.get(jobId);
    if (!targets) return;

    pendingRefreshMap.value.delete(jobId);

    for (const target of targets) {
      if (target.type === 'directory') {
        const { useFileStore } = await import('@/stores/fileStore');
        const fileStore = useFileStore();
        if (fileStore.currentDirectoryId === target.directoryId) {
          await fileStore.refreshDirectory();
        }
      } else if (target.type === 'recycleBin') {
        const { useRecycleBinStore } = await import('@/stores/recycleBinStore');
        const recycleBinStore = useRecycleBinStore();
        recycleBinStore.setNeedsRefresh();
      }
    }
  }

  function startPolling(): void {
    jobPollingService.configure(
      fetchJobs,
      async (jobId, description, newStatus) => {
        const { useMessage } = await import('naive-ui');
        const message = useMessage();
        if (newStatus === 'Completed') {
          message.success(`${description} 已完成`);
          await consumePendingRefresh(jobId);
        } else if (newStatus === 'Failed') {
          message.error(`${description} 失败`);
        }
      },
    );
    jobPollingService.startPolling();
  }

  function stopPolling(): void {
    jobPollingService.stopPolling();
  }

  function togglePanel(): void {
    panelVisible.value = !panelVisible.value;
    jobPollingService.adjustPollRate(hasRunningJobs.value, panelVisible.value);
  }

  function setStatusFilter(status: CloudDriveJobStatus | null): void {
    statusFilter.value = status;
    fetchJobs();
  }

  function setAccountScope(scope: 'currentAccount' | 'allAccounts'): void {
    accountScope.value = scope;
    fetchJobs();
  }

  return {
    jobs,
    totalCount,
    statusFilter,
    accountScope,
    showAccountScopeSwitch,
    panelVisible,
    pollTimerId,
    pendingRefreshMap,
    jobLocalContextMap,
    hasRunningJobs,
    runningJobCount,
    fetchJobs,
    addJob,
    consumePendingRefresh,
    startPolling,
    stopPolling,
    togglePanel,
    setStatusFilter,
    setAccountScope,
  };
});
