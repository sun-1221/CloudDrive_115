<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useMessage } from 'naive-ui';
import { Loader2 } from 'lucide-vue-next';
import { useAuthStore } from '@/stores/authStore';

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const message = useMessage();
const status = ref<'loading' | 'error'>('loading');
const errorDetail = ref('');

onMounted(async () => {
  const code = route.query.code as string | undefined;
  const state = route.query.state as string | undefined;

  if (!code) {
    errorDetail.value = '缺少授权码参数';
    status.value = 'error';
    message.error('授权失败：缺少授权码参数，请重试连接');
    redirectToFiles();
    return;
  }

  // CSRF state 校验
  const savedState = sessionStorage.getItem('oauthState');
  if (!state || state !== savedState) {
    errorDetail.value = '状态校验失败，可能存在安全风险';
    status.value = 'error';
    message.error('授权失败：状态校验不通过，请重试连接');
    redirectToFiles();
    return;
  }

  // 恢复 pkceSessionId
  const pkceSessionId = sessionStorage.getItem('pkceSessionId');
  if (!pkceSessionId) {
    errorDetail.value = 'PKCE 会话已过期';
    status.value = 'error';
    message.error('授权失败：PKCE 会话已过期，请重试连接');
    redirectToFiles();
    return;
  }

  try {
    await authStore.handleCallback(code, pkceSessionId);
    // 清理 sessionStorage
    sessionStorage.removeItem('oauthState');
    sessionStorage.removeItem('pkceSessionId');
    message.success('授权成功');
    router.replace('/cloud-drive/files');
  } catch (e) {
    const reason = e instanceof Error ? e.message : '未知错误';
    errorDetail.value = reason;
    status.value = 'error';
    message.error(`授权失败：${reason}，请重试连接`);
    redirectToFiles();
  }
});

function redirectToFiles() {
  setTimeout(() => {
    router.replace('/cloud-drive/files');
  }, 2000);
}
</script>

<template>
  <div class="flex items-center justify-center min-h-screen">
    <div class="glass-card p-8 text-center space-y-4 max-w-sm">
      <template v-if="status === 'loading'">
        <Loader2 :size="32" class="animate-spin text-indigo-400 mx-auto" />
        <p class="text-white/80">正在完成授权...</p>
      </template>
      <template v-else>
        <p class="text-red-400 font-medium">授权失败</p>
        <p class="text-sm text-white/60">{{ errorDetail }}</p>
        <p class="text-xs text-white/40">正在跳转...</p>
      </template>
    </div>
  </div>
</template>
