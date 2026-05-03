<script setup lang="ts">
import { NMessageProvider, NNotificationProvider, NDialogProvider, NConfigProvider, darkTheme } from 'naive-ui';
import { themeOverrides } from '@/theme/naiveThemeOverrides';
import NetworkStatusBar from '@/components/common/NetworkStatusBar.vue';
</script>

<template>
  <NConfigProvider :theme="darkTheme" :theme-overrides="themeOverrides">
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
