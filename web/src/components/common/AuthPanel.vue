<script setup lang="ts">
import { computed } from 'vue';
import { useRouter } from 'vue-router';
import { NButton } from 'naive-ui';
import { Link } from 'lucide-vue-next';
import { useAuthStore } from '@/stores/authStore';
import FeatureUnavailableState from './FeatureUnavailableState.vue';

const authStore = useAuthStore();
const router = useRouter();

const isAuthorized = computed(() => authStore.authStatus === 'authorized');
const isDisabled = computed(() => authStore.authStatus === 'disabled');
const hasAccount = computed(() => (authStore.currentUser?.accounts.length ?? 0) > 0);

function handleRetryCheck() {
  authStore.bootstrapAuthState();
}

function openUserCenter() {
  void router.push({ name: 'UserCenter' });
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
        当前账号尚未绑定 115 账号，请登录后在用户中心绑定。
      </p>
      <NButton
        type="primary"
        size="large"
        block
        :loading="authStore.loading"
        @click="handleRetryCheck"
      >
        重新检查连接状态
      </NButton>
    </div>

    <!-- 已授权态（简化：仅显示已连接状态） -->
    <div v-else-if="hasAccount" class="glass-card p-6 max-w-md w-full space-y-4">
      <div class="flex items-center gap-4">
        <div class="w-12 h-12 rounded-full bg-green-500/20 flex items-center justify-center shrink-0">
          <Link :size="20" class="text-green-400" />
        </div>
        <div class="flex-1 min-w-0">
          <p class="text-white font-medium">115 网盘已连接</p>
          <p class="text-xs text-white/50">Cookie 会话有效</p>
        </div>
      </div>
    </div>

    <div v-else class="glass-card p-6 max-w-md w-full space-y-4 text-center">
      <p class="text-white font-medium">尚未绑定 115 账号</p>
      <p class="text-xs text-white/50">请在用户中心绑定账号后再访问文件。</p>
      <NButton type="primary" block @click="openUserCenter">打开用户中心</NButton>
    </div>
  </div>
</template>
