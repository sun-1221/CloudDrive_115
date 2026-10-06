<script setup lang="ts">
import { computed } from 'vue';
import { NMessageProvider, NNotificationProvider, NDialogProvider, NConfigProvider, darkTheme, lightTheme } from 'naive-ui';
import { themeOverrides } from '@/theme/naiveThemeOverrides';
import NetworkStatusBar from '@/components/common/NetworkStatusBar.vue';
import { useUIStore } from '@/stores/uiStore';

const uiStore = useUIStore();
const activeTheme = computed(() => uiStore.theme === 'dark' ? darkTheme : lightTheme);
</script>

<template>
  <NConfigProvider :theme="activeTheme" :theme-overrides="themeOverrides">
    <NMessageProvider>
      <NDialogProvider>
        <NNotificationProvider>
          <NetworkStatusBar />
          <router-view />
        </NNotificationProvider>
      </NDialogProvider>
    </NMessageProvider>
  </NConfigProvider>
</template>
