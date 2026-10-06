<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { NButton } from 'naive-ui';
import { Link } from 'lucide-vue-next';
import { useAuthStore } from '@/stores/authStore';
import FeatureUnavailableState from '@/components/common/FeatureUnavailableState.vue';
import { toSafeUiError } from '@/api/problemDetails';
import '@/styles/main.css';

const authStore = useAuthStore();
const router = useRouter();
const route = useRoute();
const isDisabled = computed(() => authStore.authStatus === 'disabled');
const userName = ref('');
const password = ref('');
const registerMode = ref(false);
const errorMessage = ref('');

onMounted(() => {
  if (authStore.authStatus === 'unknown') void authStore.bootstrapAuthState();
});

async function retry(): Promise<void> {
  errorMessage.value = '';
  try {
    await authStore.signIn(userName.value, password.value, registerMode.value);
  } catch (error: unknown) {
    errorMessage.value = toSafeUiError(error).message;
  }
  if (authStore.authStatus === 'authorized') {
    const returnUrl = typeof route.query.returnUrl === 'string' ? route.query.returnUrl : '/cloud-drive/files';
    await router.replace(returnUrl);
  }
}
</script>

<template>
  <main class="min-h-screen flex items-center justify-center bg-[#0a0a0f] p-6" aria-labelledby="login-title">
    <FeatureUnavailableState v-if="isDisabled" />
    <section v-else class="glass-card w-full max-w-md p-8 text-center space-y-6">
      <div class="mx-auto w-16 h-16 rounded-full bg-indigo-500/20 flex items-center justify-center">
        <Link :size="32" class="text-indigo-400" aria-hidden="true" />
      </div>
      <div>
        <h1 id="login-title" class="text-xl font-semibold text-white">{{ registerMode ? '创建个人账号' : '登录 115 Web' }}</h1>
        <p class="mt-2 text-sm text-white/60">使用本地数据库账号登录，115 Cookie 仅在绑定时加密保存。</p>
      </div>
      <input v-model="userName" class="w-full rounded bg-white/10 px-3 py-2 text-white" placeholder="用户名" autocomplete="username" />
      <input v-model="password" type="password" class="w-full rounded bg-white/10 px-3 py-2 text-white" placeholder="密码（至少 8 位）" autocomplete="current-password" />
      <p v-if="errorMessage" class="text-sm text-amber-300">{{ errorMessage }}</p>
      <NButton type="primary" size="large" block :loading="authStore.loading" @click="retry">
        {{ registerMode ? '注册并登录' : '登录' }}
      </NButton>
      <button class="text-sm text-indigo-300" type="button" @click="registerMode = !registerMode">
        {{ registerMode ? '已有账号？登录' : '没有账号？注册' }}
      </button>
    </section>
  </main>
</template>
