<script setup lang="ts">
import { computed } from 'vue';
import { NButton, NTag, NProgress, NEmpty } from 'naive-ui';
import { ChevronUp, ChevronDown, ListTodo, Loader2 } from 'lucide-vue-next';
import { useJobStore } from '@/stores/jobStore';
import type { CloudDriveJobStatus } from '@/types';

const jobStore = useJobStore();

const statusFilters: { label: string; value: CloudDriveJobStatus | null }[] = [
  { label: '全部', value: null },
  { label: '运行中', value: 'Running' },
  { label: '已完成', value: 'Completed' },
  { label: '失败', value: 'Failed' },
];

const statusTagType = (status: CloudDriveJobStatus) => {
  switch (status) {
    case 'Completed': return 'success' as const;
    case 'Failed': return 'error' as const;
    case 'Running': return 'info' as const;
    case 'Pending': return 'warning' as const;
    default: return 'default' as const;
  }
};

const statusLabel = (status: CloudDriveJobStatus) => {
  switch (status) {
    case 'Completed': return '已完成';
    case 'Failed': return '失败';
    case 'Running': return '运行中';
    case 'Pending': return '等待中';
    default: return status;
  }
};

const showScopeSwitch = computed(() => jobStore.showAccountScopeSwitch);
</script>

<template>
  <div class="glass-card border-t border-white/10">
    <!-- 触发按钮 -->
    <button
      class="w-full flex items-center justify-between px-4 py-2 hover:bg-white/5 transition-colors duration-200"
      @click="jobStore.togglePanel()"
    >
      <div class="flex items-center gap-2">
        <ListTodo :size="16" class="text-white/60" />
        <span class="text-sm text-white/80">任务面板</span>
        <span
          v-if="jobStore.runningJobCount > 0"
          class="flex items-center gap-1 text-xs text-indigo-400"
        >
          <Loader2 :size="12" class="animate-spin" />
          {{ jobStore.runningJobCount }}
        </span>
      </div>
      <component :is="jobStore.panelVisible ? ChevronDown : ChevronUp" :size="16" class="text-white/40" />
    </button>

    <!-- 面板内容 -->
    <div v-if="jobStore.panelVisible" class="border-t border-white/10 max-h-64 overflow-y-auto">
      <!-- 筛选栏 -->
      <div class="flex items-center gap-2 px-4 py-2 border-b border-white/5">
        <NButton
          v-for="filter in statusFilters"
          :key="filter.label"
          :type="jobStore.statusFilter === filter.value ? 'primary' : 'default'"
          size="tiny"
          quaternary
          @click="jobStore.setStatusFilter(filter.value)"
        >
          {{ filter.label }}
        </NButton>

        <div v-if="showScopeSwitch" class="ml-auto flex items-center gap-1">
          <NButton
            :type="jobStore.accountScope === 'currentAccount' ? 'primary' : 'default'"
            size="tiny"
            quaternary
            @click="jobStore.setAccountScope('currentAccount')"
          >
            当前账号
          </NButton>
          <NButton
            :type="jobStore.accountScope === 'allAccounts' ? 'primary' : 'default'"
            size="tiny"
            quaternary
            @click="jobStore.setAccountScope('allAccounts')"
          >
            全部账号
          </NButton>
        </div>
      </div>

      <!-- 任务列表 -->
      <div v-if="jobStore.jobs.length === 0" class="py-8">
        <NEmpty description="暂无任务" size="small" />
      </div>
      <div v-else class="divide-y divide-white/5">
        <div
          v-for="item in jobStore.jobs"
          :key="item.job.id"
          class="flex items-center gap-3 px-4 py-3"
        >
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2">
              <p class="text-sm text-white/80 truncate">{{ item.description }}</p>
              <NTag
                v-if="jobStore.accountScope === 'allAccounts'"
                size="tiny"
                :bordered="false"
              >
                {{ item.accountBadge }}
              </NTag>
            </div>
            <p
              v-if="item.job.status === 'Failed' && item.job.errorMessage"
              class="text-xs text-red-400 mt-0.5 truncate"
            >
              {{ item.job.errorMessage }}
            </p>
          </div>

          <NProgress
            v-if="item.job.status === 'Running'"
            type="line"
            :percentage="item.job.progress"
            :show-indicator="false"
            class="w-20"
          />

          <NTag :type="statusTagType(item.job.status)" size="small" :bordered="false">
            {{ statusLabel(item.job.status) }}
          </NTag>
        </div>
      </div>
    </div>
  </div>
</template>
