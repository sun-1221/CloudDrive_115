<script setup lang="ts">
import { ShieldOff } from 'lucide-vue-next';
import { useAuthStore } from '@/stores/authStore';

const authStore = useAuthStore();
</script>

<template>
  <div class="glass-card p-8 max-w-md w-full text-center space-y-6">
    <div class="flex justify-center">
      <div class="w-16 h-16 rounded-full bg-orange-500/20 flex items-center justify-center">
        <ShieldOff :size="32" class="text-orange-400" />
      </div>
    </div>
    <h2 class="text-xl font-semibold text-white">功能暂不可用</h2>
    <p class="text-sm text-white/60">
      {{ authStore.featureUnavailableReason || '115 网盘功能当前未启用或暂不可用。' }}
    </p>
    <p class="text-xs text-white/40">
      如需使用此功能，请联系管理员或稍后再试。
    </p>
  </div>
</template>
