<script setup lang="ts">
import { AlertTriangle } from 'lucide-vue-next';
import { NButton } from 'naive-ui';

defineProps<{
  title?: string;
  message: string;
}>();

const emit = defineEmits<{
  retry: [];
}>();
</script>

<template>
  <div class="flex flex-col items-center justify-center gap-4 py-16 text-center">
    <AlertTriangle :size="48" class="text-red-400/80" />
    <h3 class="text-lg font-medium text-white/90">
      {{ title ?? '加载失败' }}
    </h3>
    <p class="text-sm text-white/60 max-w-md">
      {{ message }}
    </p>
    <NButton type="primary" @click="emit('retry')">
      重试
    </NButton>
  </div>
</template>
