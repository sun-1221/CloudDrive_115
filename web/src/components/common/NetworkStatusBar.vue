<script setup lang="ts">
import { computed } from 'vue';
import { useUIStore } from '@/stores/uiStore';

const uiStore = useUIStore();
const isOffline = computed(() => !uiStore.isOnline);
</script>

<template>
  <Transition name="slide-down">
    <div
      v-if="isOffline"
      class="fixed top-0 left-0 right-0 z-50 flex items-center justify-center px-4 py-2 text-sm font-medium text-white bg-red-600/90 backdrop-blur-sm"
    >
      网络连接已断开，请检查网络
    </div>
  </Transition>
</template>

<style scoped>
.slide-down-enter-active,
.slide-down-leave-active {
  transition: transform var(--transition-normal), opacity var(--transition-normal);
}
.slide-down-enter-from,
.slide-down-leave-to {
  transform: translateY(-100%);
  opacity: 0;
}
</style>
