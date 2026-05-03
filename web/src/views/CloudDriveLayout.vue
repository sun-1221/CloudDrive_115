<script setup lang="ts">
import { computed, onMounted, onUnmounted } from 'vue';
import { useUIStore } from '@/stores/uiStore';
import { useAuthStore } from '@/stores/authStore';
import { useJobStore } from '@/stores/jobStore';
import AppSidebar from '@/components/layout/AppSidebar.vue';
import FileToolbar from '@/components/layout/FileToolbar.vue';
import MobileBottomNav from '@/components/layout/MobileBottomNav.vue';
import AuthPanel from '@/components/common/AuthPanel.vue';
import TaskPanel from '@/components/common/TaskPanel.vue';
import '@/styles/main.css';

const uiStore = useUIStore();
const authStore = useAuthStore();
const jobStore = useJobStore();

const isAuthorized = computed(() => authStore.authStatus === 'authorized');
const isDisabled = computed(() => authStore.authStatus === 'disabled');
const isMobile = computed(() => uiStore.breakpoint === 'mobile');

onMounted(() => {
  uiStore.initListeners();
  authStore.bootstrapAuthState();
  jobStore.startPolling();
});

onUnmounted(() => {
  uiStore.destroyListeners();
  jobStore.stopPolling();
});
</script>

<template>
  <div class="min-h-screen bg-[#0a0a0f] flex">
    <!-- 侧边栏（桌面/平板） -->
    <AppSidebar v-if="!isMobile && isAuthorized" />

    <div class="flex-1 flex flex-col min-w-0">
      <!-- 工具栏 -->
      <FileToolbar v-if="isAuthorized" />

      <!-- 主内容区 -->
      <main class="flex-1 overflow-y-auto">
        <AuthPanel v-if="!isAuthorized || isDisabled" />
        <router-view v-else />
      </main>

      <!-- 任务面板 -->
      <TaskPanel v-if="isAuthorized" />
    </div>

    <!-- 移动端底部导航 -->
    <MobileBottomNav v-if="isMobile && isAuthorized" />
  </div>
</template>
