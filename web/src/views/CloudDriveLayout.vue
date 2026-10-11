<script setup lang="ts">
import { computed, onMounted, onUnmounted } from 'vue';
import { useRoute } from 'vue-router';
import { useUIStore } from '@/stores/uiStore';
import { useAuthStore } from '@/stores/authStore';
import AppSidebar from '@/components/layout/AppSidebar.vue';
import FileToolbar from '@/components/layout/FileToolbar.vue';
import MobileBottomNav from '@/components/layout/MobileBottomNav.vue';
import AuthPanel from '@/components/common/AuthPanel.vue';
import '@/styles/main.css';

const uiStore = useUIStore();
const authStore = useAuthStore();
const route = useRoute();

const isAuthorized = computed(() => authStore.authStatus === 'authorized');
const hasAccount = computed(() => (authStore.currentUser?.accounts.length ?? 0) > 0);
const needsAccountSelection = computed(() => hasAccount.value
  && (authStore.currentUser?.accounts.length ?? 0) > 1
  && !authStore.currentUser?.currentCloudAccountUid);
const upstreamUnavailable = computed(() => {
  const check = authStore.loginCheckResult;
  return Boolean(check && check.isLoggedIn !== true);
});
const isUserCenter = computed(() => route.name === 'UserCenter');
const canShowWorkspace = computed(() => isAuthorized.value && hasAccount.value && !needsAccountSelection.value && !upstreamUnavailable.value);
const isMobile = computed(() => uiStore.breakpoint === 'mobile');

onMounted(() => {
  uiStore.initListeners();
  authStore.bootstrapAuthState();
});

onUnmounted(() => {
  uiStore.destroyListeners();
});
</script>

<template>
  <div class="min-h-screen bg-[#0a0a0f] flex">
    <!-- 侧边栏（桌面/平板） -->
    <AppSidebar v-if="!isMobile && (canShowWorkspace || isUserCenter)" />

    <div class="flex-1 flex flex-col min-w-0">
      <!-- 工具栏 -->
      <FileToolbar v-if="canShowWorkspace" />

      <!-- 主内容区 -->
      <main class="flex-1 overflow-y-auto">
        <router-view v-if="isUserCenter || canShowWorkspace" />
        <AuthPanel v-else />
      </main>

    </div>

    <!-- 移动端底部导航 -->
    <MobileBottomNav v-if="isMobile && (canShowWorkspace || isUserCenter)" />
  </div>
</template>
