<script setup lang="ts">
import { computed } from 'vue';
import { NButton, NAvatar, useDialog } from 'naive-ui';
import { Link, Unlink, ChevronDown } from 'lucide-vue-next';
import { useAuthStore } from '@/stores/authStore';
import FeatureUnavailableState from './FeatureUnavailableState.vue';

const authStore = useAuthStore();
const dialog = useDialog();

const isAuthorized = computed(() => authStore.authStatus === 'authorized');
const isDisabled = computed(() => authStore.authStatus === 'disabled');
const hasMultipleAccounts = computed(() => authStore.hasMultipleAccounts);

function handleConnect() {
  authStore.redirectToAuth();
}

function handleDisconnect() {
  dialog.warning({
    title: '断开连接',
    content: '确定要断开与 115 网盘的连接吗？断开后需要重新授权才能访问文件。',
    positiveText: '断开',
    negativeText: '取消',
    onPositiveClick: () => {
      authStore.disconnect();
    },
  });
}

function handleSwitchAccount(uid: string) {
  authStore.switchAccount(uid);
}
</script>

<template>
  <div class="flex flex-col items-center justify-center min-h-[400px] p-8">
    <!-- 功能不可用态 -->
    <FeatureUnavailableState v-if="isDisabled" />

    <!-- 未授权态 -->
    <div v-else-if="!isAuthorized" class="glass-card p-8 max-w-md w-full text-center space-y-6">
      <div class="flex justify-center">
        <div class="w-16 h-16 rounded-full bg-indigo-500/20 flex items-center justify-center">
          <Link :size="32" class="text-indigo-400" />
        </div>
      </div>
      <h2 class="text-xl font-semibold text-white">连接 115 网盘</h2>
      <p class="text-sm text-white/60">
        授权连接您的 115 网盘账号，即可在此管理和浏览您的云端文件。
      </p>
      <NButton
        type="primary"
        size="large"
        block
        :loading="authStore.loading"
        @click="handleConnect"
      >
        连接 115 网盘
      </NButton>
    </div>

    <!-- 已授权态 -->
    <div v-else class="glass-card p-6 max-w-md w-full space-y-4">
      <div class="flex items-center gap-4">
        <NAvatar
          v-if="authStore.currentAccount?.avatarUrl"
          :src="authStore.currentAccount.avatarUrl"
          :size="48"
          round
        />
        <div v-else class="w-12 h-12 rounded-full bg-indigo-500/20 flex items-center justify-center shrink-0">
          <span class="text-indigo-400 font-medium">{{ authStore.currentAccount?.nickname?.charAt(0) ?? '?' }}</span>
        </div>
        <div class="flex-1 min-w-0">
          <p class="text-white font-medium truncate">
            {{ authStore.currentAccount?.nickname ?? '未知用户' }}
          </p>
          <p class="text-xs text-white/50 truncate">
            UID: {{ authStore.currentAccountUid }}
          </p>
        </div>
        <NButton
          quaternary
          size="small"
          type="error"
          @click="handleDisconnect"
        >
          <template #icon>
            <Unlink :size="16" />
          </template>
          断开
        </NButton>
      </div>

      <!-- 多账号切换 -->
      <div v-if="hasMultipleAccounts" class="border-t border-white/10 pt-4">
        <p class="text-xs text-white/50 mb-2">切换账号</p>
        <div class="space-y-1">
          <button
            v-for="account in authStore.accounts"
            :key="account.cloudAccountUid"
            class="w-full flex items-center gap-3 px-3 py-2 rounded-lg text-left transition-colors duration-200"
            :class="account.cloudAccountUid === authStore.currentAccountUid
              ? 'bg-indigo-500/20 text-indigo-300'
              : 'hover:bg-white/5 text-white/70'"
            @click="handleSwitchAccount(account.cloudAccountUid)"
          >
            <span class="text-sm truncate flex-1">{{ account.nickname }}</span>
            <ChevronDown
              v-if="account.cloudAccountUid === authStore.currentAccountUid"
              :size="14"
              class="text-indigo-400 rotate-0"
            />
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
